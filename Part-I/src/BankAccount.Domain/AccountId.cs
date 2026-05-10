// File: BankAccount.Domain/AccountId.cs
#nullable enable

namespace BankAccount.Domain;

/// <summary>
/// The identity of a bank account.
/// An AccountId is a Value Object — two AccountIds with the same Value are equal.
/// The distinction between a transient (not yet persisted) account and a persisted
/// account is captured by <see cref="IsTransient"/> rather than by checking for zero
/// throughout the codebase.
/// </summary>
/// <param name="Value">The underlying integer identifier. Zero represents a transient account.</param>
public record AccountId(int Value)
{
    /// <summary>
    /// Returns true if this AccountId has not yet been assigned by the persistence layer.
    /// A transient account has not been saved and does not have a database-generated identifier.
    /// </summary>
    public bool IsTransient => Value <= 0;

    /// <summary>
    /// Creates an AccountId representing a new, not-yet-persisted account.
    /// </summary>
    public static AccountId Transient() => new(0);

    /// <summary>
    /// Creates an AccountId from a known persisted value.
    /// </summary>
    /// <param name="value">The persisted identifier. Must be positive.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if value is not positive.</exception>
    public static AccountId From(int value)
    {
        if (value <= 0)
            throw new ArgumentOutOfRangeException(nameof(value),
                $"A persisted AccountId must be positive. Received: {value}");
        return new(value);
    }

    /// <summary>Returns a string representation, e.g. "AccountId(42)".</summary>
    public override string ToString() => IsTransient ? "AccountId(transient)" : $"AccountId({Value})";
}
