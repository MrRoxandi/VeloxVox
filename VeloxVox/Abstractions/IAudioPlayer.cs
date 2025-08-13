using VeloxVox.Models;

namespace VeloxVox.Abstractions;

/// <summary>
/// Defines the contract for an audio playback backend.
/// </summary>
public interface IAudioPlayer : IAsyncDisposable
{
    /// <summary>
    /// Fired when playback of an audio item completes successfully.
    /// </summary>
    event AsyncEventHandler<PlaybackCompletedEventArgs>? PlaybackCompleted;

    /// <summary>
    /// Fired when an error occurs during playback.
    /// </summary>
    event AsyncEventHandler<PlaybackErrorEventArgs>? PlaybackError;

    /// <summary>
    /// Gets the current state of the player.
    /// </summary>
    PlaybackState State { get; }

    /// <summary>
    /// Gets the item currently being played, or null if idle.
    /// </summary>
    AudioItem? CurrentItem { get; }

    /// <summary>
    /// Initializes the audio player resources asynchronously.
    /// </summary>
    ValueTask InitializeAsync(CancellationToken ct = default);

    /// <summary>
    /// Plays the specified audio item asynchronously.
    /// </summary>
    ValueTask PlayAsync(AudioItem item, CancellationToken ct = default);

    /// <summary>
    /// Stops the current playback asynchronously.
    /// </summary>
    ValueTask StopAsync(CancellationToken ct = default);
}

/// <summary>
/// Represents an asynchronous event handler.
/// </summary>
public delegate Task AsyncEventHandler<TEventArgs>(object sender, TEventArgs e);