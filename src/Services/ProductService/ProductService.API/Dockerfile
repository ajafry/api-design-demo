# ----- Build stage -----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files first (layer-cache friendly)
COPY ApiDesignDemo.slnx ./
COPY src/SharedKernel/SharedKernel.csproj src/SharedKernel/
COPY src/Services/ProductService/ProductService.Domain/ProductService.Domain.csproj src/Services/ProductService/ProductService.Domain/
COPY src/Services/ProductService/ProductService.Application/ProductService.Application.csproj src/Services/ProductService/ProductService.Application/
COPY src/Services/ProductService/ProductService.Infrastructure/ProductService.Infrastructure.csproj src/Services/ProductService/ProductService.Infrastructure/
COPY src/Services/ProductService/ProductService.API/ProductService.API.csproj src/Services/ProductService/ProductService.API/

# Restore - cached unless .csproj files change
RUN dotnet restore src/Services/ProductService/ProductService.API/ProductService.API.csproj

# Copy the rest and publish
COPY src/SharedKernel/ src/SharedKernel/
COPY src/Services/ProductService/ src/Services/ProductService/

RUN dotnet publish src/Services/ProductService/ProductService.API/ProductService.API.csproj \
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

ENTRYPOINT ["dotnet", "ProductService.API.dll"]
