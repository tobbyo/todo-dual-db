using MongoDB.Bson;
using MongoDB.Driver;
using TodoApi.Models;

namespace TodoApi.Services;

public class ActivityLogService : IActivityLogService
{
    private readonly IMongoCollection<ActivityLog> _logs;

    public ActivityLogService(IMongoDatabase database)
    {
        _logs = database.GetCollection<ActivityLog>("activity_logs");
    }

    public async Task LogCreatedAsync(Todo todo)
    {
        var log = new ActivityLog
        {
            Action = "Created",
            TodoId = todo.Id,
            TodoTitle = todo.Title,
            Timestamp = DateTime.UtcNow,
            Details = new BsonDocument
            {
                { "description", todo.Description != null ? BsonValue.Create(todo.Description) : BsonNull.Value },
                { "isComplete", todo.IsComplete }
            }
        };
        await _logs.InsertOneAsync(log);
    }

    public async Task LogUpdatedAsync(Todo before, Todo after)
    {
        // Detect if this is specifically a Completed/Uncompleted action
        if (before.IsComplete != after.IsComplete && before.Title == after.Title && before.Description == after.Description)
        {
            var log = new ActivityLog
            {
                Action = after.IsComplete ? "Completed" : "Uncompleted",
                TodoId = after.Id,
                TodoTitle = after.Title,
                Timestamp = DateTime.UtcNow
            };
            await _logs.InsertOneAsync(log);
            return;
        }

        // Generic update with field-level changes
        var changes = new List<FieldChange>();
        if (before.Title != after.Title)
            changes.Add(new FieldChange { Field = "title", From = before.Title, To = after.Title });
        if (before.Description != after.Description)
            changes.Add(new FieldChange { Field = "description", From = before.Description, To = after.Description });
        if (before.IsComplete != after.IsComplete)
            changes.Add(new FieldChange { Field = "isComplete", From = before.IsComplete.ToString(), To = after.IsComplete.ToString() });

        var updateLog = new ActivityLog
        {
            Action = "Updated",
            TodoId = after.Id,
            TodoTitle = after.Title,
            Timestamp = DateTime.UtcNow,
            Changes = changes
        };
        await _logs.InsertOneAsync(updateLog);
    }

    public async Task LogDeletedAsync(Todo todo)
    {
        var log = new ActivityLog
        {
            Action = "Deleted",
            TodoId = todo.Id,
            TodoTitle = todo.Title,
            Timestamp = DateTime.UtcNow,
            Snapshot = new BsonDocument
            {
                { "id", todo.Id },
                { "title", todo.Title },
                { "description", todo.Description != null ? BsonValue.Create(todo.Description) : BsonNull.Value },
                { "isComplete", todo.IsComplete },
                { "createdAt", todo.CreatedAt }
            }
        };
        await _logs.InsertOneAsync(log);
    }

    public async Task<List<ActivityLogDto>> GetLogsAsync(int? todoId = null, int limit = 50)
    {
        var filterBuilder = Builders<ActivityLog>.Filter;
        var filter = todoId.HasValue
            ? filterBuilder.Eq(l => l.TodoId, todoId.Value)
            : filterBuilder.Empty;

        var logs = await _logs
            .Find(filter)
            .SortByDescending(l => l.Timestamp)
            .Limit(limit)
            .ToListAsync();

        return logs.Select(ToDto).ToList();
    }

    public async Task<long> GetLogCountAsync(int? todoId = null)
    {
        var filterBuilder = Builders<ActivityLog>.Filter;
        var filter = todoId.HasValue
            ? filterBuilder.Eq(l => l.TodoId, todoId.Value)
            : filterBuilder.Empty;

        return await _logs.CountDocumentsAsync(filter);
    }

    private static ActivityLogDto ToDto(ActivityLog log) => new(
        Id: log.Id ?? "",
        Action: log.Action,
        TodoId: log.TodoId,
        TodoTitle: log.TodoTitle,
        Timestamp: log.Timestamp,
        Details: log.Details != null ? BsonDocumentToDict(log.Details) : null,
        Changes: log.Changes?.Select(c => new FieldChangeDto(c.Field, c.From, c.To)).ToList(),
        Snapshot: log.Snapshot != null ? BsonDocumentToDict(log.Snapshot) : null
    );

    private static Dictionary<string, object?> BsonDocumentToDict(BsonDocument doc)
    {
        var dict = new Dictionary<string, object?>();
        foreach (var element in doc)
        {
            dict[element.Name] = element.Value.BsonType switch
            {
                BsonType.Null => null,
                BsonType.Boolean => element.Value.AsBoolean,
                BsonType.Int32 => element.Value.AsInt32,
                BsonType.Int64 => element.Value.AsInt64,
                BsonType.String => element.Value.AsString,
                BsonType.DateTime => element.Value.ToUniversalTime(),
                _ => element.Value.ToString()
            };
        }
        return dict;
    }
}
