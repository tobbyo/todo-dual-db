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
            await db.Todos.OrderByDescending(t => t.CreatedAt).ToListAsync());

        // GET single todo
        app.MapGet("/api/todos/{id}", async (int id, TodoDb db) =>
            await db.Todos.FindAsync(id)
                is Todo todo
                    ? Results.Ok(todo)
                    : Results.NotFound());

        // CREATE todo
        app.MapPost("/api/todos", async (TodoCreateDto dto, TodoDb db, IActivityLogService activityLog) =>
        {
            var todo = new Todo
            {
                Title = dto.Title,
                Description = dto.Description,
                IsComplete = false,
                CreatedAt = DateTime.UtcNow
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

            if (dto.Title is not null) todo.Title = dto.Title;
            if (dto.Description is not null) todo.Description = dto.Description;
            if (dto.IsComplete.HasValue) todo.IsComplete = dto.IsComplete.Value;

            await db.SaveChangesAsync();

            var before = new Todo
            {
                Id = todo.Id,
                Title = beforeTitle,
                Description = beforeDescription,
                IsComplete = beforeIsComplete,
                CreatedAt = todo.CreatedAt
            };
            await activityLog.LogUpdatedAsync(before, todo);

            return Results.Ok(todo);
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
