# frontend
pnpm run dev --open 

# dotnet
dotnet run --launch-profile https

# orval
cd front; $env:NODE_TLS_REJECT_UNAUTHORIZED='0'; pnpm generate:api; Remove-Item env:NODE_TLS_REJECT_UNAUTHORIZED

# ファイル出力VERSION
$env:NODE_TLS_REJECT_UNAUTHORIZED='0'; Invoke-RestMethod -Uri 'https://localhost:7082/openapi/v1.json' -OutFile 'C:\CodeStudy\SampleApi\openapi-spec.json'