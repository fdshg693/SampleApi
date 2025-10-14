# Copilot instructions for this repo

This repo is a minimal full‑stack chat + TODO app:
- Backend: ASP.NET Core Minimal API (net9.0) under `api/`
- Frontend: SvelteKit + Vite under `front/`
- Local dev flow: Vite dev server proxies `/api/*` to the .NET API over HTTPS.

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
- Backend: `api/Program.cs`, `api/Services/AiChatService.cs`, `api/Services/TodoService.cs`, `api/Models/ChatModels.cs`, `api/Models/TodoModels.cs`, `api/Properties/launchSettings.json`
- Frontend: `front/src/lib/api.ts`, `front/src/routes/+page.svelte` (chat), `front/src/routes/todos/+page.svelte` (TODO list), `front/vite.config.ts`

## Examples
- Example chat request body (frontend and API tests):
  ```json
  { "messages": [{ "role": "user", "content": "Hello" }], "model": "gpt-4o-mini" }
  ```
- Example TODO creation request:
  ```json
  { "title": "Buy groceries", "description": "Milk, eggs, bread" }
  ```
- Example TODO update request:
  ```json
  { "title": "Buy groceries", "isCompleted": true }
  ```
- Minimal Svelte usage (`+page.svelte`): call `sendChat({ messages })` then append `{ role: 'assistant', content: res.reply }`.
- TODO Svelte usage (`todos/+page.svelte`): call `getTodos()` on mount, `createTodo({ title, description })`, `updateTodo(id, { isCompleted })`, `deleteTodo(id)`.

Notes for AI agents
- Prefer editing or adding files within the folders noted above rather than scattering code elsewhere.
- When adding new endpoints, update CORS origins only if a new dev origin is required.
- If you change backend ports, also update `front/vite.config.ts` proxy target and `api/Properties/launchSettings.json` consistently.
