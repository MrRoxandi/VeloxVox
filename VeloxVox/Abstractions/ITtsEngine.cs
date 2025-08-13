using VeloxVox.Models;

namespace VeloxVox.Abstractions;

/// <summary>
/// Defines the contract for a Text-To-Speech (TTS) synthesis engine.
/// </summary>
public interface ITtsEngine : IAsyncDisposable
{
    /// <summary>
    /// Synthesizes the given text to a temporary audio file asynchronously.
    /// </summary>
    /// <param name="text">The text to synthesize.</param>
    /// <param name="options">The TTS options (voice, rate, volume).</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The path to the generated temporary audio file.</returns>
    ValueTask<string> SynthesizeToFileAsync(string text, TtsOptions options, CancellationToken ct = default);
}