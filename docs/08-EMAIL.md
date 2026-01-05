# Configuração de Email

## Visão Geral

O sistema possui funcionalidade completa de envio de emails para:
- **Recuperação de senha** - Usuário clica em "Esqueci minha senha", digita o email e recebe um link para redefinir
- **Boas-vindas** - Email automático quando um novo funcionário é cadastrado

> **?? IMPORTANTE**: Esta configuração é feita pelo **ADMINISTRADOR DO SISTEMA** uma única vez.  
> O **usuário final NÃO precisa configurar nada** - apenas utiliza normalmente a funcionalidade de "Esqueci minha senha".

## Fluxo do Usuário Final

1. Na tela de login, clica em **"Esqueci minha senha"**
2. Digita seu email e clica em **"Enviar instruções"**
3. Recebe email com link para redefinir senha
4. Clica no link, define nova senha
5. Faz login normalmente

O usuário não precisa saber nada sobre configuração de SMTP ou credenciais.

---

## Configuração do Administrador

### Pré-requisito: Credenciais de Email

O administrador precisa configurar UMA VEZ as credenciais de um servidor de email.

## Provedores Suportados

### Gmail (Recomendado)

Para usar o Gmail, você precisa criar uma "Senha de App":

1. Acesse sua conta Google ? Segurança
2. Ative a verificação em duas etapas (se ainda não estiver)
3. Vá em "Senhas de app"
4. Crie uma nova senha de app para "Email"
5. Use essa senha no campo `Password` da configuração

**Configuração:**
```json
{
  "Email": {
    "SmtpServer": "smtp.gmail.com",
    "SmtpPort": 587,
    "SenderEmail": "seu-email@gmail.com",
    "SenderName": "Employee Management System",
    "Username": "seu-email@gmail.com",
    "Password": "xxxx xxxx xxxx xxxx",
    "UseSsl": true,
    "FrontendBaseUrl": "http://localhost:5173",
    "EnableSending": true
  }
}
```

### Outlook / Office 365

**Configuração:**
```json
{
  "Email": {
    "SmtpServer": "smtp.office365.com",
    "SmtpPort": 587,
    "SenderEmail": "seu-email@outlook.com",
    "SenderName": "Employee Management System",
    "Username": "seu-email@outlook.com",
    "Password": "sua-senha",
    "UseSsl": true,
    "FrontendBaseUrl": "http://localhost:5173",
    "EnableSending": true
  }
}
```

### SendGrid

Para usar SendGrid, crie uma API Key em sendgrid.com.

**Configuração:**
```json
{
  "Email": {
    "SmtpServer": "smtp.sendgrid.net",
    "SmtpPort": 587,
    "SenderEmail": "noreply@seudominio.com",
    "SenderName": "Employee Management System",
    "Username": "apikey",
    "Password": "SG.xxxxxxxxxxxxxxxxxxxx",
    "UseSsl": true,
    "FrontendBaseUrl": "http://localhost:5173",
    "EnableSending": true
  }
}
```

### Mailtrap (Desenvolvimento/Testes)

[Mailtrap](https://mailtrap.io) é excelente para testes, pois captura emails sem enviá-los de verdade.

**Configuração:**
```json
{
  "Email": {
    "SmtpServer": "smtp.mailtrap.io",
    "SmtpPort": 2525,
    "SenderEmail": "noreply@teste.com",
    "SenderName": "Employee Management System",
    "Username": "seu-username-mailtrap",
    "Password": "sua-senha-mailtrap",
    "UseSsl": false,
    "FrontendBaseUrl": "http://localhost:5173",
    "EnableSending": true
  }
}
```

## Configurações

| Campo | Descrição |
|-------|-----------|
| `SmtpServer` | Servidor SMTP do provedor |
| `SmtpPort` | Porta SMTP (587 para TLS, 465 para SSL) |
| `SenderEmail` | Email que aparecerá como remetente |
| `SenderName` | Nome do remetente |
| `Username` | Usuário para autenticação SMTP |
| `Password` | Senha ou App Password |
| `UseSsl` | Usar conexão segura (recomendado: true) |
| `FrontendBaseUrl` | URL do frontend para links nos emails |
| `EnableSending` | Habilitar/desabilitar envio (false para dev) |
| `BccEmail` | Email para receber cópias (opcional) |

## Variáveis de Ambiente (Recomendado para Produção)

Para não expor credenciais no código, use variáveis de ambiente:

```bash
Email__SmtpServer=smtp.gmail.com
Email__SmtpPort=587
Email__SenderEmail=seu-email@gmail.com
Email__SenderName=Employee Management System
Email__Username=seu-email@gmail.com
Email__Password=xxxx-xxxx-xxxx-xxxx
Email__UseSsl=true
Email__FrontendBaseUrl=https://seudominio.com
Email__EnableSending=true
```

## Docker

No Docker Compose, adicione as variáveis de ambiente:

```yaml
services:
  api:
    environment:
      - Email__SmtpServer=smtp.gmail.com
      - Email__SmtpPort=587
      - Email__SenderEmail=${EMAIL_SENDER}
      - Email__Username=${EMAIL_USERNAME}
      - Email__Password=${EMAIL_PASSWORD}
      - Email__EnableSending=true
```

## Funcionalidades de Email

### 1. Reset de Senha (`/api/v1/auth/forgot-password`)

Quando um usuário solicita reset de senha:
- Sistema gera token de reset
- Email é enviado com link e código
- Token expira em 2 horas

### 2. Boas-vindas (Disponível para implementação futura)

O serviço já suporta envio de email de boas-vindas com:
- Informações de acesso
- Senha temporária (opcional)
- Link para o sistema

## Testando

### Com EnableSending = false

Quando `EnableSending` está desabilitado, o sistema:
- Gera logs com o conteúdo do email
- Retorna sucesso (para não quebrar o fluxo)
- Não envia nada de verdade

### Logs

Verifique os logs para debug:
```
[INF] Email sent successfully to user@email.com with subject: Redefinição de Senha
[WRN] Email sending is disabled. Would have sent email to user@email.com
```

## Troubleshooting

### Erro de Autenticação (Gmail)

- Verifique se a verificação em 2 etapas está ativa
- Use uma Senha de App, não sua senha normal
- Verifique se "Acesso a apps menos seguros" NÃO está necessário (use Senha de App)

### Timeout

- Verifique conectividade com o servidor SMTP
- Tente porta 465 com SSL se 587 não funcionar

### Certificado SSL

Se houver erro de certificado em desenvolvimento, adicione temporariamente:
```csharp
ServicePointManager.ServerCertificateValidationCallback = (s, c, h, e) => true;
```
**?? NUNCA use isso em produção!**
