#!/bin/bash

# Script para gerar certificado de desenvolvimento HTTPS para Docker
# Execute este script antes de iniciar o docker-compose

CERT_PATH="${1:-./certs}"
CERT_PASSWORD="${2:-password}"

echo "=== Gerando Certificado HTTPS para Docker ==="

# Criar diretório de certificados se não existir
if [ ! -d "$CERT_PATH" ]; then
    mkdir -p "$CERT_PATH"
    echo "Diretório '$CERT_PATH' criado."
fi

CERT_FILE="$CERT_PATH/aspnetapp.pfx"

# Verificar se o certificado já existe
if [ -f "$CERT_FILE" ]; then
    echo "Certificado já existe em '$CERT_FILE'."
    read -p "Deseja regenerar o certificado? (s/n) " response
    if [ "$response" != "s" ] && [ "$response" != "S" ]; then
        echo "Mantendo certificado existente."
        exit 0
    fi
    rm -f "$CERT_FILE"
fi

# Limpar certificados de desenvolvimento antigos
echo "Limpando certificados de desenvolvimento antigos..."
dotnet dev-certs https --clean 2>/dev/null || true

# Gerar novo certificado de desenvolvimento
echo "Gerando novo certificado de desenvolvimento..."
dotnet dev-certs https -ep "$CERT_FILE" -p "$CERT_PASSWORD"

if [ -f "$CERT_FILE" ]; then
    echo "Certificado gerado com sucesso em '$CERT_FILE'."
    
    # Confiar no certificado (Linux/Mac)
    echo "Configurando confiança no certificado..."
    dotnet dev-certs https --trust 2>/dev/null || echo "Nota: Em Linux, você pode precisar adicionar o certificado manualmente ao trust store."
    
    echo ""
    echo "=== Configuração Concluída ==="
    echo "Certificado: $CERT_FILE"
    echo "Senha: $CERT_PASSWORD"
    echo ""
    echo "Você pode agora executar:"
    echo "  docker-compose up --build"
    echo ""
    echo "Acesse:"
    echo "  HTTP:  http://localhost:5000"
    echo "  HTTPS: https://localhost:5001"
    echo "  Swagger: http://localhost:5000/swagger"
else
    echo "Erro: Certificado não foi gerado."
    exit 1
fi
