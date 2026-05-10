// File: BankAccount.Domain/AccountsContext/AccountHolder.cs
#nullable enable

namespace BankAccount.Domain.AccountsContext;

/// <summary>
/// The Accounts context's model of an account holder.
/// This is NOT a customer in the CRM sense — it is specifically the person
/// who holds a bank account in our Accounts bounded context.
/// It contains only the information the Accounts context needs.
/// </summary>
/// <param name="FullName">The account holder's full legal name.</param>
/// <param name="AccountNumber">The account number associated with this holder.</param>
public record AccountHolder(string FullName, string AccountNumber);
