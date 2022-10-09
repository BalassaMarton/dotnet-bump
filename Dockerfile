#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["src/dotnet-bump/dotnet-bump.csproj", "src/dotnet-bump/"]
RUN dotnet restore "src/dotnet-bump/dotnet-bump.csproj"
COPY . .
WORKDIR "/src/src/dotnet-bump"
RUN dotnet build "dotnet-bump.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "dotnet-bump.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "dotnet-bump-version.dll"]