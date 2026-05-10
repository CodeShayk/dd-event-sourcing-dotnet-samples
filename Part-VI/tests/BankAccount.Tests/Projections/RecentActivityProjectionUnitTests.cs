// File: BankAccount.Tests/Projections/RecentActivityProjectionUnitTests.cs
using FluentAssertions;
using BankAccount.Domain.Views;
using Xunit;

namespace BankAccount.Tests.Projections;

/// <summary>
/// Unit tests for the sliding-window list manipulation logic used by
/// RecentActivityProjection. These tests exercise the pure list operations
/// without any framework infrastructure.
/// </summary>
public sealed class RecentActivityProjectionUnitTests
{
    [Fact]
    public void Insert_ShouldPlaceNewEntryAtFront_WhenWindowNotFull()
    {
        // Arrange
        var view = new RecentActivityView
        {
            Id         = 1,
            Activities = [new RecentActivityEntry { EventType = "AccountOpened", Summary = "Opened" }]
        };
        var newEntry = new RecentActivityEntry
        {
            EventType  = "MoneyDeposited",
            Summary    = "Deposit",
            OccurredAt = DateTime.UtcNow
        };

        // Act — simulate the prepend logic
        view.Activities.Insert(0, newEntry);

        // Assert — new entry is at index 0; original entry moves to index 1
        view.Activities.Should().HaveCount(2);
        view.Activities[0].EventType.Should().Be("MoneyDeposited");
        view.Activities[1].EventType.Should().Be("AccountOpened");
    }

    [Fact]
    public void Trim_ShouldEnforceMaxEntries_WhenWindowExceedsCapacity()
    {
        // Arrange — fill beyond max entries using the same prepend-and-trim logic
        var view = new RecentActivityView { Id = 1 };
        for (int i = 0; i < RecentActivityView.MaxEntries + 3; i++)
        {
            view.Activities.Insert(0,
                new RecentActivityEntry { EventType = $"Event{i}", Summary = $"Entry {i}" });

            while (view.Activities.Count > RecentActivityView.MaxEntries)
                view.Activities.RemoveAt(view.Activities.Count - 1);
        }

        // Assert — list is capped at MaxEntries regardless of how many were inserted
        view.Activities.Should().HaveCount(RecentActivityView.MaxEntries);
    }
}
