FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project files for restore
COPY ["Stingray.Services.OutboxProcessor/Stingray.Services.OutboxProcessor.csproj", "Stingray.Services.OutboxProcessor/"]
COPY ["Stingray.Domain/Stingray.Domain.csproj", "Stingray.Domain/"]
COPY ["Stingray.Storage.InMemory/Stingray.Storage.InMemory.csproj", "Stingray.Storage.InMemory/"]

# Restore dependencies
RUN dotnet restore "Stingray.Services.OutboxProcessor/Stingray.Services.OutboxProcessor.csproj"

# Copy all source code
COPY Stingray.Services.OutboxProcessor/ Stingray.Services.OutboxProcessor/
COPY Stingray.Domain/ Stingray.Domain/
COPY Stingray.Storage.InMemory/ Stingray.Storage.InMemory/

# Build
WORKDIR "/src/Stingray.Services.OutboxProcessor"
RUN dotnet build "./Stingray.Services.OutboxProcessor.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Stingray.Services.OutboxProcessor.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Stingray.Services.OutboxProcessor.dll"]
