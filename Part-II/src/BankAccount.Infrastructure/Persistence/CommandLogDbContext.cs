// BankAccount.Infrastructure/Persistence/CommandLogDbContext.cs
#nullable enable

using Microsoft.EntityFrameworkCore;

namespace BankAccount.Infrastructure.Persistence;

/// <summary>
/// The Entity Framework Core DbContext for the command log.
/// Manages the CommandLog table and enforces the composite unique index
/// on (EntityId, SequenceNo) at the database level.
/// </summary>
public sealed class CommandLogDbContext : DbContext
{
    /// <summary>
    /// Initialises the context with the provided options.
    /// </summary>
    public CommandLogDbContext(DbContextOptions<CommandLogDbContext> options)
        : base(options)
    {
    }

    /// <summary>The set of stored command records.</summary>
    public DbSet<CommandRecordEntity> CommandRecords => Set<CommandRecordEntity>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<CommandRecordEntity>(entity =>
        {
            entity.ToTable("CommandLog");

            // The UNIQUE constraint on (EntityId, SequenceNo) is the database-level
            // enforcement of the sequence number invariant. If application-level
            // locking fails, the database will reject duplicate sequence numbers.
            entity.HasIndex(e => new { e.EntityId, e.SequenceNo })
                  .IsUnique()
                  .HasDatabaseName("IX_CommandLog_EntityId_SequenceNo");

            // Index on EntityId for fast Load() queries.
            entity.HasIndex(e => e.EntityId)
                  .HasDatabaseName("IX_CommandLog_EntityId");

            // PayloadData can be large — ensure EF does not limit it.
            entity.Property(e => e.PayloadData)
                  .HasColumnType("TEXT");
        });
    }
}
