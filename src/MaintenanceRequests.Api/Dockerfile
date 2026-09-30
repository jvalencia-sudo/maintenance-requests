# Build context: ./src (see docker-compose.yml).

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore first, from the project files only, so the layer is reused while dependencies don't change.
COPY MaintenanceRequests.Domain/MaintenanceRequests.Domain.csproj MaintenanceRequests.Domain/
COPY MaintenanceRequests.Application/MaintenanceRequests.Application.csproj MaintenanceRequests.Application/
COPY MaintenanceRequests.Infrastructure/MaintenanceRequests.Infrastructure.csproj MaintenanceRequests.Infrastructure/
COPY MaintenanceRequests.Api/MaintenanceRequests.Api.csproj MaintenanceRequests.Api/
RUN dotnet restore MaintenanceRequests.Api/MaintenanceRequests.Api.csproj

COPY . .
RUN dotnet publish MaintenanceRequests.Api/MaintenanceRequests.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Non-root user shipped with the .NET 8 images.
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "MaintenanceRequests.Api.dll"]
