using VeloxVox.Abstractions;

namespace VeloxVox.Services;

/// <summary>
/// Default implementation of <see cref="IClock"/> using <see cref="DateTime.UtcNow"/>.
/// </summary>
internal sealed class SystemClock : IClock
{
    /// <inheritdoc/>
    public DateTime UtcNow => DateTime.UtcNow;
}