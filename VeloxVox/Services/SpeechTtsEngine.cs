using System.Runtime.Versioning;
using System.Speech.Synthesis;
using VeloxVox.Abstractions;
using VeloxVox.Models;

namespace VeloxVox.Services;

/// <summary>
///     A TTS engine implementation using the built-in System.Speech.Synthesis.
/// </summary>
[SupportedOSPlatform("windows")]
internal sealed class SpeechTtsEngine : ITtsEngine
{
    private SpeechSynthesizer? _synth = new();
    private bool _disposed;

    /// <inheritdoc />
    public async ValueTask<string> SynthesizeToFileAsync(string text, TtsOptions options,
        CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ct.ThrowIfCancellationRequested();

        // The core synthesis operation (Speak) is synchronous. We wrap it in Task.Run
        // to avoid blocking the caller thread, ensuring a truly async experience.
        return await Task.Run(() =>
        {
            ct.ThrowIfCancellationRequested();

            // Note: We create a new synthesizer per call to ensure thread safety
            // when multiple TTS requests happen concurrently.
            using var synthesizer = new SpeechSynthesizer();

            if (!string.IsNullOrWhiteSpace(options.VoiceName))
                try
                {
                    synthesizer.SelectVoice(options.VoiceName);
                }
                catch (ArgumentException)
                {
                    // Voice may not exist; we'll proceed with the default voice.
                }

            synthesizer.Rate = Math.Clamp(options.Rate, -10, 10);
            synthesizer.Volume = Math.Clamp(options.Volume, 0, 100);

            var tempFilePath = Path.Combine(Path.GetTempPath(), $"veloxvox_tts_{Guid.NewGuid():N}.wav");

            // This needs to be done on the same thread.
            synthesizer.SetOutputToWaveFile(tempFilePath);
            synthesizer.Speak(text);
            synthesizer.SetOutputToNull(); // Release the file handle

            return tempFilePath;
        }, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        if (_disposed) return ValueTask.CompletedTask;

        _synth?.Dispose();
        _synth = null;
        _disposed = true;

        return ValueTask.CompletedTask;
    }
}