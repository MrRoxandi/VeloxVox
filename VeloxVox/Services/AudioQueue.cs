using System.Collections.Concurrent;
using VeloxVox.Abstractions;
using VeloxVox.Models;

namespace VeloxVox.Services;

/// <summary>
///     Default thread-safe implementation of the audio queue.
/// </summary>
internal sealed class AudioQueue : IAudioQueue
{
    private readonly ConcurrentQueue<AudioItem> _queue = new();

    public int Count => _queue.Count;

    public void Enqueue(AudioItem item) => _queue.Enqueue(item);

    public bool TryDequeue(out AudioItem? item) => _queue.TryDequeue(out item);

    public void Clear()
    {
        while (_queue.TryDequeue(out var item))
            if (item is { IsTemporaryFile: true })
                TryDeleteTempFile(item.SourcePath);
    }

    private void TryDeleteTempFile(string path)
    {
        try
        {
            if (File.Exists(path)) File.Delete(path);
        }
        catch
        {
            // ignored
        }
    }
}