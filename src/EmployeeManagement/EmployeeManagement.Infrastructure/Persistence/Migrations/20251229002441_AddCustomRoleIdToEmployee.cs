using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmployeeManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomRoleIdToEmployee : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CustomRoleId",
                table: "Employees",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "CustomRoles",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "CreatedAt",
                value: new DateTime(2025, 12, 29, 0, 24, 40, 830, DateTimeKind.Utc).AddTicks(2710));

            migrationBuilder.UpdateData(
                table: "CustomRoles",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                column: "CreatedAt",
                value: new DateTime(2025, 12, 29, 0, 24, 40, 830, DateTimeKind.Utc).AddTicks(2726));

            migrationBuilder.UpdateData(
                table: "CustomRoles",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                column: "CreatedAt",
                value: new DateTime(2025, 12, 29, 0, 24, 40, 830, DateTimeKind.Utc).AddTicks(2770));

            migrationBuilder.UpdateData(
                table: "CustomRoles",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                column: "CreatedAt",
                value: new DateTime(2025, 12, 29, 0, 24, 40, 830, DateTimeKind.Utc).AddTicks(2773));

            migrationBuilder.CreateIndex(
                name: "IX_Employees_CustomRoleId",
                table: "Employees",
                column: "CustomRoleId");

            migrationBuilder.AddForeignKey(
                name: "FK_Employees_CustomRoles_CustomRoleId",
                table: "Employees",
                column: "CustomRoleId",
                principalTable: "CustomRoles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Employees_CustomRoles_CustomRoleId",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_Employees_CustomRoleId",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "CustomRoleId",
                table: "Employees");

            migrationBuilder.UpdateData(
                table: "CustomRoles",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "CreatedAt",
                value: new DateTime(2025, 12, 28, 22, 33, 0, 799, DateTimeKind.Utc).AddTicks(8494));

            migrationBuilder.UpdateData(
                table: "CustomRoles",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                column: "CreatedAt",
                value: new DateTime(2025, 12, 28, 22, 33, 0, 799, DateTimeKind.Utc).AddTicks(8507));

            migrationBuilder.UpdateData(
                table: "CustomRoles",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                column: "CreatedAt",
                value: new DateTime(2025, 12, 28, 22, 33, 0, 799, DateTimeKind.Utc).AddTicks(8511));

            migrationBuilder.UpdateData(
                table: "CustomRoles",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                column: "CreatedAt",
                value: new DateTime(2025, 12, 28, 22, 33, 0, 799, DateTimeKind.Utc).AddTicks(8514));
        }
    }
}
