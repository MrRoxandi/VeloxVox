namespace VeloxVox.Models;

/// <summary>
///     Represents configuration options for Text-To-Speech (TTS) synthesis.
///     This is an immutable record.
/// </summary>
public sealed record TtsOptions
{
    /// <summary>
    ///     The name of the voice to use. If empty or null, the system's default voice is used.
    /// </summary>
    public string VoiceName { get; init; } = string.Empty;

    /// <summary>
    ///     The speaking rate (speed) of the voice. Range: -10 (slowest) to 10 (fastest). Default is 0.
    /// </summary>
    public int Rate { get; init; } = 0;

    /// <summary>
    ///     The volume of the voice. Range: 0 (silent) to 100 (loudest). Default is 100.
    /// </summary>
    public int Volume { get; init; } = 100;
}