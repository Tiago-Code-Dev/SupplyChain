using AutoMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SupplyChain.Infrastructure;
using SupplyChain.Infrastructure.Mappings;
using SupplyChain.Infrastructure.Services;
using System.Text;

namespace SupplyChain
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Criar o builder da aplicação
            var builder = WebApplication.CreateBuilder(args);

            // Configurar DbContext com PostgreSQL
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

            // Mapper
            builder.Services.AddAutoMapper(typeof(MappingProfile));

            // Adicionar Controllers (API)
            builder.Services.AddControllers();

            // Adicionar suporte a Swagger (documentação da API)
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                // Adicionar informações da API
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Supply Chain API",
                    Version = "v1",
                    Description = "API de Gestão de Funcionários"
                });

                // Adicionar definição de segurança JWT
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    Description = "JWT Authorization header using the Bearer scheme. Exemplo: \"Authorization: Bearer {token}\""
                });

                // Adicionar requisito de segurança para todos os endpoints
                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            //JWT
            builder.Services.AddSingleton<JwtService>();
            // Configuração do JWT
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = builder.Configuration["Jwt:Issuer"],
                        ValidAudience = builder.Configuration["Jwt:Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"]))
                    };
                });

            // 2. Construir o aplicativo
            var app = builder.Build();

            // 3. Configurar o pipeline de requisição HTTP

            // Habilitar Swagger apenas no ambiente de desenvolvimento
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            // Redirecionar requisições HTTP para HTTPS
            app.UseHttpsRedirection();

            // Habilitar autenticação
            app.UseAuthentication();

            // Habilitar autorização para a aplicação
            app.UseAuthorization();

            // Mapear os controllers da API
            app.MapControllers();

            // 4. Iniciar a aplicação
            app.Run();
        }
    }
}
