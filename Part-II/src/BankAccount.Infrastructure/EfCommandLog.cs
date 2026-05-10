// BankAccount.Infrastructure/EfCommandLog.cs
#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using BankAccount.Commands;
using BankAccount.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BankAccount.Infrastructure;

/// <summary>
/// A SQL-backed, append-only command log using Entity Framework Core.
/// Persists command records to a relational database, surviving application restarts.
///
/// This class mirrors the *intent* of SourceFlow.Net's EfCommandStore in the
/// SourceFlow.Stores.EntityFramework package. When we adopt the framework in
/// Chapter 12, this class is replaced by the framework's ICommandStoreAdapter
/// configured with AddSourceFlowEfStores().
///
/// Note on API differences: SourceFlow.Net's ICommandStore.Append receives a
/// pre-serialised CommandData object (with fields EntityId, SequenceNo,
/// CommandName, CommandType, PayloadType, PayloadData, Metadata, Timestamp).
/// Serialisation is handled upstream by CommandStoreAdapter. Our hand-built
/// EfCommandLog takes a generic TCommand and handles serialisation internally —
/// a simpler design appropriate for this teaching example. In Chapter 12 we
/// will see how the framework separates these responsibilities.
/// </summary>
public sealed class EfCommandLog
{
    private readonly CommandLogDbContext _dbContext;

    // Shared serialiser options — JsonSerializerOptions is designed to be
    // created once and reused. Creating a new instance per-call is a
    // documented .NET performance anti-pattern.
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Initialises the EfCommandLog with the provided DbContext.
    /// </summary>
    public EfCommandLog(CommandLogDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    /// <summary>
    /// Appends a command to the SQL-backed log.
    /// Serialises the command to JSON and stores the AssemblyQualifiedName for replay.
    /// Retries once on unique constraint violation (sequence number collision).
    /// </summary>
    public async Task AppendAsync<TCommand>(int entityId, TCommand command)
        where TCommand : notnull
    {
        ArgumentNullException.ThrowIfNull(command);

        const int maxRetries = 3;

        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                // Read the current maximum sequence number directly from the database.
                // We do NOT use a cached value — the database is the source of truth.
                var query = _dbContext.CommandRecords
                    .Where(r => r.EntityId == entityId)
                    .Select(r => (int?)r.SequenceNo);
                int currentMax = await query.MaxAsync() ?? 0;

                int nextSequenceNo = currentMax + 1;

                // Strip the version/culture/token from AssemblyQualifiedName to
                // produce a portable type name that survives version bumps.
                // Format: "Namespace.ClassName, AssemblyName"
                string commandType = GetPortableTypeName(command.GetType());

                string payloadData = JsonSerializer.Serialize(command, SerializerOptions);

                var record = new CommandRecordEntity
                {
                    EntityId = entityId,
                    SequenceNo = nextSequenceNo,
                    CommandType = commandType,
                    PayloadData = payloadData,
                    Timestamp = DateTimeOffset.UtcNow
                };

                _dbContext.CommandRecords.Add(record);
                await _dbContext.SaveChangesAsync();
                return; // Success.
            }
            catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
            {
                // The unique constraint on (EntityId, SequenceNo) fired.
                // Another writer took the sequence number between our MAX() read
                // and our INSERT. Detach the failed entity and retry.
                foreach (var entry in _dbContext.ChangeTracker.Entries())
                    entry.State = EntityState.Detached;

                if (attempt == maxRetries - 1)
                {
                    throw new InvalidOperationException(
                        $"Failed to append command for entity {entityId} after {maxRetries} attempts " +
                        "due to repeated sequence number collisions.", ex);
                }

                // Brief delay before retry — in production, consider exponential backoff.
                await Task.Delay(TimeSpan.FromMilliseconds(10 * (attempt + 1)));
            }
        }
    }

    /// <summary>
    /// Loads all commands for the specified entity in ascending sequence order.
    /// Uses AsNoTracking() for performance — these records are read-only.
    /// </summary>
    public async Task<IReadOnlyList<CommandRecord>> LoadAsync(int entityId)
    {
        var entities = await _dbContext.CommandRecords
            .AsNoTracking()
            .Where(r => r.EntityId == entityId)
            .OrderBy(r => r.SequenceNo)
            .ToListAsync();

        // Project from EF entity to domain record.
        return entities.Select(e => new CommandRecord
        {
            EntityId = e.EntityId,
            SequenceNo = e.SequenceNo,
            CommandType = e.CommandType,
            PayloadData = e.PayloadData,
            Timestamp = e.Timestamp
        }).ToList().AsReadOnly();
    }

    // Produces a portable type name: "Namespace.ClassName, AssemblyName"
    // Strips Version, Culture, and PublicKeyToken which change between builds.
    private static string GetPortableTypeName(Type type)
    {
        string fullName = type.AssemblyQualifiedName
            ?? throw new InvalidOperationException(
                $"Type {type.Name} does not have an AssemblyQualifiedName.");

        var parts = fullName.Split(',');
        if (parts.Length < 2)
            return fullName;

        return $"{parts[0].Trim()}, {parts[1].Trim()}";
    }

    // Heuristic check for unique constraint violations.
    // Different database providers throw different inner exception types,
    // so we check the message text as a fallback.
    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        string message = ex.InnerException?.Message ?? ex.Message;
        return message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase)
            || message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase)
            || message.Contains("unique constraint", StringComparison.OrdinalIgnoreCase);
    }
}
