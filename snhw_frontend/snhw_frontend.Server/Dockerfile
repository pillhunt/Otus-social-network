#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Debug
WORKDIR /src
COPY ["snhw_frontend/snhw_frontend.Server/snhw_frontend.Server.csproj", "snhw_frontend/snhw_frontend.Server/"]
RUN dotnet restore "./snhw_frontend/snhw_frontend.Server/snhw_frontend.Server.csproj"
COPY . .
WORKDIR "/src/snhw_frontend/snhw_frontend.Server"
RUN dotnet build "./snhw_frontend.Server.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Debug
RUN dotnet publish "./snhw_frontend.Server.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "snhw_frontend.Server.dll"]