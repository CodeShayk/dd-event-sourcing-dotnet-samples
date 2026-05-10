// Chapter 17
using SourceFlow.Messaging.Events;
using BankAccountEntity = BankAccount.Domain.BankAccount;

namespace BankAccount.Domain.Events;

public sealed class AccountClosed : Event<BankAccountEntity>
{
    public AccountClosed(BankAccountEntity account) : base(account) { }
}
