// BankAccount.Tests/SequencingTests.cs
#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BankAccount.Commands;
using BankAccount.Domain;
using BankAccount.Infrastructure;
using FluentAssertions;
using Xunit;

namespace BankAccount.Tests;

/// <summary>
/// Tests for sequence number assignment under concurrent load.
/// These tests verify the core invariant: every command for a given entity
/// receives a unique, gap-free sequence number even when multiple commands
/// are appended simultaneously.
/// </summary>
public sealed class SequencingTests : IDisposable
{
    private readonly InMemoryCommandLog _log = new();

    [Fact]
    public async Task ConcurrentAppends_SameAccount_AllReceiveDistinctSequenceNumbers()
    {
        // Arrange
        const int accountId = 42;
        const int concurrentDeposits = 10;

        var commands = Enumerable.Range(1, concurrentDeposits)
            .Select(i => new DepositMoneyCommand(accountId, i * 10m))
            .ToList();

        // Act — fire all appends simultaneously.
        var tasks = commands.Select(cmd => _log.AppendAsync(accountId, cmd));
        await Task.WhenAll(tasks);

        // Assert
        var records = _log.Load(accountId);

        records.Should().HaveCount(concurrentDeposits,
            "every concurrent append must succeed without data loss");

        var sequenceNumbers = records.Select(r => r.SequenceNo).ToList();

        sequenceNumbers.Should().OnlyHaveUniqueItems(
            "no two commands for the same account may share a sequence number");

        sequenceNumbers.Should().BeEquivalentTo(
            Enumerable.Range(1, concurrentDeposits),
            "sequence numbers must be gap-free from 1 to N");
    }

    [Fact]
    public async Task ConcurrentAppends_DifferentAccounts_DoNotInterfereWithEachOther()
    {
        // Arrange — two accounts each receiving five concurrent commands.
        const int account1 = 1;
        const int account2 = 2;
        const int depositsPerAccount = 5;

        var account1Tasks = Enumerable.Range(1, depositsPerAccount)
            .Select(i => _log.AppendAsync(account1, new DepositMoneyCommand(account1, i * 10m)));

        var account2Tasks = Enumerable.Range(1, depositsPerAccount)
            .Select(i => _log.AppendAsync(account2, new DepositMoneyCommand(account2, i * 20m)));

        // Act — all ten appends fire simultaneously across both accounts.
        await Task.WhenAll(account1Tasks.Concat(account2Tasks));

        // Assert
        var account1Records = _log.Load(account1);
        var account2Records = _log.Load(account2);

        account1Records.Should().HaveCount(depositsPerAccount);
        account2Records.Should().HaveCount(depositsPerAccount);

        // Each account's sequence numbers must be independently gap-free.
        account1Records.Select(r => r.SequenceNo).Should()
            .BeEquivalentTo(Enumerable.Range(1, depositsPerAccount),
                "account 1 sequence numbers are independent of account 2");

        account2Records.Select(r => r.SequenceNo).Should()
            .BeEquivalentTo(Enumerable.Range(1, depositsPerAccount),
                "account 2 sequence numbers are independent of account 1");
    }

    [Fact]
    public async Task PeekNextSequenceNo_AfterConcurrentAppends_ReturnsCorrectNextValue()
    {
        // Arrange — append 5 commands concurrently.
        const int accountId = 7;
        const int concurrentDeposits = 5;

        var tasks = Enumerable.Range(1, concurrentDeposits)
            .Select(i => _log.AppendAsync(accountId, new DepositMoneyCommand(accountId, i * 10m)));
        await Task.WhenAll(tasks);

        // Act
        int nextSeqNo = _log.PeekNextSequenceNo(accountId);

        // Assert — after 5 commands, the next available sequence number must be 6.
        nextSeqNo.Should().Be(concurrentDeposits + 1,
            $"after {concurrentDeposits} appended commands, PeekNextSequenceNo must return {concurrentDeposits + 1}");
    }

    public void Dispose() => _log.Dispose();
}
