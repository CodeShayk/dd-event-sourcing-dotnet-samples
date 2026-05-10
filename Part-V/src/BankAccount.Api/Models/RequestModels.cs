// File: BankAccount.Api/Models/RequestModels.cs
namespace BankAccount.Api.Models;

/// <summary>Request body for opening a new account.</summary>
public sealed record OpenAccountRequest(
    string AccountNumber,
    string AccountHolder,
    decimal InitialBalance);

/// <summary>Request body for a deposit.</summary>
public sealed record DepositRequest(decimal Amount, string Description);

/// <summary>Request body for a withdrawal.</summary>
public sealed record WithdrawRequest(decimal Amount, string Description);

/// <summary>Request body for closing an account.</summary>
public sealed record CloseAccountRequest(string Reason);
