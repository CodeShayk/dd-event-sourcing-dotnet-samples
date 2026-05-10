// File: BankAccount.Domain/BankAccountService.cs (v4 — thin orchestrator)
#nullable enable

namespace BankAccount.Domain;

/// <summary>
/// Application service for bank account operations.
/// This class is now a thin orchestrator: load the aggregate, delegate to it, save it.
/// All business logic has moved into <see cref="BankAccount"/> where it belongs.
/// </summary>
public class BankAccountService
{
    private readonly IRepository<BankAccount> _repository;

    /// <summary>Initialises the service.</summary>
    public BankAccountService(IRepository<BankAccount> repository)
        => _repository = repository;

    /// <summary>Opens a new account.</summary>
    public async Task<BankAccount> OpenAccount(string accountHolder, AccountNumber accountNumber)
    {
        var account = BankAccount.Open(accountHolder, accountNumber);
        return await _repository.Save(account);
    }

    /// <summary>Credits an account.</summary>
    /// <remarks>
    /// The parameter type is <c>int accountId</c> rather than <c>AccountId</c>
    /// because <see cref="IRepository{T}.GetById"/> uses raw integers. We will align
    /// the repository interface with our value objects in Part V.
    /// </remarks>
    public async Task CreditAccount(int accountId, Money amount, string? reference = null)
    {
        var account = await _repository.GetById(accountId)
            // InvalidOperationException here is correct — 'account not found' is an
            // infrastructure/input failure, not a domain rule violation. Domain rules
            // are enforced inside the aggregate.
            ?? throw new InvalidOperationException($"Account {accountId} not found.");

        account.Credit(amount, reference); // Business logic in the aggregate

        await _repository.Save(account);
    }

    /// <summary>Debits an account.</summary>
    /// <remarks>
    /// The parameter type is <c>int accountId</c> rather than <c>AccountId</c>
    /// because <see cref="IRepository{T}.GetById"/> uses raw integers. We will align
    /// the repository interface with our value objects in Part V.
    /// </remarks>
    public async Task DebitAccount(int accountId, Money amount, string? reference = null)
    {
        var account = await _repository.GetById(accountId)
            // InvalidOperationException here is correct — 'account not found' is an
            // infrastructure/input failure, not a domain rule violation.
            ?? throw new InvalidOperationException($"Account {accountId} not found.");

        account.Debit(amount, reference); // Business logic in the aggregate

        await _repository.Save(account);
    }

    /// <summary>Closes an account.</summary>
    public async Task CloseAccount(int accountId)
    {
        var account = await _repository.GetById(accountId)
            ?? throw new InvalidOperationException($"Account {accountId} not found.");

        account.Close(); // Business logic in the aggregate

        await _repository.Save(account);
    }
}
