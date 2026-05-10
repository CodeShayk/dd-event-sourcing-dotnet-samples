// Chapter 17-21 — Commands for the BankAccount domain (Part IV evolution)
using SourceFlow.Messaging;
using SourceFlow.Messaging.Commands;

namespace BankAccount.Domain.Commands;

// --- Commands ---

public sealed class OpenAccountCommand : Command<OpenAccountPayload>
{
    public OpenAccountCommand() { }
    public OpenAccountCommand(bool newEntity, OpenAccountPayload payload)
        : base(newEntity, payload) { }
}

public sealed class DepositMoneyCommand : Command<DepositPayload>
{
    public DepositMoneyCommand() { }
    public DepositMoneyCommand(int entityId, DepositPayload payload)
        : base(entityId, payload) { }
}

public sealed class WithdrawMoneyCommand : Command<WithdrawPayload>
{
    public WithdrawMoneyCommand() { }
    public WithdrawMoneyCommand(int entityId, WithdrawPayload payload)
        : base(entityId, payload) { }
}

public sealed class CloseAccountCommand : Command<CloseAccountPayload>
{
    public CloseAccountCommand() { }
    public CloseAccountCommand(int entityId, CloseAccountPayload payload)
        : base(entityId, payload) { }
}

// --- Payloads ---

public sealed record OpenAccountPayload : IPayload
{
    public string AccountNumber { get; init; } = string.Empty;
    public string AccountHolder { get; init; } = string.Empty;
    public decimal InitialBalance { get; init; }

    public OpenAccountPayload() { }
    public OpenAccountPayload(string accountNumber, string accountHolder, decimal initialBalance)
    {
        AccountNumber = accountNumber;
        AccountHolder = accountHolder;
        InitialBalance = initialBalance;
    }
}

public sealed record DepositPayload : IPayload
{
    public decimal Amount { get; init; }
    public string Description { get; init; } = string.Empty;

    public DepositPayload() { }
    public DepositPayload(decimal amount, string description)
    {
        Amount = amount;
        Description = description;
    }
}

public sealed record WithdrawPayload : IPayload
{
    public decimal Amount { get; init; }
    public string Description { get; init; } = string.Empty;

    public WithdrawPayload() { }
    public WithdrawPayload(decimal amount, string description)
    {
        Amount = amount;
        Description = description;
    }
}

public sealed record CloseAccountPayload : IPayload
{
    public string Reason { get; init; } = string.Empty;

    public CloseAccountPayload() { }
    public CloseAccountPayload(string reason)
    {
        Reason = reason;
    }
}
