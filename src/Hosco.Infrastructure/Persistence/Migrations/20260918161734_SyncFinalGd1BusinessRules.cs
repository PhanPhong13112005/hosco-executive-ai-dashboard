using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hosco.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SyncFinalGd1BusinessRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AlertRules_TenantId_Code",
                table: "AlertRules");

            migrationBuilder.AddColumn<decimal>(
                name: "FloorPrice",
                table: "Products",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsKeySku",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ReservedQuantity",
                table: "Inventories",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "BaselineValue",
                table: "Alerts",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResolutionNote",
                table: "Alerts",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EscalatedAt",
                table: "Alerts",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RefundItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RefundId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    ReturnedValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefundItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefundItems_OrderItems_OrderItemId",
                        column: x => x.OrderItemId,
                        principalTable: "OrderItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RefundItems_Refunds_RefundId",
                        column: x => x.RefundId,
                        principalTable: "Refunds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RefundItems_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AlertRules_TenantId_BranchId_Code",
                table: "AlertRules",
                columns: new[] { "TenantId", "BranchId", "Code" },
                unique: true,
                filter: "[BranchId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RefundItems_OrderItemId",
                table: "RefundItems",
                column: "OrderItemId");

            migrationBuilder.CreateIndex(
                name: "IX_RefundItems_RefundId",
                table: "RefundItems",
                column: "RefundId");

            migrationBuilder.CreateIndex(
                name: "IX_RefundItems_TenantId_RefundId_OrderItemId",
                table: "RefundItems",
                columns: new[] { "TenantId", "RefundId", "OrderItemId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RefundItems");

            migrationBuilder.DropIndex(
                name: "IX_AlertRules_TenantId_BranchId_Code",
                table: "AlertRules");

            migrationBuilder.DropColumn(
                name: "FloorPrice",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "IsKeySku",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ReservedQuantity",
                table: "Inventories");

            migrationBuilder.DropColumn(
                name: "BaselineValue",
                table: "Alerts");

            migrationBuilder.DropColumn(
                name: "ResolutionNote",
                table: "Alerts");

            migrationBuilder.DropColumn(
                name: "EscalatedAt",
                table: "Alerts");

            migrationBuilder.CreateIndex(
                name: "IX_AlertRules_TenantId_Code",
                table: "AlertRules",
                columns: new[] { "TenantId", "Code" },
                unique: true);
        }
    }
}
