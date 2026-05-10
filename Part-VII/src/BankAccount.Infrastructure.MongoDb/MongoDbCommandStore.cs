// Chapter 31 — MongoDB-backed implementation of ICommandStore
#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using SourceFlow;
using SourceFlow.Messaging.Commands;

namespace BankAccount.Infrastructure.MongoDb;

/// <summary>
/// A MongoDB-backed implementation of ICommandStore.
/// Stores command data as documents in a MongoDB collection, with a compound
/// unique index on (EntityId, SequenceNo) to prevent concurrent write collisions.
/// </summary>
public sealed class MongoDbCommandStore : ICommandStore
{
    private const string CollectionName = "commands";

    private readonly IMongoCollection<CommandDocument> _collection;

    /// <summary>
    /// Initialises a new instance of <see cref="MongoDbCommandStore"/>.
    /// </summary>
    /// <param name="database">The MongoDB database to store commands in.</param>
    public MongoDbCommandStore(IMongoDatabase database)
    {
        if (database is null)
            throw new ArgumentNullException(nameof(database));

        _collection = database.GetCollection<CommandDocument>(CollectionName);
        EnsureIndexes();
    }

    /// <summary>
    /// Appends a serialised command document to the MongoDB collection.
    /// </summary>
    public Task Append(CommandData commandData)
    {
        if (commandData is null)
            throw new ArgumentNullException(nameof(commandData));

        var document = MapToDocument(commandData);
        return _collection.InsertOneAsync(document);
    }

    /// <summary>
    /// Loads all command documents for the specified aggregate, ordered ascending by SequenceNo.
    /// </summary>
    public async Task<IEnumerable<CommandData>> Load(int aggregateId)
    {
        var filter = Builders<CommandDocument>.Filter.Eq(d => d.EntityId, aggregateId);
        var sort = Builders<CommandDocument>.Sort.Ascending(d => d.SequenceNo);

        var documents = await _collection
            .Find(filter)
            .Sort(sort)
            .ToListAsync();

        var result = new List<CommandData>(documents.Count);
        foreach (var doc in documents)
            result.Add(MapToCommandData(doc));

        return result;
    }

    private void EnsureIndexes()
    {
        var indexKeys = Builders<CommandDocument>.IndexKeys
            .Ascending(d => d.EntityId)
            .Ascending(d => d.SequenceNo);

        var indexOptions = new CreateIndexOptions { Unique = true };
        var indexModel = new CreateIndexModel<CommandDocument>(indexKeys, indexOptions);

        _collection.Indexes.CreateOne(indexModel);
    }

    private static CommandDocument MapToDocument(CommandData data) => new()
    {
        EntityId = data.EntityId,
        SequenceNo = data.SequenceNo,
        CommandName = data.CommandName,
        CommandType = data.CommandType,
        PayloadType = data.PayloadType,
        PayloadData = data.PayloadData,
        Metadata = data.Metadata,
        Timestamp = data.Timestamp
    };

    private static CommandData MapToCommandData(CommandDocument doc) => new()
    {
        EntityId = doc.EntityId,
        SequenceNo = doc.SequenceNo,
        CommandName = doc.CommandName,
        CommandType = doc.CommandType,
        PayloadType = doc.PayloadType,
        PayloadData = doc.PayloadData,
        Metadata = doc.Metadata,
        Timestamp = doc.Timestamp
    };
}

/// <summary>
/// The MongoDB BSON document model for a persisted command.
/// </summary>
internal sealed class CommandDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    public int EntityId { get; set; }
    public int SequenceNo { get; set; }
    public string CommandName { get; set; } = string.Empty;
    public string CommandType { get; set; } = string.Empty;
    public string PayloadType { get; set; } = string.Empty;
    public string PayloadData { get; set; } = string.Empty;
    public string Metadata { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
