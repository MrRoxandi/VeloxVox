using System.Runtime.Versioning;
using VeloxVox.Abstractions;
using VeloxVox.Models;
using VeloxVox.Services;

namespace VeloxVox;

/// <summary>
///     The main orchestration engine for VeloxVox. Manages the audio queue,
///     playback, and TTS synthesis. Create instances using <see cref="VeloxVoxBuilder" />.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class VeloxVoxEngine : IAsyncDisposable
{
    private readonly IAudioQueue _queue;
    private readonly IAudioPlayer _player;
    private readonly ITtsEngine _tts;
    private readonly CancellationTokenSource _engineCts = new();
    private readonly AsyncAutoResetEvent _pulse = new();
    private readonly Task _playbackPumpTask;

    /// <summary>
    ///     Fired when a new item from the queue begins to play.
    /// </summary>
    public event EventHandler<QueueItemStartedEventArgs>? QueueItemStarted;

    /// <summary>
    ///     Fired when an item finishes playing, is skipped, or the engine shuts down.
    /// </summary>
    public event EventHandler<QueueItemCompletedEventArgs>? QueueItemCompleted;

    /// <summary>
    ///     Fired when an error occurs during an item's playback.
    /// </summary>
    public event EventHandler<QueueItemFailedEventArgs>? QueueItemFailed;

    /// <summary>
    ///     Fired when the queue becomes empty and the last item has finished playing.
    /// </summary>
    public event EventHandler<QueueEmptyEventArgs>? QueueEmpty;

    internal VeloxVoxEngine(IAudioQueue queue, IAudioPlayer player, ITtsEngine tts)
    {
        _queue = queue;
        _player = player;
        _tts = tts;

        _player.PlaybackCompleted += OnPlayerPlaybackCompleted;
        _player.PlaybackError += OnPlayerPlaybackError;

        _playbackPumpTask = Task.Factory.StartNew(
            async () => await PlaybackPumpLoopAsync(),
            _engineCts.Token,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default
        ).Unwrap();
    }

    /// <summary>
    ///     Gets the number of items currently in the playback queue.
    /// </summary>
    public int QueueLength => _queue.Count;

    /// <summary>
    ///     Gets the current playback state of the audio player.
    /// </summary>
    public PlaybackState PlaybackState => _player.State;

    /// <summary>
    ///     Gets the audio item that is currently playing.
    /// </summary>
    public AudioItem? CurrentItem => _player.CurrentItem;

    /// <summary>
    ///     Enqueues a local audio file for playback.
    /// </summary>
    /// <param name="filePath">The full path to the audio file.</param>
    public void EnqueueFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentNullException(nameof(filePath));
        if (!File.Exists(filePath))
            throw new FileNotFoundException("The specified audio file was not found.", filePath);

        _queue.Enqueue(AudioItem.FromFile(Path.GetFullPath(filePath)));
        _pulse.Set(); // Signal the pump to check the queue.
    }

    /// <summary>
    ///     Enqueues an audio stream from a network URL for playback.
    ///     Supported formats depend on the LibVLC backend (e.g., HTTP streams, direct links to MP3/WAV/etc.).
    /// </summary>
    /// <param name="url">The absolute URL of the audio stream.</param>
    /// <exception cref="ArgumentException">Thrown if the URL is not a valid absolute URI.</exception>
    public void EnqueueUrl(string url)
    {
        var audioItem = AudioItem.FromUrl(url); // Validation happens inside FromUrl
        _queue.Enqueue(audioItem);
        _pulse.Set(); // Signal the pump to check the queue.
    }

    /// <summary>
    ///     Synthesizes text to speech and enqueues the resulting audio for playback.
    /// </summary>
    /// <param name="text">The text to synthesize.</param>
    /// <param name="options">Optional TTS settings. If null, defaults will be used.</param>
    /// <param name="ct">A cancellation token for the synthesis operation.</param>
    public async Task EnqueueTtsAsync(string text, TtsOptions? options = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        options ??= new TtsOptions();

        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, _engineCts.Token);

        var tempFilePath = await _tts.SynthesizeToFileAsync(text, options, linkedCts.Token).ConfigureAwait(false);

        _queue.Enqueue(AudioItem.FromTempFile(tempFilePath));
        _pulse.Set(); // Signal the pump.
    }

    /// <summary>
    ///     Stops the currently playing audio item. The queue will then proceed to the next item.
    ///     This is equivalent to skipping the current item.
    /// </summary>
    public async ValueTask SkipCurrentAsync()
    {
        if (_player.State is PlaybackState.Playing or PlaybackState.Stopping)
            await _player.StopAsync(_engineCts.Token).ConfigureAwait(false);
    }

    /// <summary>
    ///     Clears all items from the playback queue. Does not stop the currently playing item.
    /// </summary>
    public void ClearQueue()
    {
        _queue.Clear();
    }

    private async Task PlaybackPumpLoopAsync()
    {
        while (!_engineCts.IsCancellationRequested)
            try
            {
                await _pulse.WaitAsync(_engineCts.Token).ConfigureAwait(false);

                // Process all items in the queue as long as the player is idle
                while (_player.State == PlaybackState.Idle && _queue.TryDequeue(out var item))
                {
                    _engineCts.Token.ThrowIfCancellationRequested();

                    if (item is null) continue;

                    try
                    {
                        QueueItemStarted?.Invoke(this, new QueueItemStartedEventArgs(item));
                        await _player.PlayAsync(item, _engineCts.Token).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        QueueItemFailed?.Invoke(this, new QueueItemFailedEventArgs(item, ex));
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // This is the expected way to exit the loop.
                break;
            }
            catch
            {
                // Avoid a tight spin-loop on repeated errors.
                await Task.Delay(500, CancellationToken.None).ConfigureAwait(false);
            }
    }

    private Task OnPlayerPlaybackCompleted(object sender, PlaybackCompletedEventArgs e)
    {
        QueueItemCompleted?.Invoke(this, new QueueItemCompletedEventArgs(e.Item, e.Reason));
        CheckIfQueueIsEmpty();
        _pulse.Set(); // Signal the pump to check for the next item.
        return Task.CompletedTask;
    }

    private Task OnPlayerPlaybackError(object sender, PlaybackErrorEventArgs e)
    {
        QueueItemFailed?.Invoke(this, new QueueItemFailedEventArgs(e.Item, e.Exception));
        CheckIfQueueIsEmpty();
        _pulse.Set(); // Signal the pump to move on.
        return Task.CompletedTask;
    }

    private void CheckIfQueueIsEmpty()
    {
        if (_queue.Count == 0 && _player.State == PlaybackState.Idle)
            QueueEmpty?.Invoke(this, new QueueEmptyEventArgs());
    }

    /// <summary>
    ///     Shuts down the engine, stops playback, and releases all resources.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        // 1. Signal cancellation to all operations
        if (!_engineCts.IsCancellationRequested) await _engineCts.CancelAsync();

        // 2. Unblock the pump task so it can exit
        _pulse.Set();

        // 3. Wait for the pump to finish
        try
        {
            await _playbackPumpTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            /* Expected */
        }
        catch
        {
            // ignored
        }

        // 4. Dispose managed resources
        await _player.DisposeAsync().ConfigureAwait(false);
        await _tts.DisposeAsync().ConfigureAwait(false);
        _queue.Clear(); // Clean up any remaining temp files
        _engineCts.Dispose();
        _pulse.Dispose();
    }
}

