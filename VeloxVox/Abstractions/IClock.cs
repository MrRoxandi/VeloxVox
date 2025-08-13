namespace VeloxVox.Abstractions;

/// <summary>
/// Provides an abstraction for time-related operations to enhance testability.
/// </summary>
public interface IClock
{
    /// <summary>
    /// Gets the current Coordinated Universal Time (UTC).
    /// </summary>
    DateTime UtcNow { get; }
}