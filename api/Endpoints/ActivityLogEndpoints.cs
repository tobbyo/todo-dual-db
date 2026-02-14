using TodoApi.Services;

namespace TodoApi.Endpoints;

public static class ActivityLogEndpoints
{
    public static void MapActivityLogEndpoints(this WebApplication app)
    {
        app.MapGet("/api/activity-logs", async (IActivityLogService activityLog, int? todoId, int? limit) =>
            await activityLog.GetLogsAsync(todoId, limit ?? 50));

        app.MapGet("/api/activity-logs/count", async (IActivityLogService activityLog, int? todoId) =>
            new { count = await activityLog.GetLogCountAsync(todoId) });
    }
}
