// File: BankAccount.Domain/Exceptions.cs
#nullable enable

namespace BankAccount.Domain;

/// <summary>
/// Base class for all domain-layer exceptions in the Bank Account context.
/// Catching <see cref="DomainException"/> catches any business rule violation.
/// </summary>
public class DomainException : Exception
{
    /// <summary>Creates a new domain exception with the given message.</summary>
    public DomainException(string message) : base(message) { }

    /// <summary>Creates a new domain exception with the given message and inner exception.</summary>
    public DomainException(string message, Exception innerException)
        : base(message, innerException) { }
}

/// <summary>
/// Raised when a debit operation cannot be completed because the account
/// balance would fall below the permitted minimum.
/// </summary>
public sealed class InsufficientFundsException : DomainException
{
    /// <summary>The current balance at the time of the failed transaction.</summary>
    public Money CurrentBalance { get; }

    /// <summary>The amount that was requested.</summary>
    public Money RequestedAmount { get; }

    /// <summary>
    /// Creates a new <see cref="InsufficientFundsException"/>.
    /// </summary>
    public InsufficientFundsException(Money currentBalance, Money requestedAmount)
        : base($"Insufficient funds. Balance is {currentBalance}, requested {requestedAmount}.")
    {
        CurrentBalance = currentBalance;
        RequestedAmount = requestedAmount;
    }
}

/// <summary>
/// Raised when an operation is attempted on an account that is not in an active state.
/// </summary>
public sealed class AccountNotActiveException : DomainException
{
    /// <summary>The account number of the closed account.</summary>
    public string AccountNumber { get; }

    /// <summary>Creates a new <see cref="AccountNotActiveException"/>.</summary>
    public AccountNotActiveException(string accountNumber)
        : base($"Account {accountNumber} is not active and cannot accept transactions.")
    {
        AccountNumber = accountNumber;
    }
}
