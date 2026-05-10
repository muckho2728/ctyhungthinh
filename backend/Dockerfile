# ─── Build Stage ──────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Environment to avoid Windows NuGet cache issues
ENV DOTNET_SKIP_FIRST_TIME_EXPERIENCE=true
ENV DOTNET_NOLOGO=true
ENV NUGET_PACKAGES=/root/.nuget/packages

# Copy config files
COPY Directory.Build.props ./
COPY backend/global.json ./
COPY backend/NuGet.config ./
COPY backend/CoffeeAnalytics.API/CoffeeAnalytics.API.csproj backend/CoffeeAnalytics.API/
COPY backend/CoffeeAnalytics.Application/CoffeeAnalytics.Application.csproj backend/CoffeeAnalytics.Application/
COPY backend/CoffeeAnalytics.Domain/CoffeeAnalytics.Domain.csproj backend/CoffeeAnalytics.Domain/
COPY backend/CoffeeAnalytics.Infrastructure/CoffeeAnalytics.Infrastructure.csproj backend/CoffeeAnalytics.Infrastructure/

RUN dotnet restore backend/CoffeeAnalytics.API/CoffeeAnalytics.API.csproj --no-cache

# Copy source and build
COPY . .
RUN dotnet build backend/CoffeeAnalytics.API/CoffeeAnalytics.API.csproj -c Release
RUN mkdir -p /app/publish
RUN cp -r backend/CoffeeAnalytics.API/bin/Release/net8.0/* /app/publish/

# ─── Runtime Stage ────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Security: run as non-root
RUN addgroup --system appgroup && adduser --system --ingroup appgroup appuser
USER appuser

COPY --from=build /app/publish .

EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "CoffeeAnalytics.API.dll"]
