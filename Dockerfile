# Mage Knight Online - Dockerfile
# Multi-stage build for optimized production image

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files
COPY MageKnightOnline.slnx ./
COPY src/MageKnightOnline.Core/MageKnightOnline.Core.csproj src/MageKnightOnline.Core/
COPY src/MageKnightOnline.Data/MageKnightOnline.Data.csproj src/MageKnightOnline.Data/
COPY src/MageKnightOnline.Web/MageKnightOnline.Web.csproj src/MageKnightOnline.Web/

# Restore dependencies
RUN dotnet restore src/MageKnightOnline.Web/MageKnightOnline.Web.csproj

# Copy source code
COPY src/ src/
COPY spec/definitions/ spec/definitions/

# Build and publish
RUN dotnet publish src/MageKnightOnline.Web/MageKnightOnline.Web.csproj -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Install required fonts for PDF generation (if needed)
# RUN apt-get update && apt-get install -y fonts-liberation && rm -rf /var/lib/apt/lists/*

# Copy published files
COPY --from=build /app/publish .

# Copy game definition files to wwwroot
COPY --from=build /src/src/MageKnightOnline.Web/wwwroot/data/definitions wwwroot/data/definitions

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_RUNNING_IN_CONTAINER=true

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Expose port
EXPOSE 8080

# Run the application
ENTRYPOINT ["dotnet", "MageKnightOnline.Web.dll"]

