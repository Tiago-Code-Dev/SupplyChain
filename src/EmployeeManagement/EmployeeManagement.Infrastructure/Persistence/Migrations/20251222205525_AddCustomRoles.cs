using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace EmployeeManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Employees_Employees_ManagerId",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_Employees_DocumentNumber",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_Employees_Email",
                table: "Employees");

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                table: "PhoneNumbers",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "PhoneNumbers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedBy",
                table: "PhoneNumbers",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "PhoneNumbers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedBy",
                table: "PhoneNumbers",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                table: "Employees",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Employees",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                table: "Employees",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Employees",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedBy",
                table: "Employees",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Employees",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedBy",
                table: "Employees",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CustomRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    HierarchyLevel = table.Column<int>(type: "int", nullable: false),
                    IsSystemRole = table.Column<bool>(type: "bit", nullable: false),
                    LegacyRole = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RolePermissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomRoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Permission = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Resource = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RolePermissions_CustomRoles_CustomRoleId",
                        column: x => x.CustomRoleId,
                        principalTable: "CustomRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "CustomRoles",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DisplayName", "HierarchyLevel", "IsDeleted", "IsSystemRole", "LegacyRole", "Name", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(2025, 12, 22, 20, 55, 24, 734, DateTimeKind.Utc).AddTicks(6778), null, null, null, "Funcionário", 10, false, true, 1, "Employee", null, null },
                    { new Guid("22222222-2222-2222-2222-222222222222"), new DateTime(2025, 12, 22, 20, 55, 24, 734, DateTimeKind.Utc).AddTicks(6789), null, null, null, "Líder", 20, false, true, 2, "Leader", null, null },
                    { new Guid("33333333-3333-3333-3333-333333333333"), new DateTime(2025, 12, 22, 20, 55, 24, 734, DateTimeKind.Utc).AddTicks(6792), null, null, null, "Diretor", 30, false, true, 3, "Director", null, null },
                    { new Guid("44444444-4444-4444-4444-444444444444"), new DateTime(2025, 12, 22, 20, 55, 24, 734, DateTimeKind.Utc).AddTicks(6796), null, null, null, "Administrador", 100, false, true, 4, "Admin", null, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Employees_DocumentNumber",
                table: "Employees",
                column: "DocumentNumber",
                unique: true,
                filter: "IsDeleted = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_Email",
                table: "Employees",
                column: "Email",
                unique: true,
                filter: "IsDeleted = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_IsDeleted",
                table: "Employees",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_CustomRoles_LegacyRole",
                table: "CustomRoles",
                column: "LegacyRole",
                unique: true,
                filter: "[LegacyRole] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CustomRoles_Name",
                table: "CustomRoles",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_CustomRoleId_Permission_Resource",
                table: "RolePermissions",
                columns: new[] { "CustomRoleId", "Permission", "Resource" },
                unique: true,
                filter: "[Resource] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Employees_Employees_ManagerId",
                table: "Employees",
                column: "ManagerId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Employees_Employees_ManagerId",
                table: "Employees");

            migrationBuilder.DropTable(
                name: "RolePermissions");

            migrationBuilder.DropTable(
                name: "CustomRoles");

            migrationBuilder.DropIndex(
                name: "IX_Employees_DocumentNumber",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_Employees_Email",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_Employees_IsDeleted",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "PhoneNumbers");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "PhoneNumbers");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "PhoneNumbers");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "PhoneNumbers");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "PhoneNumbers");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Employees");

            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                table: "Employees",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Employees",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255);

            migrationBuilder.CreateIndex(
                name: "IX_Employees_DocumentNumber",
                table: "Employees",
                column: "DocumentNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Employees_Email",
                table: "Employees",
                column: "Email",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Employees_Employees_ManagerId",
                table: "Employees",
                column: "ManagerId",
                principalTable: "Employees",
                principalColumn: "Id");
        }
    }
}
