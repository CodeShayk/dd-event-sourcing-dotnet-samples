// BankAccount.Infrastructure/SnapshotPolicy.cs
#nullable enable

namespace BankAccount.Infrastructure;

/// <summary>
/// Determines when a snapshot should be taken for a BankAccount.
/// This implementation uses a count-based policy: a snapshot is triggered
/// every N commands. The N value is configurable.
/// </summary>
public sealed class SnapshotPolicy
{
    private readonly int _snapshotEveryN;

    /// <summary>
    /// Initialises the policy with the specified snapshot frequency.
    /// </summary>
    /// <param name="snapshotEveryN">
    /// A snapshot is taken when SequenceNo is a multiple of this value.
    /// Defaults to 100 (snapshot every 100 commands).
    /// </param>
    public SnapshotPolicy(int snapshotEveryN = 100)
    {
        if (snapshotEveryN <= 0)
            throw new ArgumentOutOfRangeException(nameof(snapshotEveryN),
                "Snapshot frequency must be a positive integer.");
        _snapshotEveryN = snapshotEveryN;
    }

    /// <summary>
    /// Returns true if a snapshot should be taken after appending the command
    /// with the specified sequence number.
    /// </summary>
    public bool ShouldSnapshot(int sequenceNo) => sequenceNo % _snapshotEveryN == 0;
}
