using TodoApi.Models;

namespace TodoApi.Services;

public interface IActivityLogService
{
    Task LogCreatedAsync(Todo todo);
    Task LogUpdatedAsync(Todo before, Todo after);
    Task LogDeletedAsync(Todo todo);
    Task<List<ActivityLogDto>> GetLogsAsync(int? todoId = null, int limit = 50);
    Task<long> GetLogCountAsync(int? todoId = null);
}
