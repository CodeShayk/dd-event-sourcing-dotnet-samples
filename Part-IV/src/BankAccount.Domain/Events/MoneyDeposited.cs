// Chapter 17
using SourceFlow.Messaging.Events;
using BankAccountEntity = BankAccount.Domain.BankAccount;

namespace BankAccount.Domain.Events;

public sealed class MoneyDeposited : Event<BankAccountEntity>
{
    public MoneyDeposited(BankAccountEntity account) : base(account) { }
}
