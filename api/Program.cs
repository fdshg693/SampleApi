using SampleApi.Models;
using SampleApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
// Allow React dev server(s) to call the API during development
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(
                "http://localhost:5173", // Vite default
                "http://localhost:3000"  // CRA/Next dev
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});

// HTTP + typed services
builder.Services.AddHttpClient();

// AI chat service
builder.Services.AddSingleton<AiChatService>();

// TODO service
builder.Services.AddSingleton<TodoService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseCors();

// Health check for quick smoke tests
app.MapGet("/api/health", () => Results.Ok(new { status = "ok", time = DateTimeOffset.UtcNow }))
   .WithName("Health")
   .WithOpenApi();

// Chat endpoint: accepts { messages: [{ role, content }], model? } and returns { reply, isStub }
app.MapPost("/api/chat", async (ChatRequest request, AiChatService ai, CancellationToken ct) =>
{
    if (request?.Messages is null || request.Messages.Count == 0)
    {
        return Results.BadRequest(new { error = "messages is required and must contain at least one item" });
    }

    var response = await ai.GetChatCompletionAsync(request, ct);
    return Results.Ok(response);
})
.WithName("Chat")
.WithOpenApi();

// TODO endpoints
// GET /api/todos - 全TODO取得
app.MapGet("/api/todos", async (TodoService todoService, CancellationToken ct) =>
{
    var response = await todoService.GetAllAsync(ct);
    return Results.Ok(response);
})
.WithName("GetTodos")
.WithOpenApi();

// POST /api/todos - 新規TODO作成
app.MapPost("/api/todos", async (CreateTodoRequest request, TodoService todoService, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(request?.Title) || request.Title.Length > 200)
    {
        return Results.BadRequest(new { error = "title is required and must be 1-200 characters" });
    }

    if (request.Description?.Length > 1000)
    {
        return Results.BadRequest(new { error = "description must be 0-1000 characters" });
    }

    var todo = await todoService.CreateAsync(request, ct);
    return Results.Ok(todo);
})
.WithName("CreateTodo")
.WithOpenApi();

// PUT /api/todos/{id} - TODO更新
app.MapPut("/api/todos/{id}", async (string id, UpdateTodoRequest request, TodoService todoService, CancellationToken ct) =>
{
    if (request is null)
    {
        return Results.BadRequest(new { error = "request body is required" });
    }

    if (request.Title is not null && (string.IsNullOrWhiteSpace(request.Title) || request.Title.Length > 200))
    {
        return Results.BadRequest(new { error = "title must be 1-200 characters when provided" });
    }

    if (request.Description?.Length > 1000)
    {
        return Results.BadRequest(new { error = "description must be 0-1000 characters" });
    }

    var todo = await todoService.UpdateAsync(id, request, ct);
    if (todo is null)
    {
        return Results.NotFound(new { error = $"Todo with id '{id}' not found" });
    }
    return Results.Ok(todo);
})
.WithName("UpdateTodo")
.WithOpenApi();

// DELETE /api/todos/{id} - TODO削除
app.MapDelete("/api/todos/{id}", async (string id, TodoService todoService, CancellationToken ct) =>
{
    var deleted = await todoService.DeleteAsync(id, ct);
    if (!deleted)
    {
        return Results.NotFound(new { error = $"Todo with id '{id}' not found" });
    }
    return Results.NoContent();
})
.WithName("DeleteTodo")
.WithOpenApi();

app.Run();