using LibVLCSharp.Shared;
using VeloxVox.Abstractions;
using VeloxVox.Models;

namespace VeloxVox.Services;

/// <summary>
/// An audio player implementation using LibVLCSharp, configured for audio-only (headless) playback.
/// </summary>
internal sealed class VlcAudioPlayer : IAudioPlayer
{
    private LibVLC? _vlc;
    private MediaPlayer? _mediaPlayer;
    private volatile bool _isEventTriggered = false;

    /// <inheritdoc/>
    public event AsyncEventHandler<PlaybackCompletedEventArgs>? PlaybackCompleted;

    /// <inheritdoc/>
    public event AsyncEventHandler<PlaybackErrorEventArgs>? PlaybackError;

    /// <inheritdoc/>
    public PlaybackState State { get; private set; } = PlaybackState.Idle;

    /// <inheritdoc/>
    public AudioItem? CurrentItem { get; private set; }


    /// <inheritdoc/>
    public ValueTask InitializeAsync(CancellationToken ct = default)
    {
        Core.Initialize();

        // ============================================================================
        // ** KEY CHANGE FOR HEADLESS OPERATION **
        // We pass arguments to LibVLC to explicitly disable video output.
        // --no-video: Disables the video pipeline entirely.
        // --vout=dummy: Uses a dummy video output, which does nothing.
        // This guarantees that no window will ever be created, even if a video file is played.
        // ============================================================================
        var vlcOptions = new string[]
        {
            "--no-video",
            "--vout=dummy"
        };

        _vlc = new LibVLC(enableDebugLogs: false, vlcOptions);
        _mediaPlayer = new MediaPlayer(_vlc);

        _mediaPlayer.EndReached += OnEndReached;
        _mediaPlayer.EncounteredError += OnEncounteredError;
        _mediaPlayer.Stopped += OnStopped;
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc/>
    public ValueTask PlayAsync(AudioItem item, CancellationToken ct = default)
    {
        if (_mediaPlayer is null || _vlc is null)
            throw new InvalidOperationException("Player is not initialized. Call InitializeAsync() first.");

        ct.ThrowIfCancellationRequested();

        _isEventTriggered = false;
        State = PlaybackState.Playing;
        CurrentItem = item;

        // The Media object needs to be disposed, 'using' is the best practice.
        using var media = new Media(_vlc, new Uri(item.SourcePath));
        if (!_mediaPlayer.Play(media))
        {
            State = PlaybackState.Idle;
            CurrentItem = null;
            var ex = new InvalidOperationException($"LibVLC failed to start playback for {item.SourcePath}.");
            // Fire the error event asynchronously without awaiting
            _ = PlaybackError?.Invoke(this, new PlaybackErrorEventArgs(item, ex));
        }
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc/>
    public ValueTask StopAsync(CancellationToken ct = default)
    {
        if (_mediaPlayer is null || State == PlaybackState.Idle)
        {
            return ValueTask.CompletedTask;
        }

        State = PlaybackState.Stopping;
        _mediaPlayer.Stop(); // This will trigger the OnStopped event
        return ValueTask.CompletedTask;
    }

    private void OnEndReached(object? sender, EventArgs e) => HandlePlaybackEnd(CompletionReason.Finished);
    private void OnStopped(object? sender, EventArgs e) => HandlePlaybackEnd(CompletionReason.Skipped);

    private void HandlePlaybackEnd(CompletionReason reason)
    {
        if (_isEventTriggered) return;
        _isEventTriggered = true;

        var item = CurrentItem;
        State = PlaybackState.Idle;
        CurrentItem = null;

        if (item is null) return;

        _ = PlaybackCompleted?.Invoke(this, new PlaybackCompletedEventArgs(item, reason));

        if (item.IsTemporaryFile)
        {
            TryDeleteTempFile(item.SourcePath);
        }
    }

    private void OnEncounteredError(object? sender, EventArgs e)
    {
        if (_isEventTriggered) return;
        _isEventTriggered = true;

        var item = CurrentItem;
        State = PlaybackState.Idle;
        CurrentItem = null;

        if (item is null) return;

        var ex = new InvalidOperationException("LibVLC encountered an unspecified error during playback.");

        _ = PlaybackError?.Invoke(this, new PlaybackErrorEventArgs(item, ex));

        if (item.IsTemporaryFile)
        {
            TryDeleteTempFile(item.SourcePath);
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_mediaPlayer != null)
        {
            _mediaPlayer.EndReached -= OnEndReached;
            _mediaPlayer.EncounteredError -= OnEncounteredError;
            _mediaPlayer.Stopped -= OnStopped;

            if (_mediaPlayer.IsPlaying)
            {
                _mediaPlayer.Stop();
                await Task.Delay(100); // Give it a moment to stop
            }
        }
        _mediaPlayer?.Dispose();
        _vlc?.Dispose();
        _mediaPlayer = null;
        _vlc = null;
    }

    private void TryDeleteTempFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch {}
    }
}