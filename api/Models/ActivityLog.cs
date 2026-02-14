using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TodoApi.Models;

public class ActivityLog
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    public string Action { get; set; } = default!;
    public int TodoId { get; set; }
    public string TodoTitle { get; set; } = default!;
    public DateTime Timestamp { get; set; }

    // Flexible fields — different action types use different shapes
    [BsonIgnoreIfNull]
    public BsonDocument? Details { get; set; }

    [BsonIgnoreIfNull]
    public List<FieldChange>? Changes { get; set; }

    [BsonIgnoreIfNull]
    public BsonDocument? Snapshot { get; set; }
}

public class FieldChange
{
    public string Field { get; set; } = default!;
    public string? From { get; set; }
    public string? To { get; set; }
}

// DTOs for API responses (no BSON dependency)
public record ActivityLogDto(
    string Id,
    string Action,
    int TodoId,
    string TodoTitle,
    DateTime Timestamp,
    Dictionary<string, object?>? Details,
    List<FieldChangeDto>? Changes,
    Dictionary<string, object?>? Snapshot
);

public record FieldChangeDto(string Field, string? From, string? To);
