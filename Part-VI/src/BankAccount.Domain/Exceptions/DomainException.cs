// File: BankAccount.Domain/Exceptions/DomainException.cs
namespace BankAccount.Domain.Exceptions;

/// <summary>
/// Base class for all domain-level exceptions in the bank account example.
/// Thrown by sagas and aggregates when business invariants are violated.
/// Caught at the API middleware layer and converted to 400 Bad Request responses.
/// </summary>
public class DomainException : Exception
{
    /// <summary>A machine-readable error code for the specific violation.</summary>
    public string ErrorCode { get; }

    /// <summary>
    /// Initialises a new domain exception with a message and error code.
    /// </summary>
    public DomainException(string message, string errorCode)
        : base(message)
    {
        ErrorCode = errorCode;
    }
}

/// <summary>Thrown when an operation is attempted on a closed account.</summary>
public sealed class AccountClosedException : DomainException
{
    public AccountClosedException(int accountId)
        : base($"Account {accountId} is closed and cannot accept transactions.",
               "ACCOUNT_CLOSED") { }
}

/// <summary>Thrown when a withdrawal would result in a negative balance.</summary>
public sealed class InsufficientFundsException : DomainException
{
    public InsufficientFundsException(int accountId, decimal requested, decimal available)
        : base(
            $"Account {accountId} has insufficient funds. " +
            $"Requested: {requested:C}, Available: {available:C}.",
            "INSUFFICIENT_FUNDS")
    { }
}
