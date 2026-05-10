# Chapter 29 — Multi-stage Dockerfile for BankAccount.Api

# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["src/BankAccount.Api/BankAccount.Api.csproj", "src/BankAccount.Api/"]
COPY ["src/BankAccount.Domain/BankAccount.Domain.csproj", "src/BankAccount.Domain/"]
COPY ["src/BankAccount.Infrastructure/BankAccount.Infrastructure.csproj", "src/BankAccount.Infrastructure/"]

RUN dotnet restore "src/BankAccount.Api/BankAccount.Api.csproj"

COPY . .

WORKDIR "/src/src/BankAccount.Api"
RUN dotnet build "BankAccount.Api.csproj" -c Release -o /app/build

# Stage 2: Publish
FROM build AS publish
RUN dotnet publish "BankAccount.Api.csproj" -c Release -o /app/publish \
    /p:UseAppHost=false

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

RUN adduser --disabled-password --gecos "" appuser
USER appuser

COPY --from=publish /app/publish .

HEALTHCHECK --interval=30s --timeout=10s --start-period=30s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "BankAccount.Api.dll"]
