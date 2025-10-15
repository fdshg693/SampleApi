# Copilot instructions for this repo

This repo is a minimal full‑stack chat + TODO app:
- Backend: ASP.NET Core Minimal API (net9.0) under `api/`
- Frontend: SvelteKit + Vite under `front/`
- Local dev flow: Vite dev server proxies `/api/*` to the .NET API over HTTPS
- Validation: FluentValidation (backend) + Zod (frontend) with unified error format
- Code generation: Orval generates TypeScript client + Zod schemas from OpenAPI spec

## Architecture and data flow
- API endpoints are defined in `api/Program.cs`:
  - `GET /api/health` returns `{ status, time }` for smoke tests.
  - `POST /api/chat` accepts `{ messages: [{ role, content }], model? }` and returns `{ reply, isStub }`.
  - `GET /api/todos` returns `{ todos: [...], total }` for all TODO items.
  - `POST /api/todos` accepts `{ title, description? }` and returns the created `TodoItem`; validates `title` is non‑empty.
  - `PUT /api/todos/{id}` accepts `{ title?, description?, isCompleted? }` and returns the updated `TodoItem`; returns 404 if not found.
  - `DELETE /api/todos/{id}` removes the TODO; returns 204 on success, 404 if not found.
- Chat logic is in `api/Services/AiChatService.cs`:
  - Tries OpenAI Chat Completions using API key from `OpenAI:ApiKey` or env `OPENAI_API_KEY`; model from `request.Model` or `OpenAI:Model` (default `gpt-4o-mini`).
  - On missing key or errors, falls back to a stub echo; sets `IsStub = true` and includes the last user message.
- TODO logic is in `api/Services/TodoService.cs`:
  - In‑memory CRUD using `ConcurrentDictionary<string, TodoItem>`.
  - Methods: `GetAllAsync()`, `GetByIdAsync(id)`, `CreateAsync(request)`, `UpdateAsync(id, request)`, `DeleteAsync(id)`.
- Shared request/response types live in `api/Models/ChatModels.cs` and `api/Models/TodoModels.cs`.
- Frontend API calls are centralized in `front/src/lib/api.ts` with a blank `BASE` (same‑origin). During dev, Vite's proxy forwards `/api` to `https://localhost:7082` (see `front/vite.config.ts`). The main page is `front/src/routes/+page.svelte` (chat) and `front/src/routes/todos/+page.svelte` (TODO list).

## Local development
- Backend dev ports are set in `api/Properties/launchSettings.json`:
  - HTTPS `https://localhost:7082` and HTTP `http://localhost:5073`.
- CORS is enabled for `http://localhost:5173` (Vite) and `http://localhost:3000` (React/Next) in `Program.cs`.
- OpenAPI is exposed in Development only at `/openapi/v1.json`.

Recommended dev steps (Windows PowerShell):
1) API
- Trust dev certs if needed: `dotnet dev-certs https --trust`
- Run: `cd api; dotnet restore; dotnet run`
2) Frontend
- `cd front; pnpm install` (or npm install)
- `pnpm dev` (or npm run dev) → http://localhost:5173

## Conventions and patterns
- All new HTTP endpoints go in `Program.cs` using Minimal API style; prefer request/response DTOs in `api/Models/` with `System.Text.Json` annotations.
- Long‑running/external calls should live in injectable services under `api/Services/` and be registered in `Program.cs`.
- Frontend calls the backend only via `front/src/lib/api.ts`. Add functions there and import them from Svelte routes or components.
- Keep `/api` as the path prefix for server routes to remain covered by the Vite proxy.
- Error semantics: 
  - `/api/chat` returns 400 when `messages` is missing/empty; service falls back to stub with `isStub: true` on provider errors.
  - `/api/todos` POST returns 400 when `title` is null/empty.
  - `/api/todos/{id}` PUT/DELETE return 404 when the ID doesn't exist.

## External integrations
- OpenAI Chat Completions (`https://api.openai.com/v1/chat/completions`) via `AiChatService`; set keys through:
  - appsettings: `OpenAI:ApiKey`, `OpenAI:Model`
  - or environment: `OPENAI_API_KEY`
  - Effective precedence: appsettings > environment. Default model: `gpt-4o-mini`.

## CI/CD
- GitHub Actions workflow in `.github/workflows/master_seiwan-sampleapi.yml` builds with .NET 9 and publishes to Azure Web App `seiwan-sampleApi` on manual dispatch.

