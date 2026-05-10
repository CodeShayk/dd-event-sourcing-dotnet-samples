// BankAccount.Domain/BankAccount.cs
using SourceFlow;

namespace BankAccount.Domain;

/// <summary>
/// The BankAccount aggregate root.
/// Maintains the account's current state: balance, holder, active status.
/// All mutations are applied by saga handlers — this class exposes the state
/// and the domain invariants, not the commands that change it.
/// </summary>
public class BankAccount : IEntity
{
    /// <inheritdoc />
    public int Id { get; set; }

    /// <summary>The name of the account holder.</summary>
    public string HolderName { get; set; } = string.Empty;

    /// <summary>The current balance.</summary>
    public decimal Balance { get; set; }

    /// <summary>Whether the account is active. False if closed.</summary>
    public bool IsActive { get; set; }

    /// <summary>The date the account was opened.</summary>
    public DateTimeOffset OpenedOn { get; set; }
}
