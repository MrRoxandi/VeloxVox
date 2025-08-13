namespace VeloxVox.Models;

/// <summary>
/// Represents an item to be played, which can be a permanent file or a temporary TTS result.
/// This is an immutable record.
/// </summary>
/// <param name="SourcePath">The absolute path to the audio file.</param>
/// <param name="IsTemporaryFile">Indicates if the file should be deleted after playback.</param>
public sealed record AudioItem(string SourcePath, bool IsTemporaryFile)
{
    /// <summary>
    /// Creates an AudioItem from an existing, persistent file.
    /// </summary>
    /// <param name="path">The path to the audio file.</param>
    /// <returns>A new AudioItem instance.</returns>
    public static AudioItem FromFile(string path) => new(path, false);

    /// <summary>
    /// Creates an AudioItem from a temporary file (e.g., from TTS) that will be deleted after use.
    /// </summary>
    /// <param name="path">The path to the temporary audio file.</param>
    /// <returns>A new AudioItem instance.</returns>
    public static AudioItem FromTempFile(string path) => new(path, true);

    /// <summary>
    /// Creates an AudioItem from a network URL (e.g., HTTP stream or direct link to an audio file).
    /// </summary>
    /// <param name="url">The URL of the audio stream.</param>
    /// <returns>A new AudioItem instance.</returns>
    /// <exception cref="ArgumentException">Thrown if the provided string is not a valid, absolute URL.</exception>
    public static AudioItem FromUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out _))
        {
            throw new ArgumentException($"The provided string is not a valid absolute URL: {url}", nameof(url));
        }
        return new(url, false);
    }
}