## Useful file map
- Backend: 
  - `api/Program.cs` - Main entry point, endpoint definitions, DI configuration, CORS setup, FluentValidation registration
  - `api/Services/AiChatService.cs` - OpenAI Chat Completions integration with fallback stub
  - `api/Services/TodoService.cs` - In-memory TODO CRUD operations using ConcurrentDictionary
  - `api/Models/ChatModels.cs` - Chat request/response DTOs (ChatRequest, ChatResponse, ChatMessage)
  - `api/Models/TodoModels.cs` - TODO DTOs (TodoItem, CreateTodoRequest, UpdateTodoRequest, GetTodosResponse)
  - `api/Validators/ChatValidators.cs` - FluentValidation validators for chat (ChatRequestValidator, ChatMessageValidator)
  - `api/Validators/TodoValidators.cs` - FluentValidation validators for todos (CreateTodoRequestValidator, UpdateTodoRequestValidator)
  - `api/Properties/launchSettings.json` - Launch profiles with port configurations (7082 HTTPS, 5073 HTTP)
  - `api/appsettings.json` / `api/appsettings.Development.json` - Configuration including OpenAI settings
- Frontend:
  - `front/src/lib/api.ts` - Centralized API client functions (health, sendChat, getTodos, createTodo, updateTodo, deleteTodo)
  - `front/src/lib/schemas.ts` - Zod validation schemas (re-exports from generated .zod.ts files)
  - `front/src/lib/generated/` - Orval-generated TypeScript client code (DO NOT EDIT MANUALLY)
  - `front/src/lib/generated/chat/chat.ts` - Chat API client (fetch)
  - `front/src/lib/generated/chat/chat.zod.ts` - Chat Zod schemas for validation
  - `front/src/lib/generated/todos/todos.ts` - TODO API client (fetch)
  - `front/src/lib/generated/todos/todos.zod.ts` - TODO Zod schemas for validation
  - `front/src/lib/generated/models/` - TypeScript type definitions for all DTOs
  - `front/src/routes/+page.svelte` - Chat UI with message history and form submission
  - `front/src/routes/todos/+page.svelte` - TODO list UI with create/update/delete operations
  - `front/vite.config.ts` - Vite configuration with proxy to backend HTTPS endpoint
  - `front/orval.config.ts` - Orval configuration for API client + Zod schema generation from OpenAPI spec
  - `front/package.json` - Scripts including `generate:api` for Orval, dev/build commands
- Documentation:
  - `docs/FluentValidation.md` - FluentValidation setup and integration with Swagger/OpenAPI
  - `docs/orval.md` - Orval configuration details for dual generation (fetch + Zod)
  - `docs/zod.md` - Zod usage patterns and validation examples
  - `docs/openapi.md` - OpenAPI specification details
- Root:
  - `openapi-spec.json` - Generated OpenAPI specification (used by Orval for stable generation)

## Technical implementation details

### Backend architecture
- **Framework**: ASP.NET Core Minimal API (.NET 9.0)
- **Dependency injection**: Services registered in `Program.cs` using `AddSingleton`
- **OpenAPI**: Generated using `AddOpenApi()` and exposed at `/openapi/v1.json` (Development only)
- **Swagger UI**: Available at `/swagger` in Development mode using `AddSwaggerGen()` and `UseSwaggerUI()`
- **Validation**: FluentValidation with `MicroElements.Swashbuckle.FluentValidation` bridge for OpenAPI schema enrichment
- **Validation flow**: 
  1. Validators auto-registered via `AddValidatorsFromAssemblyContaining<>`
  2. FluentValidation rules reflected in OpenAPI/Swagger via `AddFluentValidationRulesToSwagger()`
  3. Endpoints inject `IValidator<T>` and call `ValidateAsync()` before processing
  4. Validation errors returned as: `{ error: "Validation failed", errors: [{ field, message }] }`
- **CORS**: Default policy allows `localhost:5173` (Vite) and `localhost:3000` (React/Next) with credentials
- **HTTPS**: Disabled redirection in Development to avoid proxy issues; production uses HTTPS redirection

### AiChatService implementation
- Uses `HttpClient` to call OpenAI Chat Completions API (`https://api.openai.com/v1/chat/completions`)
- Configuration priority: `appsettings.json` `OpenAI:ApiKey` > environment variable `OPENAI_API_KEY`
- Model selection: Request.Model > `OpenAI:Model` config > default `gpt-4o-mini`
- Fallback behavior: On missing key or API errors, returns stub response with `IsStub = true` and echoes last user message
- Authorization header: `Bearer {apiKey}`
- Content-Type: `application/json`

