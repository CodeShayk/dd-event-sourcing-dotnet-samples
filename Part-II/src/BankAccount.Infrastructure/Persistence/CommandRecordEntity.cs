// BankAccount.Infrastructure/Persistence/CommandRecordEntity.cs
#nullable enable

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankAccount.Infrastructure.Persistence;

/// <summary>
/// The Entity Framework Core entity mapped to the CommandLog table.
/// One row per stored command. Rows are never updated or deleted.
/// </summary>
[Table("CommandLog")]
public sealed class CommandRecordEntity
{
    /// <summary>
    /// Auto-generated surrogate primary key. Not used during replay —
    /// ordering is determined by SequenceNo, not by Id.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    /// <summary>
    /// The identifier of the entity (bank account) this command belongs to.
    /// </summary>
    [Required]
    public int EntityId { get; set; }

    /// <summary>
    /// The sequence number of this command within the entity's history.
    /// Together with EntityId, forms a unique composite index.
    /// </summary>
    [Required]
    public int SequenceNo { get; set; }

    /// <summary>
    /// The assembly-qualified type name of the command.
    /// Used by the replayer to deserialise PayloadData to the correct type.
    /// </summary>
    [Required]
    [MaxLength(512)]
    public string CommandType { get; set; } = string.Empty;

    /// <summary>
    /// The JSON-serialised command payload.
    /// Column type is TEXT (SQLite) or NVARCHAR(MAX) / TEXT (PostgreSQL).
    /// </summary>
    [Required]
    public string PayloadData { get; set; } = string.Empty;

    /// <summary>The UTC timestamp at which this command was appended.</summary>
    [Required]
    public DateTimeOffset Timestamp { get; set; }
}
