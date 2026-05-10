// BankAccount.Domain/Commands/Payloads.cs
using SourceFlow.Messaging;

namespace BankAccount.Domain.Commands;

/// <summary>
/// Payload for the OpenAccountCommand.
/// Carries the account holder's name and the initial deposit amount.
/// </summary>
/// <param name="HolderName">The full name of the account holder.</param>
/// <param name="InitialDeposit">The opening balance. Must be greater than zero.</param>
public sealed record OpenAccountPayload : IPayload
{
    public string HolderName { get; init; } = string.Empty;
    public decimal InitialDeposit { get; init; }
    public OpenAccountPayload() { }
    public OpenAccountPayload(string holderName, decimal initialDeposit)
    {
        HolderName = holderName;
        InitialDeposit = initialDeposit;
    }
}

/// <summary>
/// Payload for the DepositMoneyCommand.
/// Carries the amount to credit to the account.
/// </summary>
public sealed record DepositMoneyPayload : IPayload
{
    public decimal Amount { get; init; }
    public string? Reference { get; init; }
    public DepositMoneyPayload() { }
    public DepositMoneyPayload(decimal amount, string? reference = null)
    {
        Amount = amount;
        Reference = reference;
    }
}

/// <summary>
/// Payload for the WithdrawMoneyCommand.
/// Carries the amount to debit from the account.
/// </summary>
public sealed record WithdrawMoneyPayload : IPayload
{
    public decimal Amount { get; init; }
    public string? Reference { get; init; }
    public WithdrawMoneyPayload() { }
    public WithdrawMoneyPayload(decimal amount, string? reference = null)
    {
        Amount = amount;
        Reference = reference;
    }
}

/// <summary>
/// Payload for the CloseAccountCommand.
/// Closing an account requires no business data beyond the account ID.
/// </summary>
public sealed record CloseAccountPayload : IPayload;
