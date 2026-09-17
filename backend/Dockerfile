FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/FlakeGuard.Core/FlakeGuard.Core.csproj src/FlakeGuard.Core/
COPY src/FlakeGuard.Api/FlakeGuard.Api.csproj src/FlakeGuard.Api/
RUN dotnet restore src/FlakeGuard.Api/FlakeGuard.Api.csproj

COPY src/ src/
RUN dotnet publish src/FlakeGuard.Api/FlakeGuard.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "FlakeGuard.Api.dll"]
