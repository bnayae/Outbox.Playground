using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using OutboxPlayground.Infra.Abstractions;
using System.Diagnostics.CodeAnalysis;

namespace OutboxPlayground.Infra.MongoOutboxExtensions;

[ExcludeFromCodeCoverage]
public static class MongoOutboxExtensions
{
    /// <summary>
    /// Ensures the MongoDB collection(s) for CloudEvent are created with the appropriate indexes and validation rules.
    /// This method should be called during application startup or initialization.
    /// </summary>
    /// <param name="database">The MongoDB database instance.</param>
    /// <param name="collectionNames">Optional: One or more collection names to configure. If none provided, uses "Outbox".</param>
    public static void EnsureOutboxCollections(
        this IMongoDatabase database,
        params string[] collectionNames)
    {
        var names = (collectionNames != null && collectionNames.Length > 0)
            ? collectionNames
            : new[] { "Outbox" };

        foreach (var name in names)
        {
            EnsureCloudEventCollection(database, name);
        }
    }

    private static void EnsureCloudEventCollection(IMongoDatabase database, string collectionName)
    {
        // Register the CloudEvent class map if not already registered
        if (!BsonClassMap.IsClassMapRegistered(typeof(CloudEvent)))
        {
            BsonClassMap.RegisterClassMap<CloudEvent>(cm =>
            {
                cm.AutoMap();
                cm.SetIgnoreExtraElements(true);
                
                // Map properties with camelCase element names to match JSON schema
                cm.MapMember(c => c.SpecVersion).SetElementName("specVersion").SetIsRequired(true);
                cm.MapMember(c => c.Type).SetElementName("type").SetIsRequired(true);
                cm.MapMember(c => c.Source).SetElementName("source").SetIsRequired(true);
                // Map Id as a regular field, not as MongoDB's _id
                cm.UnmapMember(c => c.Id);
                cm.MapMember(c => c.Id).SetElementName("id").SetIsRequired(true);
                cm.MapMember(c => c.Time).SetElementName("time").SetIsRequired(true)
                    .SetSerializer(new DateTimeOffsetSerializer(MongoDB.Bson.BsonType.DateTime));
                cm.MapMember(c => c.PartitionKey).SetElementName("partitionKey").SetIsRequired(true);
                cm.MapMember(c => c.DataContentType).SetElementName("dataContentType");
                cm.MapMember(c => c.DataSchema).SetElementName("dataSchema");
                cm.MapMember(c => c.Subject).SetElementName("subject");
                cm.MapMember(c => c.Data).SetElementName("data");
                cm.MapMember(c => c.DataRef).SetElementName("dataRef");
                cm.MapMember(c => c.TraceParent).SetElementName("traceParent")
                    .SetSerializer(new NullableStructStringSerializer<OtelTraceParent>());
                cm.MapMember(c => c.Sequence).SetElementName("sequence");
                
                // Don't map any property as the MongoDB _id - let MongoDB auto-generate it
                cm.SetIdMember(null);
            });
        }

        // Create collection with validation if it doesn't exist
        var collectionList = database.ListCollectionNames().ToList();
        if (!collectionList.Contains(collectionName))
        {
            var validator = new BsonDocument
            {
                {
                    "$jsonSchema", new BsonDocument
                    {
                        { "bsonType", "object" },
                        { "required", new BsonArray { "specVersion", "type", "source", "id", "time", "partitionKey" } },
                        { "properties", new BsonDocument
                            {
                                { "specVersion", new BsonDocument { { "bsonType", "string" }, { "maxLength", 10 } } },
                                { "type", new BsonDocument { { "bsonType", "string" }, { "maxLength", 255 } } },
                                { "source", new BsonDocument { { "bsonType", "string" }, { "maxLength", 255 } } },
                                { "id", new BsonDocument { { "bsonType", "string" }, { "maxLength", 255 } } },
                                { "time", new BsonDocument { { "bsonType", "date" } } },
                                { "dataContentType", new BsonDocument { { "bsonType", new BsonArray { "string", "null" } }, { "maxLength", 255 } } },
                                { "dataSchema", new BsonDocument { { "bsonType", new BsonArray { "string", "null" } }, { "maxLength", 500 } } },
                                { "subject", new BsonDocument { { "bsonType", new BsonArray { "string", "null" } }, { "maxLength", 255 } } },
                                { "dataRef", new BsonDocument { { "bsonType", new BsonArray { "string", "null" } }, { "maxLength", 500 } } },
                                { "data", new BsonDocument { { "bsonType", new BsonArray { "binData", "null" } } } },
                                { "traceParent", new BsonDocument { { "bsonType", new BsonArray { "string", "null" } }, { "maxLength", 55 } } },
                                { "sequence", new BsonDocument { { "bsonType", new BsonArray { "long", "null" } } } },
                                { "partitionKey", new BsonDocument { { "bsonType", "string" }, { "maxLength", 400 } } }
                            }
                        }
                    }
                }
            };

            var options = new CreateCollectionOptions<BsonDocument> { Validator = new BsonDocumentFilterDefinition<BsonDocument>(validator) };
            database.CreateCollection(collectionName, options);
        }

        var collection = database.GetCollection<CloudEvent>(collectionName);

        // Ensure indexes
        var indexKeysTime = Builders<CloudEvent>.IndexKeys.Ascending(e => e.Time);
        var indexKeysSourceType = Builders<CloudEvent>.IndexKeys.Ascending(e => e.Source).Ascending(e => e.Type);

        var indexModels = new[]
        {
            new CreateIndexModel<CloudEvent>(indexKeysTime),
            new CreateIndexModel<CloudEvent>(indexKeysSourceType)
        };

        collection.Indexes.CreateMany(indexModels);
    }

    /// <summary>
    /// Serializer for nullable struct OtelTraceParent as string.
    /// </summary>
    private class NullableStructStringSerializer<T> : SerializerBase<T?> where T : struct
    {
        public override T? Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var bsonType = context.Reader.GetCurrentBsonType();
            if (bsonType == BsonType.Null)
            {
                context.Reader.ReadNull();
                return null;
            }
            var str = context.Reader.ReadString();
            if (string.IsNullOrEmpty(str)) return null;
            return (T?)typeof(T).GetMethod("From", new[] { typeof(string) })?.Invoke(null, new object[] { str });
        }

        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, T? value)
        {
            if (value.HasValue)
                context.Writer.WriteString(value.Value.ToString());
            else
                context.Writer.WriteNull();
        }
    }
}