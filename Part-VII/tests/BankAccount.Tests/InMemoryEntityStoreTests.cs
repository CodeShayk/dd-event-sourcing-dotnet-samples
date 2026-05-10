// Chapter 31 — Unit tests for InMemoryEntityStore
#nullable enable

using System.Threading.Tasks;
using BankAccount.Tests.Infrastructure;
using FluentAssertions;
using Xunit;

using BankAccountEntity = BankAccount.Domain.BankAccount;

namespace BankAccount.Tests;

/// <summary>
/// Unit tests for InMemoryEntityStore — verifying its contract independently
/// before it is used as a substitute in saga unit tests.
/// </summary>
public sealed class InMemoryEntityStoreTests
{
    [Fact]
    public async Task Get_ReturnsNull_WhenEntityNotPersisted()
    {
        var store = new InMemoryEntityStore();

        var result = await store.Get<BankAccountEntity>(99);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Persist_ThenGet_ReturnsPersistedEntity()
    {
        var store = new InMemoryEntityStore();
        var account = new BankAccountEntity { Id = 1, Balance = 300m };

        await store.Persist(account);
        var retrieved = await store.Get<BankAccountEntity>(1);

        retrieved.Should().NotBeNull();
        retrieved!.Id.Should().Be(1);
        retrieved.Balance.Should().Be(300m);
    }

    [Fact]
    public async Task Delete_RemovesEntity_SoGetReturnsNull()
    {
        var store = new InMemoryEntityStore();
        var account = new BankAccountEntity { Id = 2, Balance = 100m };
        await store.Persist(account);

        await store.Delete(account);
        var result = await store.Get<BankAccountEntity>(2);

        result.Should().BeNull();
        store.Count.Should().Be(0);
    }
}
