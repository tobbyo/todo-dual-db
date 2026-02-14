using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mongo2Go;
using MongoDB.Driver;
using TodoApi.Data;
using TodoApi.Models;
using TodoApi.Services;

namespace TodoApi.Tests;

public class TodoApiTests : IClassFixture<TodoApiTests.TodoApiFactory>
{
    private readonly HttpClient _client;

    public TodoApiTests(TodoApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetTodos_ReturnsEmptyList()
    {
        var response = await _client.GetAsync("/api/todos");
        response.EnsureSuccessStatusCode();

        var todos = await response.Content.ReadFromJsonAsync<List<Todo>>();
        Assert.NotNull(todos);
    }

    [Fact]
    public async Task CreateTodo_ReturnsCreated()
    {
        var dto = new TodoCreateDto("Test todo", "A description");
        var response = await _client.PostAsJsonAsync("/api/todos", dto);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var todo = await response.Content.ReadFromJsonAsync<Todo>();
        Assert.NotNull(todo);
        Assert.Equal("Test todo", todo.Title);
        Assert.Equal("A description", todo.Description);
        Assert.False(todo.IsComplete);
        Assert.True(todo.Id > 0);
    }

    [Fact]
    public async Task CreateTodo_WithoutDescription_ReturnsCreated()
    {
        var dto = new TodoCreateDto("No desc todo", null);
        var response = await _client.PostAsJsonAsync("/api/todos", dto);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var todo = await response.Content.ReadFromJsonAsync<Todo>();
        Assert.NotNull(todo);
        Assert.Null(todo.Description);
    }

    [Fact]
    public async Task GetTodoById_ReturnsNotFound_WhenMissing()
    {
        var response = await _client.GetAsync("/api/todos/99999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetTodoById_ReturnsTodo()
    {
        var create = await _client.PostAsJsonAsync("/api/todos", new TodoCreateDto("Find me", null));
        var created = await create.Content.ReadFromJsonAsync<Todo>();

        var response = await _client.GetAsync($"/api/todos/{created!.Id}");
        response.EnsureSuccessStatusCode();

        var todo = await response.Content.ReadFromJsonAsync<Todo>();
        Assert.Equal("Find me", todo!.Title);
    }

    [Fact]
    public async Task UpdateTodo_ChangesFields()
    {
        var create = await _client.PostAsJsonAsync("/api/todos", new TodoCreateDto("Update me", "old desc"));
        var created = await create.Content.ReadFromJsonAsync<Todo>();

        var update = new TodoUpdateDto("Updated title", "new desc", true);
        var response = await _client.PutAsJsonAsync($"/api/todos/{created!.Id}", update);
        response.EnsureSuccessStatusCode();

        var todo = await response.Content.ReadFromJsonAsync<Todo>();
        Assert.Equal("Updated title", todo!.Title);
        Assert.Equal("new desc", todo.Description);
        Assert.True(todo.IsComplete);
    }

    [Fact]
    public async Task UpdateTodo_PartialUpdate_OnlyChangesProvided()
    {
        var create = await _client.PostAsJsonAsync("/api/todos", new TodoCreateDto("Partial", "keep this"));
        var created = await create.Content.ReadFromJsonAsync<Todo>();

        var update = new TodoUpdateDto(null, null, true);
        var response = await _client.PutAsJsonAsync($"/api/todos/{created!.Id}", update);
        response.EnsureSuccessStatusCode();

        var todo = await response.Content.ReadFromJsonAsync<Todo>();
        Assert.Equal("Partial", todo!.Title);
        Assert.Equal("keep this", todo.Description);
        Assert.True(todo.IsComplete);
    }

    [Fact]
    public async Task UpdateTodo_ReturnsNotFound_WhenMissing()
    {
        var response = await _client.PutAsJsonAsync("/api/todos/99999", new TodoUpdateDto("x", null, null));
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteTodo_ReturnsNoContent()
    {
        var create = await _client.PostAsJsonAsync("/api/todos", new TodoCreateDto("Delete me", null));
        var created = await create.Content.ReadFromJsonAsync<Todo>();

        var response = await _client.DeleteAsync($"/api/todos/{created!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var get = await _client.GetAsync($"/api/todos/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
    }

    [Fact]
    public async Task DeleteTodo_ReturnsNotFound_WhenMissing()
    {
        var response = await _client.DeleteAsync("/api/todos/99999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // --- Activity Log Tests ---

    [Fact]
    public async Task CreateTodo_GeneratesActivityLog()
    {
        var dto = new TodoCreateDto("Log test create", "desc");
        var createResp = await _client.PostAsJsonAsync("/api/todos", dto);
        var todo = await createResp.Content.ReadFromJsonAsync<Todo>();

        var logsResp = await _client.GetAsync($"/api/activity-logs?todoId={todo!.Id}");
        logsResp.EnsureSuccessStatusCode();

        var logs = await logsResp.Content.ReadFromJsonAsync<List<ActivityLogDto>>();
        Assert.NotNull(logs);
        Assert.Contains(logs, l => l.Action == "Created" && l.TodoId == todo.Id);
    }

    [Fact]
    public async Task UpdateTodo_GeneratesActivityLog()
    {
        var createResp = await _client.PostAsJsonAsync("/api/todos", new TodoCreateDto("Log test update", "old"));
        var todo = await createResp.Content.ReadFromJsonAsync<Todo>();

        await _client.PutAsJsonAsync($"/api/todos/{todo!.Id}", new TodoUpdateDto("New title", null, null));

        var logsResp = await _client.GetAsync($"/api/activity-logs?todoId={todo.Id}");
        var logs = await logsResp.Content.ReadFromJsonAsync<List<ActivityLogDto>>();

        Assert.Contains(logs!, l => l.Action == "Updated" && l.Changes != null
            && l.Changes.Any(c => c.Field == "title" && c.From == "Log test update" && c.To == "New title"));
    }

    [Fact]
    public async Task CompleteTodo_GeneratesCompletedLog()
    {
        var createResp = await _client.PostAsJsonAsync("/api/todos", new TodoCreateDto("Log test complete", null));
        var todo = await createResp.Content.ReadFromJsonAsync<Todo>();

        await _client.PutAsJsonAsync($"/api/todos/{todo!.Id}", new TodoUpdateDto(null, null, true));

        var logsResp = await _client.GetAsync($"/api/activity-logs?todoId={todo.Id}");
        var logs = await logsResp.Content.ReadFromJsonAsync<List<ActivityLogDto>>();

        Assert.Contains(logs!, l => l.Action == "Completed" && l.TodoId == todo.Id);
    }

    [Fact]
    public async Task DeleteTodo_GeneratesActivityLog()
    {
        var createResp = await _client.PostAsJsonAsync("/api/todos", new TodoCreateDto("Log test delete", "snap"));
        var todo = await createResp.Content.ReadFromJsonAsync<Todo>();

        await _client.DeleteAsync($"/api/todos/{todo!.Id}");

        var logsResp = await _client.GetAsync($"/api/activity-logs?todoId={todo.Id}");
        var logs = await logsResp.Content.ReadFromJsonAsync<List<ActivityLogDto>>();

        Assert.Contains(logs!, l => l.Action == "Deleted" && l.TodoId == todo.Id && l.Snapshot != null);
    }

    [Fact]
    public async Task ActivityLogs_LimitWorks()
    {
        // Create 3 todos to generate 3 log entries
        await _client.PostAsJsonAsync("/api/todos", new TodoCreateDto("Limit 1", null));
        await _client.PostAsJsonAsync("/api/todos", new TodoCreateDto("Limit 2", null));
        await _client.PostAsJsonAsync("/api/todos", new TodoCreateDto("Limit 3", null));

        var logsResp = await _client.GetAsync("/api/activity-logs?limit=2");
        var logs = await logsResp.Content.ReadFromJsonAsync<List<ActivityLogDto>>();

        Assert.NotNull(logs);
        Assert.Equal(2, logs.Count);
    }

    [Fact]
    public async Task ActivityLogs_CountEndpoint()
    {
        var createResp = await _client.PostAsJsonAsync("/api/todos", new TodoCreateDto("Count test", null));
        var todo = await createResp.Content.ReadFromJsonAsync<Todo>();

        var countResp = await _client.GetAsync($"/api/activity-logs/count?todoId={todo!.Id}");
        countResp.EnsureSuccessStatusCode();

        var result = await countResp.Content.ReadFromJsonAsync<CountResult>();
        Assert.True(result!.Count >= 1);
    }

    private record CountResult(long Count);

    // --- Test factory that swaps SQLite for in-memory DB and MongoDB for Mongo2Go ---
    public class TodoApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
    {
        private MongoDbRunner _mongoRunner = default!;

        public Task InitializeAsync()
        {
            _mongoRunner = MongoDbRunner.Start();
            return Task.CompletedTask;
        }

        public new Task DisposeAsync()
        {
            _mongoRunner?.Dispose();
            return Task.CompletedTask;
        }

        protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // Remove all DbContext-related registrations (context, options, provider)
                var dbDescriptors = services
                    .Where(d => d.ServiceType.FullName?.Contains("EntityFrameworkCore") == true
                             || d.ServiceType == typeof(TodoDb)
                             || d.ServiceType == typeof(DbContextOptions<TodoDb>)
                             || d.ServiceType == typeof(DbContextOptions))
                    .ToList();
                foreach (var d in dbDescriptors) services.Remove(d);

                // Add in-memory database for testing
                var dbName = $"TodoTestDb_{Guid.NewGuid()}";
                services.AddDbContext<TodoDb>(opt =>
                    opt.UseInMemoryDatabase(dbName));

                // Remove existing MongoDB registrations
                var mongoDescriptors = services
                    .Where(d => d.ServiceType == typeof(IMongoClient)
                             || d.ServiceType == typeof(IMongoDatabase)
                             || d.ServiceType == typeof(IActivityLogService))
                    .ToList();
                foreach (var d in mongoDescriptors) services.Remove(d);

                // Add Mongo2Go-backed services
                var testDbName = $"test_{Guid.NewGuid():N}";
                services.AddSingleton<IMongoClient>(_ => new MongoClient(_mongoRunner.ConnectionString));
                services.AddSingleton<IMongoDatabase>(sp =>
                    sp.GetRequiredService<IMongoClient>().GetDatabase(testDbName));
                services.AddSingleton<IActivityLogService, ActivityLogService>();
            });
        }
    }
}
