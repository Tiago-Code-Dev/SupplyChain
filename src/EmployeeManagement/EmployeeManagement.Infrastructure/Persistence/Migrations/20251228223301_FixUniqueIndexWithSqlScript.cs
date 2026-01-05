using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmployeeManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixUniqueIndexWithSqlScript : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Forçar recriação do índice único com filtro para soft delete
            migrationBuilder.Sql(@"
                -- Dropar o índice antigo se existir
                IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_CustomRoles_Name' AND object_id = OBJECT_ID('CustomRoles'))
                BEGIN
                    DROP INDEX [IX_CustomRoles_Name] ON [CustomRoles];
                END

                -- Criar novo índice com filtro para IsDeleted
                CREATE UNIQUE INDEX [IX_CustomRoles_Name] ON [CustomRoles] ([Name]) WHERE [IsDeleted] = 0;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_CustomRoles_Name' AND object_id = OBJECT_ID('CustomRoles'))
                BEGIN
                    DROP INDEX [IX_CustomRoles_Name] ON [CustomRoles];
                END

                CREATE UNIQUE INDEX [IX_CustomRoles_Name] ON [CustomRoles] ([Name]);
            ");
        }
    }
}
