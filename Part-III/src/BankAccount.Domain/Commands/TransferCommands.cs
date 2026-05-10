// BankAccount.Domain/Commands/TransferCommands.cs
using SourceFlow.Messaging;
using SourceFlow.Messaging.Commands;

namespace BankAccount.Domain.Commands;

/// <summary>
/// Payload for TransferMoneyCommand.
/// </summary>
public sealed record TransferMoneyPayload : IPayload
{
    public int SourceAccountId { get; init; }
    public int TargetAccountId { get; init; }
    public decimal Amount { get; init; }
    public TransferMoneyPayload() { }
    public TransferMoneyPayload(int sourceAccountId, int targetAccountId, decimal amount)
    {
        SourceAccountId = sourceAccountId;
        TargetAccountId = targetAccountId;
        Amount = amount;
    }
}

/// <summary>Command to initiate a money transfer between two accounts.</summary>
public sealed class TransferMoneyCommand : Command<TransferMoneyPayload>
{
    public TransferMoneyCommand() : base() { }
    public TransferMoneyCommand(TransferMoneyPayload payload)
        : base(payload.SourceAccountId, payload) { }
}

/// <summary>Payload for DebitAccountCommand.</summary>
public sealed record DebitAccountPayload : IPayload
{
    public decimal Amount { get; init; }
    public Guid TransferId { get; init; }
    public DebitAccountPayload() { }
    public DebitAccountPayload(decimal amount, Guid transferId)
    {
        Amount = amount;
        TransferId = transferId;
    }
}

/// <summary>Command to debit a specific account as part of a transfer operation.</summary>
public sealed class DebitAccountCommand : Command<DebitAccountPayload>
{
    public DebitAccountCommand() : base() { }
    public DebitAccountCommand(int accountId, DebitAccountPayload payload)
        : base(accountId, payload) { }
}

/// <summary>
/// Payload for CreditAccountCommand.
/// Carries SourceAccountId so the compensation handler can route ReverseDebitCommand
/// back to the correct account if the credit fails.
/// </summary>
public sealed record CreditAccountPayload : IPayload
{
    public decimal Amount { get; init; }
    public Guid TransferId { get; init; }
    public int SourceAccountId { get; init; }
    public CreditAccountPayload() { }
    public CreditAccountPayload(decimal amount, Guid transferId, int sourceAccountId)
    {
        Amount = amount;
        TransferId = transferId;
        SourceAccountId = sourceAccountId;
    }
}

/// <summary>Command to credit a specific account as part of a transfer operation.</summary>
public sealed class CreditAccountCommand : Command<CreditAccountPayload>
{
    public CreditAccountCommand() : base() { }
    public CreditAccountCommand(int accountId, CreditAccountPayload payload)
        : base(accountId, payload) { }
}

/// <summary>Payload for ReverseDebitCommand.</summary>
public sealed record ReverseDebitPayload : IPayload
{
    public decimal Amount { get; init; }
    public Guid TransferId { get; init; }
    public ReverseDebitPayload() { }
    public ReverseDebitPayload(decimal amount, Guid transferId)
    {
        Amount = amount;
        TransferId = transferId;
    }
}

/// <summary>Compensating command to reverse a debit when the corresponding credit fails.</summary>
public sealed class ReverseDebitCommand : Command<ReverseDebitPayload>
{
    public ReverseDebitCommand() : base() { }
    public ReverseDebitCommand(int accountId, ReverseDebitPayload payload)
        : base(accountId, payload) { }
}
