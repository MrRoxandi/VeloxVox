using VeloxVox.Abstractions;
namespace VeloxVox.Models;

/// <summary>
///     Base class for all VeloxVox engine events.
/// </summary>
public abstract class VeloxVoxEventArgs : EventArgs
{
    /// <summary>
    ///     The UTC timestamp when the event was generated.
    /// </summary>
    public DateTime TimestampUtc { get; } = DateTime.UtcNow;
}

/// <summary>
///     Provides data for the <see cref="VeloxVoxEngine.QueueItemStarted" /> event.
/// </summary>
public sealed class QueueItemStartedEventArgs(AudioItem item) : VeloxVoxEventArgs
{
    /// <summary>
    ///     The audio item that started playing.
    /// </summary>
    public AudioItem Item { get; } = item;
}

/// <summary>
///     Provides data for the <see cref="VeloxVoxEngine.QueueItemCompleted" /> event.
/// </summary>
public sealed class QueueItemCompletedEventArgs(AudioItem item, CompletionReason reason) : VeloxVoxEventArgs
{
    /// <summary>
    ///     The audio item that finished playing.
    /// </summary>
    public AudioItem Item { get; } = item;

    /// <summary>
    ///     The reason for completion.
    /// </summary>
    public CompletionReason Reason { get; } = reason;
}

/// <summary>
///     Provides data for the <see cref="VeloxVoxEngine.QueueItemFailed" /> event.
/// </summary>
public sealed class QueueItemFailedEventArgs(AudioItem item, Exception exception) : VeloxVoxEventArgs
{
    /// <summary>
    ///     The audio item that failed to play.
    /// </summary>
    public AudioItem Item { get; } = item;

    /// <summary>
    ///     The exception that caused the failure.
    /// </summary>
    public Exception Exception { get; } = exception;
}

/// <summary>
///     Provides data for the <see cref="VeloxVoxEngine.QueueEmpty" /> event.
/// </summary>
public sealed class QueueEmptyEventArgs : VeloxVoxEventArgs
{
}

/// <summary>
///     Represents the reason why an audio item playback was completed.
/// </summary>
public enum CompletionReason
{
    /// <summary>
    ///     The item played to its natural end.
    /// </summary>
    Finished,

    /// <summary>
    ///     The item was stopped by a user request (e.g., skip).
    /// </summary>
    Skipped,

    /// <summary>
    ///     The item was stopped because the engine is shutting down.
    /// </summary>
    Shutdown
}

/// <summary>
///     Provides data for the <see cref="IAudioPlayer.PlaybackCompleted" /> event.
///     Part of the player extensibility contract.
/// </summary>
public sealed class PlaybackCompletedEventArgs(AudioItem item, CompletionReason reason) : EventArgs
{
    /// <summary>The item that completed playback.</summary>
    public AudioItem Item { get; } = item;

    /// <summary>The reason for completion.</summary>
    public CompletionReason Reason { get; } = reason;
}

/// <summary>
///     Provides data for the <see cref="IAudioPlayer.PlaybackError" /> event.
///     Part of the player extensibility contract.
/// </summary>
public sealed class PlaybackErrorEventArgs(AudioItem item, Exception ex) : EventArgs
{
    /// <summary>The item that failed.</summary>
    public AudioItem Item { get; } = item;

    /// <summary>The exception that occurred.</summary>
    public Exception Exception { get; } = ex;
}