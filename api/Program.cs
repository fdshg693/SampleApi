using FluentValidation;
using MicroElements.Swashbuckle.FluentValidation.AspNetCore;
using SampleApi.Models;
using SampleApi.Services;
using SampleApi.Validators;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Add Swagger UI for Development
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "SampleApi",
        Version = "v1",
        Description = "A minimal full-stack chat + TODO API built with ASP.NET Core Minimal API",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "SampleApi Team",
        }
    });
});

// Add FluentValidation rules to Swagger
builder.Services.AddFluentValidationRulesToSwagger();

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

// FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<CreateTodoRequestValidator>();

// AI chat service
builder.Services.AddSingleton<AiChatService>();

// TODO service
builder.Services.AddSingleton<TodoService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/openapi/v1.json", "SampleApi v1");
        c.RoutePrefix = "swagger";
    });
}

// Use HTTPS redirection only in Production to avoid CORS issues in Development
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors();

// Health check for quick smoke tests
app.MapGet("/api/health", () => Results.Ok(new { status = "ok", time = DateTimeOffset.UtcNow }))
   .WithName("Health")
   .WithTags("Health")
   .WithSummary("Health check endpoint")
   .WithDescription("Returns the API health status and current server time for quick smoke tests.")
   .WithOpenApi();

// Chat endpoint: accepts { messages: [{ role, content }], model? } and returns { reply, isStub }
app.MapPost("/api/chat", async (ChatRequest request, IValidator<ChatRequest> validator, AiChatService ai, CancellationToken ct) =>
{
    var validationResult = await validator.ValidateAsync(request, ct);
    if (!validationResult.IsValid)
    {
        return Results.BadRequest(new { 
            error = "Validation failed", 
            errors = validationResult.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage })
        });
    }

    var response = await ai.GetChatCompletionAsync(request, ct);
    return Results.Ok(response);
})
.WithName("Chat")
.WithTags("Chat")
.WithSummary("Send chat messages to AI")
.WithDescription("Accepts an array of chat messages and returns an AI-generated response. Supports OpenAI Chat Completions API or falls back to a stub echo if the API key is not configured.")
.WithOpenApi();

// TODO endpoints
// GET /api/todos - 全TODO取得
app.MapGet("/api/todos", async (TodoService todoService, CancellationToken ct) =>
{
    var response = await todoService.GetAllAsync(ct);
    return Results.Ok(response);
})
.WithName("GetTodos")
.WithTags("Todos")
.WithSummary("Get all TODO items")
.WithDescription("Retrieves all TODO items from the in-memory store, including their id, title, description, completion status, and timestamps.")
.WithOpenApi();

// POST /api/todos - 新規TODO作成
app.MapPost("/api/todos", async (CreateTodoRequest request, IValidator<CreateTodoRequest> validator, TodoService todoService, CancellationToken ct) =>
{
    var validationResult = await validator.ValidateAsync(request, ct);
    if (!validationResult.IsValid)
    {
        return Results.BadRequest(new { 
            error = "Validation failed", 
            errors = validationResult.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage })
        });
    }

    var todo = await todoService.CreateAsync(request, ct);
    return Results.Ok(todo);
})
.WithName("CreateTodo")
.WithTags("Todos")
.WithSummary("Create a new TODO item")
.WithDescription("Creates a new TODO item with the specified title and optional description. Title must be 1-200 characters, description must be 0-1000 characters.")
.WithOpenApi();

// PUT /api/todos/{id} - TODO更新
app.MapPut("/api/todos/{id}", async (string id, UpdateTodoRequest request, IValidator<UpdateTodoRequest> validator, TodoService todoService, CancellationToken ct) =>
{
    var validationResult = await validator.ValidateAsync(request, ct);
    if (!validationResult.IsValid)
    {
        return Results.BadRequest(new { 
            error = "Validation failed", 
            errors = validationResult.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage })
        });
    }

    var todo = await todoService.UpdateAsync(id, request, ct);
    if (todo is null)
    {
        return Results.NotFound(new { error = $"Todo with id '{id}' not found" });
    }
    return Results.Ok(todo);
})
.WithName("UpdateTodo")
.WithTags("Todos")
.WithSummary("Update an existing TODO item")
.WithDescription("Updates a TODO item's title, description, or completion status. All fields are optional in the request body. Returns 404 if the TODO item is not found.")
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
.WithTags("Todos")
.WithSummary("Delete a TODO item")
.WithDescription("Permanently removes a TODO item from the in-memory store. Returns 204 No Content on success, 404 if the item is not found.")
.WithOpenApi();

app.Run();