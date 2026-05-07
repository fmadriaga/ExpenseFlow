# syntax=docker/dockerfile:1
# ──────────────────────────────────────────────────────────────────────────────
# Stage 1: restore & publish
# ──────────────────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution + project files first so restore is cached unless deps change
COPY ExpenseFlow.sln .
COPY src/ExpenseFlow.Domain/ExpenseFlow.Domain.csproj             src/ExpenseFlow.Domain/
COPY src/ExpenseFlow.Application/ExpenseFlow.Application.csproj   src/ExpenseFlow.Application/
COPY src/ExpenseFlow.Infrastructure/ExpenseFlow.Infrastructure.csproj src/ExpenseFlow.Infrastructure/
COPY src/ExpenseFlow.Api/ExpenseFlow.Api.csproj                   src/ExpenseFlow.Api/
COPY src/ExpenseFlow.Worker/ExpenseFlow.Worker.csproj             src/ExpenseFlow.Worker/
COPY src/ExpenseFlow.Web/ExpenseFlow.Web.csproj                   src/ExpenseFlow.Web/

RUN dotnet restore src/ExpenseFlow.Api/ExpenseFlow.Api.csproj

# Copy the rest of the source
COPY src/ src/

RUN dotnet publish src/ExpenseFlow.Api/ExpenseFlow.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ──────────────────────────────────────────────────────────────────────────────
# Stage 2: runtime image
# ──────────────────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Non-root user for security
RUN addgroup --system appgroup && adduser --system --ingroup appgroup appuser

COPY --from=build /app/publish .

# Volumes for shared SQLite database and logs (mounted by docker-compose)
VOLUME ["/data", "/logs"]

ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_ENVIRONMENT=Production

# SQLite path relative to the container's working dir
ENV ConnectionStrings__ExpenseFlow=/data/expenseflow.db

USER appuser
EXPOSE 8080

ENTRYPOINT ["dotnet", "ExpenseFlow.Api.dll"]
