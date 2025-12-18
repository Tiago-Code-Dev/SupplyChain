# Script para gerar certificado de desenvolvimento HTTPS para Docker
# Execute este script antes de iniciar o docker-compose

param(
    [string]$CertPath = ".\certs",
    [string]$CertPassword = "password"
)

Write-Host "=== Gerando Certificado HTTPS para Docker ===" -ForegroundColor Cyan

# Criar diretório de certificados se não existir
if (-not (Test-Path $CertPath)) {
    New-Item -ItemType Directory -Path $CertPath -Force | Out-Null
    Write-Host "Diretório '$CertPath' criado." -ForegroundColor Green
}

$certFile = Join-Path $CertPath "aspnetapp.pfx"

# Verificar se o certificado já existe
if (Test-Path $certFile) {
    Write-Host "Certificado já existe em '$certFile'." -ForegroundColor Yellow
    $response = Read-Host "Deseja regenerar o certificado? (s/n)"
    if ($response -ne 's' -and $response -ne 'S') {
        Write-Host "Mantendo certificado existente." -ForegroundColor Green
        exit 0
    }
    Remove-Item $certFile -Force
}

try {
    # Limpar certificados de desenvolvimento antigos
    Write-Host "Limpando certificados de desenvolvimento antigos..." -ForegroundColor Yellow
    dotnet dev-certs https --clean 2>$null

    # Gerar novo certificado de desenvolvimento
    Write-Host "Gerando novo certificado de desenvolvimento..." -ForegroundColor Yellow
    dotnet dev-certs https -ep $certFile -p $CertPassword

    if (Test-Path $certFile) {
        Write-Host "Certificado gerado com sucesso em '$certFile'." -ForegroundColor Green
        
        # Confiar no certificado (Windows)
        Write-Host "Configurando confiança no certificado..." -ForegroundColor Yellow
        dotnet dev-certs https --trust
        
        Write-Host ""
        Write-Host "=== Configuração Concluída ===" -ForegroundColor Cyan
        Write-Host "Certificado: $certFile" -ForegroundColor White
        Write-Host "Senha: $CertPassword" -ForegroundColor White
        Write-Host ""
        Write-Host "Você pode agora executar:" -ForegroundColor Green
        Write-Host "  docker-compose up --build" -ForegroundColor Yellow
        Write-Host ""
        Write-Host "Acesse:" -ForegroundColor Green
        Write-Host "  HTTP:  http://localhost:5000" -ForegroundColor White
        Write-Host "  HTTPS: https://localhost:5001" -ForegroundColor White
        Write-Host "  Swagger: http://localhost:5000/swagger" -ForegroundColor White
    }
    else {
        Write-Host "Erro: Certificado não foi gerado." -ForegroundColor Red
        exit 1
    }
}
catch {
    Write-Host "Erro ao gerar certificado: $_" -ForegroundColor Red
    exit 1
}
