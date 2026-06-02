# ----- Build stage -----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files first (layer-cache friendly)
COPY ApiDesignDemo.slnx ./
COPY src/SharedKernel/SharedKernel.csproj src/SharedKernel/
COPY src/Services/OrderService/OrderService.Domain/OrderService.Domain.csproj src/Services/OrderService/OrderService.Domain/
COPY src/Services/OrderService/OrderService.Application/OrderService.Application.csproj src/Services/OrderService/OrderService.Application/
COPY src/Services/OrderService/OrderService.Infrastructure/OrderService.Infrastructure.csproj src/Services/OrderService/OrderService.Infrastructure/
COPY src/Services/OrderService/OrderService.API/OrderService.API.csproj src/Services/OrderService/OrderService.API/

# Restore - cached unless .csproj files change
RUN dotnet restore src/Services/OrderService/OrderService.API/OrderService.API.csproj

# Copy the rest and publish
COPY src/SharedKernel/ src/SharedKernel/
COPY src/Services/OrderService/ src/Services/OrderService/

RUN dotnet publish src/Services/OrderService/OrderService.API/OrderService.API.csproj \
    -c Release -o /app/publish --no-restore

# ----- Runtime stage -----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Non-root user for security
RUN groupadd --system appgroup && useradd --system --gid appgroup appuser
USER appuser

COPY --from=build /app/publish .

# ACA exposes port 8080 by default (non-privileged)
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "OrderService.API.dll"]
