// Chapter 31 — DynamoDB-backed IViewModelStore (structural sketch, reader exercise)
#nullable enable

using System;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using SourceFlow;
using SourceFlow.Projections;

namespace BankAccount.Infrastructure.Aws;

/// <summary>
/// A DynamoDB-backed implementation of IViewModelStore.
///
/// Table schema:
///   Partition key: ViewModelType (String) — the full type name of the view model
///   Sort key: EntityId (Number) — the entity ID
///   Attribute: Data (String) — JSON-serialised view model
///
/// Reader exercise: complete the three methods using the AWS SDK v3 DynamoDB client.
/// </summary>
public sealed class DynamoDbViewModelStore : IViewModelStore
{
    private const string TableName = "viewmodels";
    private readonly IAmazonDynamoDB _client;

    public DynamoDbViewModelStore(IAmazonDynamoDB client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    public Task<TViewModel> Get<TViewModel>(int id)
        where TViewModel : class, IViewModel
    {
        throw new NotImplementedException(
            "Implement using GetItemRequest. See class summary for schema details.");
    }

    public Task<TViewModel> Persist<TViewModel>(TViewModel model)
        where TViewModel : class, IViewModel
    {
        throw new NotImplementedException(
            "Implement using PutItemRequest. Serialise model to the Data attribute.");
    }

    public Task Delete<TViewModel>(TViewModel model)
        where TViewModel : class, IViewModel
    {
        throw new NotImplementedException(
            "Implement using DeleteItemRequest.");
    }
}
