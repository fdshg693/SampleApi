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

app.Run();