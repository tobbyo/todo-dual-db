using Microsoft.EntityFrameworkCore;
using TodoApi.Data;
using TodoApi.Models;
using TodoApi.Services;

namespace TodoApi.Endpoints;

public static class TodoEndpoints
{
    public static void MapTodoEndpoints(this WebApplication app)
    {
        // GET all todos
        app.MapGet("/api/todos", async (TodoDb db) =>
            await db.Todos.OrderBy(t => t.SortOrder).ThenByDescending(t => t.CreatedAt).ToListAsync());

        // GET single todo
        app.MapGet("/api/todos/{id}", async (int id, TodoDb db) =>
            await db.Todos.FindAsync(id)
                is Todo todo
                    ? Results.Ok(todo)
                    : Results.NotFound());

        // CREATE todo
        app.MapPost("/api/todos", async (TodoCreateDto dto, TodoDb db, IActivityLogService activityLog) =>
        {
            var maxOrder = await db.Todos.AnyAsync()
                ? await db.Todos.MaxAsync(t => t.SortOrder)
                : -1;

            var todo = new Todo
            {
                Title = dto.Title,
                Description = dto.Description,
                IsComplete = false,
                CreatedAt = DateTime.UtcNow,
                DueDate = dto.DueDate,
                Priority = dto.Priority,
                SortOrder = maxOrder + 1
            };
            db.Todos.Add(todo);
            await db.SaveChangesAsync();

            await activityLog.LogCreatedAsync(todo);

            return Results.Created($"/api/todos/{todo.Id}", todo);
        });

        // UPDATE todo
        app.MapPut("/api/todos/{id}", async (int id, TodoUpdateDto dto, TodoDb db, IActivityLogService activityLog) =>
        {
            var todo = await db.Todos.FindAsync(id);
            if (todo is null) return Results.NotFound();

            // Snapshot "before" state for change detection
            var beforeTitle = todo.Title;
            var beforeDescription = todo.Description;
            var beforeIsComplete = todo.IsComplete;
            var beforeDueDate = todo.DueDate;
            var beforePriority = todo.Priority;

            if (dto.Title is not null) todo.Title = dto.Title;
            if (dto.Description is not null) todo.Description = dto.Description;
            if (dto.IsComplete.HasValue) todo.IsComplete = dto.IsComplete.Value;
            if (dto.DueDate.HasValue) todo.DueDate = dto.DueDate.Value;
            if (dto.Priority is not null) todo.Priority = dto.Priority;

            await db.SaveChangesAsync();

            var before = new Todo
            {
                Id = todo.Id,
                Title = beforeTitle,
                Description = beforeDescription,
                IsComplete = beforeIsComplete,
                CreatedAt = todo.CreatedAt,
                DueDate = beforeDueDate,
                Priority = beforePriority
            };
            await activityLog.LogUpdatedAsync(before, todo);

            return Results.Ok(todo);
        });

        // REORDER todos
        app.MapPut("/api/todos/reorder", async (List<TodoReorderDto> items, TodoDb db) =>
        {
            var ids = items.Select(i => i.Id).ToList();
            var todos = await db.Todos.Where(t => ids.Contains(t.Id)).ToListAsync();
            foreach (var item in items)
            {
                var todo = todos.FirstOrDefault(t => t.Id == item.Id);
                if (todo is not null) todo.SortOrder = item.SortOrder;
            }
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        // DELETE todo
        app.MapDelete("/api/todos/{id}", async (int id, TodoDb db, IActivityLogService activityLog) =>
        {
            var todo = await db.Todos.FindAsync(id);
            if (todo is null) return Results.NotFound();

            await activityLog.LogDeletedAsync(todo);

            db.Todos.Remove(todo);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }
}
