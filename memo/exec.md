# frontend
pnpm run dev --open 

# dotnet
dotnet run --launch-profile https

# orval
cd front; $env:NODE_TLS_REJECT_UNAUTHORIZED='0'; pnpm generate:api; Remove-Item env:NODE_TLS_REJECT_UNAUTHORIZED