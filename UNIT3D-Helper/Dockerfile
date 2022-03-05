#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0-bullseye-slim AS base
VOLUME /stats
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:6.0-bullseye-slim AS build
WORKDIR /src
COPY ["UNIT3D-Helper/UNIT3D-Helper.csproj", "UNIT3D-Helper/"]
RUN dotnet restore "UNIT3D-Helper/UNIT3D-Helper.csproj"
COPY . .
WORKDIR "/src/UNIT3D-Helper"
RUN dotnet build "UNIT3D-Helper.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "UNIT3D-Helper.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "UNIT3D-Helper.dll"]