#!/usr/bin/env pwsh
# Script para configurar certificados HTTPS para desenvolvimento local e Docker

param(
    [string]$CertPassword = "password",
    [string]$CertPath = "./certs"
)

Write-Host "=== Configurando HTTPS para EmployeeManagement ===" -ForegroundColor Cyan

# Criar diretório de certificados se não existir
if (-not (Test-Path $CertPath)) {
    New-Item -ItemType Directory -Path $CertPath -Force | Out-Null
    Write-Host "Diretório '$CertPath' criado." -ForegroundColor Green
}

# Gerar certificado de desenvolvimento do ASP.NET Core
Write-Host "`nGerando certificado de desenvolvimento..." -ForegroundColor Yellow

# Limpar certificados antigos
dotnet dev-certs https --clean

# Criar novo certificado e confiar nele
dotnet dev-certs https --trust

# Exportar certificado para arquivo PFX (usado pelo Docker)
$pfxPath = Join-Path $CertPath "aspnetapp.pfx"
dotnet dev-certs https -ep $pfxPath -p $CertPassword

if (Test-Path $pfxPath) {
    Write-Host "`nCertificado exportado com sucesso para: $pfxPath" -ForegroundColor Green
} else {
    Write-Host "`nErro ao exportar certificado!" -ForegroundColor Red
    exit 1
}

Write-Host "`n=== Configuração concluída! ===" -ForegroundColor Cyan
Write-Host @"

Para executar a aplicação:

  [Local - HTTP e HTTPS]
  cd src/EmployeeManagement/EmployeeManagement.Api
  dotnet run
  
  Acesse:
    - HTTP:  http://localhost:7000/swagger
    - HTTPS: https://localhost:7001/swagger

  [Docker - HTTP e HTTPS]
  docker compose up -d
  
  Acesse:
    - HTTP:  http://localhost:5000/swagger
    - HTTPS: https://localhost:5001/swagger

"@ -ForegroundColor White
