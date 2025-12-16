FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["EmployeeManagement/EmployeeManagement.Api/EmployeeManagement.Api.csproj", "EmployeeManagement/EmployeeManagement.Api/"]
COPY ["EmployeeManagement/EmployeeManagement.Application/EmployeeManagement.Application.csproj", "EmployeeManagement/EmployeeManagement.Application/"]
COPY ["EmployeeManagement/EmployeeManagement.Domain/EmployeeManagement.Domain.csproj", "EmployeeManagement/EmployeeManagement.Domain/"]
COPY ["EmployeeManagement/EmployeeManagement.Infrastructure/EmployeeManagement.Infrastructure.csproj", "EmployeeManagement/EmployeeManagement.Infrastructure/"]
COPY ["Shared/Shared.Contracts/Shared.Contracts.csproj", "Shared/Shared.Contracts/"]
COPY ["Shared/Shared.CrossCutting/Shared.CrossCutting.csproj", "Shared/Shared.CrossCutting/"]
RUN dotnet restore "EmployeeManagement/EmployeeManagement.Api/EmployeeManagement.Api.csproj"
COPY . .
WORKDIR "/src/EmployeeManagement/EmployeeManagement.Api"
RUN dotnet build "EmployeeManagement.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "EmployeeManagement.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EmployeeManagement.Api.dll"]