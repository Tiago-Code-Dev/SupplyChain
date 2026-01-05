using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmployeeManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixCustomRoleUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CustomRoles_Name",
                table: "CustomRoles");

            migrationBuilder.UpdateData(
                table: "CustomRoles",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "CreatedAt",
                value: new DateTime(2025, 12, 28, 22, 10, 16, 954, DateTimeKind.Utc).AddTicks(5043));

            migrationBuilder.UpdateData(
                table: "CustomRoles",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                column: "CreatedAt",
                value: new DateTime(2025, 12, 28, 22, 10, 16, 954, DateTimeKind.Utc).AddTicks(5055));

            migrationBuilder.UpdateData(
                table: "CustomRoles",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                column: "CreatedAt",
                value: new DateTime(2025, 12, 28, 22, 10, 16, 954, DateTimeKind.Utc).AddTicks(5058));

            migrationBuilder.UpdateData(
                table: "CustomRoles",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                column: "CreatedAt",
                value: new DateTime(2025, 12, 28, 22, 10, 16, 954, DateTimeKind.Utc).AddTicks(5061));

            migrationBuilder.CreateIndex(
                name: "IX_CustomRoles_Name",
                table: "CustomRoles",
                column: "Name",
                unique: true,
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CustomRoles_Name",
                table: "CustomRoles");

            migrationBuilder.UpdateData(
                table: "CustomRoles",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "CreatedAt",
                value: new DateTime(2025, 12, 22, 20, 55, 24, 734, DateTimeKind.Utc).AddTicks(6778));

            migrationBuilder.UpdateData(
                table: "CustomRoles",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                column: "CreatedAt",
                value: new DateTime(2025, 12, 22, 20, 55, 24, 734, DateTimeKind.Utc).AddTicks(6789));

            migrationBuilder.UpdateData(
                table: "CustomRoles",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                column: "CreatedAt",
                value: new DateTime(2025, 12, 22, 20, 55, 24, 734, DateTimeKind.Utc).AddTicks(6792));

            migrationBuilder.UpdateData(
                table: "CustomRoles",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                column: "CreatedAt",
                value: new DateTime(2025, 12, 22, 20, 55, 24, 734, DateTimeKind.Utc).AddTicks(6796));

            migrationBuilder.CreateIndex(
                name: "IX_CustomRoles_Name",
                table: "CustomRoles",
                column: "Name",
                unique: true);
        }
    }
}
