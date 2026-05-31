FROM mcr.microsoft.com/dotnet/sdk:10.0.300 AS build
WORKDIR /src

COPY global.json ./
COPY ServiceFlow.slnx ./
COPY src/ServiceFlow.Domain/ServiceFlow.Domain.csproj src/ServiceFlow.Domain/
COPY src/ServiceFlow.Application/ServiceFlow.Application.csproj src/ServiceFlow.Application/
COPY src/ServiceFlow.Infrastructure/ServiceFlow.Infrastructure.csproj src/ServiceFlow.Infrastructure/
COPY src/ServiceFlow.Api/ServiceFlow.Api.csproj src/ServiceFlow.Api/

RUN dotnet restore src/ServiceFlow.Api/ServiceFlow.Api.csproj

COPY . .
RUN dotnet publish src/ServiceFlow.Api/ServiceFlow.Api.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*

EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "ServiceFlow.Api.dll"]
