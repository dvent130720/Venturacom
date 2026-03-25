# ── Build stage ────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY VenturacomSri.sln .
COPY src/VenturacomSri.Api/VenturacomSri.Api.csproj src/VenturacomSri.Api/
RUN dotnet restore

COPY . .
RUN dotnet publish src/VenturacomSri.Api -c Release -o /app/publish

# ── Runtime stage ──────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

RUN apt-get update && apt-get install -y ca-certificates && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "VenturacomSri.Api.dll"]
