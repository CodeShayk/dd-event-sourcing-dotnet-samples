// File: BankAccount.Domain/BankAccount.cs (v5 — with domain event raising)
#nullable enable

using BankAccount.Domain.Events;

namespace BankAccount.Domain;

/// <summary>
/// The BankAccount aggregate root, now with domain event raising.
/// Implements <see cref="IHasDomainEvents"/> so that the event dispatcher
/// can service this aggregate without being coupled to its concrete type.
/// After each state-changing operation, the aggregate records a domain event.
/// These events are collected in <see cref="DomainEvents"/> and dispatched
/// by the application layer after the aggregate has been persisted.
/// </summary>
public class BankAccount : IHasDomainEvents
{
    private readonly List<TransactionLine> _transactions = new();
    private readonly List<IDomainEvent> _domainEvents = new();

    internal int RawId { get; private set; }

    /// <summary>The domain identity of this account.</summary>
    public AccountId Id => RawId <= 0 ? AccountId.Transient() : AccountId.From(RawId);

    /// <summary>The account number.</summary>
    public AccountNumber AccountNumber { get; private set; } = default!;

    /// <summary>The account holder's name.</summary>
    public string AccountHolder { get; private set; } = string.Empty;

    /// <summary>The current cleared balance.</summary>
    public Money Balance { get; private set; } = Money.Zero();

    /// <summary>Whether the account is active.</summary>
    public bool IsActive { get; private set; }

    /// <summary>When the account was opened.</summary>
    public DateTime OpenedOn { get; private set; }

    /// <summary>The transaction history.</summary>
    public IReadOnlyList<TransactionLine> Transactions => _transactions.AsReadOnly();

    /// <summary>
    /// Domain events raised by this aggregate since the last time they were cleared.
    /// The application layer reads these after persisting the aggregate and dispatches them
    /// to any interested subscribers (notifications, audit logs, read model projectors).
    /// </summary>
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Clears all pending domain events.
    /// Called by the application layer after events have been dispatched.
    /// </summary>
    public void ClearDomainEvents() => _domainEvents.Clear();

    /// <summary>Protected parameterless constructor for EF Core.</summary>
    protected BankAccount() { }

    /// <summary>
    /// Opens a new bank account and raises an <see cref="AccountOpened"/> event.
    /// </summary>
    public static BankAccount Open(string accountHolder, AccountNumber accountNumber)
    {
        if (string.IsNullOrWhiteSpace(accountHolder))
            throw new DomainException("Account holder name cannot be empty.");

        var account = new BankAccount
        {
            AccountHolder = accountHolder,
            AccountNumber = accountNumber,
            Balance = Money.Zero(),
            IsActive = true,
            OpenedOn = DateTime.UtcNow
        };

        // The AggregateId is 0 here because the account has not been persisted yet
        // and has no database-assigned integer identity. This is a genuine limitation
        // of integer-identity aggregates: the event is raised before we know the ID.
        // In Part II we will see how event sourcing systems address this, typically by
        // separating the event stream identity from the persistence identity — often
        // using a client-generated GUID as the aggregate identity so that the ID is
        // known at creation time, before any database round-trip.
        account._domainEvents.Add(new AccountOpened(
            AggregateId: 0,    // Transient — not yet assigned
            AccountHolder: accountHolder,
            AccountNumber: accountNumber.Value,
            OccurredOn: DateTime.UtcNow));

        return account;
    }

    /// <summary>
    /// Credits this account and raises an <see cref="AccountCredited"/> event.
    /// </summary>
    public void Credit(Money amount, string? reference = null)
    {
        if (!IsActive)
            throw new AccountNotActiveException(AccountNumber.Value);

        if (!amount.IsPositive)
            throw new DomainException($"Credit amount must be positive. Received: {amount}");

        Balance += amount;
        _transactions.Add(TransactionLine.ForCredit(amount, DateTime.UtcNow, reference));

        _domainEvents.Add(new AccountCredited(
            AggregateId: RawId,
            Amount: amount,
            NewBalance: Balance,
            Reference: reference,
            OccurredOn: DateTime.UtcNow));
    }

    /// <summary>
    /// Debits this account and raises an <see cref="AccountDebited"/> event.
    /// </summary>
    public void Debit(Money amount, string? reference = null)
    {
        if (!IsActive)
            throw new AccountNotActiveException(AccountNumber.Value);

        if (!amount.IsPositive)
            throw new DomainException($"Debit amount must be positive. Received: {amount}");

        if ((Balance - amount).IsNegative)
            throw new InsufficientFundsException(Balance, amount);

        Balance -= amount;
        _transactions.Add(TransactionLine.ForDebit(amount, DateTime.UtcNow, reference));

        _domainEvents.Add(new AccountDebited(
            AggregateId: RawId,
            Amount: amount,
            NewBalance: Balance,
            Reference: reference,
            OccurredOn: DateTime.UtcNow));
    }

    /// <summary>
    /// Closes this account and raises an <see cref="AccountClosed"/> event.
    /// Closing an already-closed account is idempotent and raises no event.
    /// </summary>
    public void Close()
    {
        if (!IsActive) return;
        IsActive = false;

        _domainEvents.Add(new AccountClosed(
            AggregateId: RawId,
            OccurredOn: DateTime.UtcNow));
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
        => obj is BankAccount other && RawId != 0 && RawId == other.RawId;

    /// <inheritdoc/>
    public override int GetHashCode() => RawId.GetHashCode();

    /// <summary>Equality operator.</summary>
    public static bool operator ==(BankAccount? left, BankAccount? right) => Equals(left, right);

    /// <summary>Inequality operator.</summary>
    public static bool operator !=(BankAccount? left, BankAccount? right) => !Equals(left, right);
}
