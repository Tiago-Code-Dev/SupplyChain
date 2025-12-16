# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy csproj files and restore
COPY src/EmployeeManagement/EmployeeManagement.Domain/*.csproj ./EmployeeManagement.Domain/
COPY src/EmployeeManagement/EmployeeManagement.Application/*.csproj ./EmployeeManagement.Application/
COPY src/EmployeeManagement/EmployeeManagement.Infrastructure/*.csproj ./EmployeeManagement.Infrastructure/
COPY src/EmployeeManagement/EmployeeManagement.Api/*.csproj ./EmployeeManagement.Api/
COPY src/Shared/Shared.Contracts/*.csproj ./Shared.Contracts/
COPY src/Shared/Shared.CrossCutting/*.csproj ./Shared.CrossCutting/

WORKDIR /src/EmployeeManagement.Api
RUN dotnet restore

# Copy source code
WORKDIR /src
COPY src/EmployeeManagement/EmployeeManagement.Application/ ./EmployeeManagement.Application/
COPY src/EmployeeManagement/EmployeeManagement.Domain/ ./EmployeeManagement.Domain/
COPY src/EmployeeManagement/EmployeeManagement.Infrastructure/ ./EmployeeManagement.Infrastructure/
COPY src/EmployeeManagement/EmployeeManagement.Api/ ./EmployeeManagement.Api/
COPY src/Shared/Shared.Contracts/ ./Shared.Contracts/
COPY src/Shared/Shared.CrossCutting/ ./Shared.CrossCutting/

# Build
WORKDIR /src/EmployeeManagement.Api
RUN dotnet publish -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

EXPOSE 8080
EXPOSE 8081

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "EmployeeManagement.Api.dll"]