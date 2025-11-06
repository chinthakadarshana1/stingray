FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Stingray.Services.UserService/Stingray.Services.UserService.csproj", "Stingray.Services.UserService/"]
COPY ["Stingray.Application/Stingray.Application.csproj", "Stingray.Application/"]
COPY ["Stingray.Domain/Stingray.Domain.csproj", "Stingray.Domain/"]
COPY ["Stingray.Storage.InMemory/Stingray.Storage.InMemory.csproj", "Stingray.Storage.InMemory/"]
COPY ["Stingray.Services.OutboxProcessor/Stingray.Services.OutboxProcessor.csproj", "Stingray.Services.OutboxProcessor/"]
RUN dotnet restore "Stingray.Services.UserService/Stingray.Services.UserService.csproj"
COPY . .
WORKDIR "/src/Stingray.Services.UserService"
RUN dotnet build "./Stingray.Services.UserService.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Stingray.Services.UserService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Stingray.Services.UserService.dll"]
