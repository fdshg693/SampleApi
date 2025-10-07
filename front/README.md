# Svelte Chat UI

Minimal SvelteKit frontend that talks to the .NET backend in the parent folder.

- Backend API base: `http://localhost:5073` (see `Properties/launchSettings.json`)
- Frontend dev server: `http://localhost:5173`
- Dev proxy is configured in `vite.config.ts` so calls to `/api/*` go to the backend (no CORS hassle).

## Prereqs

- Node 18+ and pnpm
- .NET 9 SDK

## Run

Open two terminals.

1) Backend (.NET)

```
# from repo root
# optional: set OPENAI_API_KEY for real completions, otherwise you get stub echo
$env:OPENAI_API_KEY = "<your key>"; dotnet run
```

2) Frontend (SvelteKit)

```
# from myapp/

pnpm run dev --open
```

Then open http://localhost:5173 and chat. The header shows a "stub" tag if the backend is in stub mode.

