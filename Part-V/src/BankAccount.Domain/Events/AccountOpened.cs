// Chapter 17
using SourceFlow.Messaging.Events;
using BankAccountEntity = BankAccount.Domain.BankAccount;

namespace BankAccount.Domain.Events;

public sealed class AccountOpened : Event<BankAccountEntity>
{
    public AccountOpened(BankAccountEntity account) : base(account) { }
}
