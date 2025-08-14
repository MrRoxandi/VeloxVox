using VeloxVox.Models;

namespace VeloxVox.Abstractions;

/// <summary>
///     Defines the contract for a thread-safe audio item queue.
/// </summary>
public interface IAudioQueue
{
    /// <summary>
    ///     Gets the number of items currently in the queue.
    /// </summary>
    int Count { get; }

    /// <summary>
    ///     Adds an audio item to the end of the queue.
    /// </summary>
    void Enqueue(AudioItem item);

    /// <summary>
    ///     Tries to remove and return the audio item at the beginning of the queue.
    /// </summary>
    /// <param name="item">The dequeued item, or null if the queue was empty.</param>
    /// <returns>True if an item was successfully dequeued; otherwise, false.</returns>
    bool TryDequeue(out AudioItem? item);

    /// <summary>
    ///     Removes all items from the queue, deleting temporary files associated with them.
    /// </summary>
    void Clear();
}