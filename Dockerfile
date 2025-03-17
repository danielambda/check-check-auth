FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY CheckCheckAuth.csproj .
RUN dotnet restore

COPY . .

RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS runtime
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 5183

ENTRYPOINT ["dotnet", "CheckCheckAuth.dll"]
