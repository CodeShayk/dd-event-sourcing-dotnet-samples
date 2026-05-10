// Chapter 17
using SourceFlow.Messaging.Events;
using BankAccountEntity = BankAccount.Domain.BankAccount;

namespace BankAccount.Domain.Events;

public sealed class MoneyWithdrawn : Event<BankAccountEntity>
{
    public MoneyWithdrawn(BankAccountEntity account) : base(account) { }
}