/// <summary>
///     A builder for creating and configuring a <see cref="VeloxVoxEngine" /> instance.
///     This provides a fluent API for setting up the engine with custom or default components.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class VeloxVoxBuilder
{
    private IAudioPlayer? _audioPlayer;
    private ITtsEngine? _ttsEngine;
    private IAudioQueue? _audioQueue;

    /// <summary>
    ///     Specifies a custom audio player backend.
    /// </summary>
    public VeloxVoxBuilder WithAudioPlayer(IAudioPlayer audioPlayer)
    {
        _audioPlayer = audioPlayer ?? throw new ArgumentNullException(nameof(audioPlayer));
        return this;
    }

    /// <summary>
    ///     Specifies a custom Text-To-Speech engine.
    /// </summary>
    public VeloxVoxBuilder WithTtsEngine(ITtsEngine ttsEngine)
    {
        _ttsEngine = ttsEngine ?? throw new ArgumentNullException(nameof(ttsEngine));
        return this;
    }

    /// <summary>
    ///     Specifies a custom audio queue implementation.
    /// </summary>
    public VeloxVoxBuilder WithAudioQueue(IAudioQueue audioQueue)
    {
        _audioQueue = audioQueue ?? throw new ArgumentNullException(nameof(audioQueue));
        return this;
    }

    /// <summary>
    ///     Asynchronously builds, initializes, and returns a new <see cref="VeloxVoxEngine" /> instance
    ///     based on the provided configuration.
    /// </summary>
    /// <param name="ct">A cancellation token for the initialization process.</param>
    /// <returns>A fully initialized and running <see cref="VeloxVoxEngine" />.</returns>
    public async Task<VeloxVoxEngine> BuildAsync(CancellationToken ct = default)
    {
        var player = _audioPlayer ?? new VlcAudioPlayer();
        await player.InitializeAsync(ct).ConfigureAwait(false);

        var tts = _ttsEngine ?? new SpeechTtsEngine();
        var queue = _audioQueue ?? new AudioQueue();

        return new VeloxVoxEngine(queue, player, tts);
    }
}

/// <summary>
///     An asynchronous auto-reset event.
/// </summary>
internal sealed class AsyncAutoResetEvent : IDisposable
{
    private static readonly Task _completed = Task.CompletedTask;
    private readonly Queue<TaskCompletionSource> _waiters = new();
    private bool _isSignaled;

    public Task WaitAsync(CancellationToken cancellationToken)
    {
        lock (_waiters)
        {
            if (_isSignaled)
            {
                _isSignaled = false;
                return _completed;
            }

            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            cancellationToken.Register(() => tcs.TrySetCanceled(), false);
            _waiters.Enqueue(tcs);
            return tcs.Task;
        }
    }

    public void Set()
    {
        lock (_waiters)
        {
            if (_waiters.Count > 0)
                _waiters.Dequeue().SetResult();
            else if (!_isSignaled) _isSignaled = true;
        }
    }

    public void Dispose()
    {
        // Cancel all pending waiters
        lock (_waiters)
        {
            foreach (var waiter in _waiters) waiter.TrySetCanceled();
            _waiters.Clear();
        }
    }
}