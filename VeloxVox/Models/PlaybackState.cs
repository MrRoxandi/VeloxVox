namespace VeloxVox.Models;

/// <summary>
///     Represents the current operational state of the audio player.
/// </summary>
public enum PlaybackState
{
    /// <summary>
    ///     The player is not currently playing and is ready to accept a new command.
    ///     This is the default state.
    /// </summary>
    Idle,

    /// <summary>
    ///     The player is actively playing an audio item.
    /// </summary>
    Playing,

    /// <summary>
    ///     The player has received a stop request and is in the process of shutting down the current playback.
    ///     This is a transient state before the player returns to <see cref="Idle" />.
    /// </summary>
    Stopping
}