### TodoService implementation
- **Storage**: `ConcurrentDictionary<string, TodoItem>` for thread-safe in-memory storage
- **ID generation**: Uses `Guid.NewGuid().ToString("N")[..8]` for short alphanumeric IDs
- **Async pattern**: All methods return `Task<T>` even though operations are synchronous (for future database integration)
- **Validation**: `CreateAsync` throws `ArgumentException` if title is null/whitespace
- **Thread safety**: Dictionary operations use TryGetValue, TryAdd, TryUpdate, TryRemove

### Frontend architecture
- **Framework**: SvelteKit with Vite as build tool and dev server
- **Routing**: File-based routing using `src/routes/` directory
- **State management**: Svelte 5 reactivity with `$state()` runes
- **API calls**: Centralized in `api.ts` using fetch with JSON serialization
- **Validation**: Zod schemas generated from OpenAPI spec, re-exported in `schemas.ts`
- **Proxy setup**: Vite proxies `/api` to `https://localhost:7082` with SSL verification disabled in dev
- **Type generation**: Orval reads OpenAPI spec and generates TypeScript client code + Zod schemas

### Orval configuration
- **Dual generation**: Two separate configurations in `orval.config.ts`
  1. `api`: Generates fetch-based API client (`tags-split` mode)
  2. `apiZod`: Generates Zod validation schemas with `.zod.ts` extension
- **Input**: `../openapi-spec.json` (local file for stable generation, avoids SSL issues)
- **Output mode**: `tags-split` - generates separate files per OpenAPI tag
- **Client types**: 
  - `api` config: `client: 'fetch'` - generates fetch-based client functions
  - `apiZod` config: `client: 'zod'` - generates Zod schemas with strict mode
- **Zod options**:
  - `generate`: Generates schemas for response, body, param, query, header
  - `strict`: Enables strict validation for all schema types
  - `coerce`: Auto-coerces query params (string, number, boolean, date)
  - `dateTimeOptions`: Handles ISO string with local/offset/precision settings
- **Generated structure**: 
  - `src/lib/generated/chat/chat.ts` - Chat API client (fetch)
  - `src/lib/generated/chat/chat.zod.ts` - Chat Zod schemas (chatBody, chatResponse, etc.)
  - `src/lib/generated/todos/todos.ts` - TODO API client (fetch)
  - `src/lib/generated/todos/todos.zod.ts` - TODO Zod schemas (createTodoBody, updateTodoBody, etc.)
  - `src/lib/generated/health/health.ts` - Health API client (fetch)
  - `src/lib/generated/models/` - TypeScript interfaces for all DTOs
- **Usage pattern**: Import from `schemas.ts` which re-exports generated Zod schemas for easy consumption

## Examples
- Example chat request body (frontend and API tests):
  ```json
  { "messages": [{ "role": "user", "content": "Hello" }], "model": "gpt-4o-mini" }
  ```
- Example TODO creation request:
  ```json
  { "title": "Buy groceries", "description": "Milk, eggs, bread" }
  ```
- Example TODO update request (all fields optional):
  ```json
  { "title": "Buy groceries", "isCompleted": true }
  ```
- Minimal Svelte usage (`+page.svelte`): call `sendChat({ messages })` then append `{ role: 'assistant', content: res.reply }`.
- TODO Svelte usage (`todos/+page.svelte`): call `getTodos()` on mount, `createTodo({ title, description })`, `updateTodo(id, { isCompleted })`, `deleteTodo(id)`.

## Common development workflows

### Adding a new API endpoint
1. Define request/response DTOs in `api/Models/[FeatureName]Models.cs`
2. Create service class in `api/Services/[FeatureName]Service.cs` if business logic is complex
3. Register service in `Program.cs` using `builder.Services.AddSingleton<[FeatureName]Service>()`
4. Add endpoint in `Program.cs` using `app.MapGet/Post/Put/Delete("/api/path", handler)`
5. Add `.WithName()`, `.WithTags()`, `.WithSummary()`, `.WithDescription()`, `.WithOpenApi()` for documentation
6. Test endpoint using Swagger UI or `api/SampleApi.http` file
7. Regenerate frontend client: `cd front && pnpm generate:api`
8. Add API function in `front/src/lib/api.ts` if needed (or use generated hooks)
9. Update CORS origins if new dev port is required

### Regenerating API client (Orval)
```powershell
cd front
pnpm generate:api
```
Note: Uses local `openapi-spec.json` file (no SSL issues). If generating from live API:
```powershell
$env:NODE_TLS_REJECT_UNAUTHORIZED='0'  # Disable SSL verification for self-signed cert
pnpm generate:api
Remove-Item env:NODE_TLS_REJECT_UNAUTHORIZED
```