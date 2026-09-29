using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hosco.Infrastructure.Persistence.Migrations;

[DbContext(typeof(HoscoDbContext))]
[Migration("20260915151000_AddDashboardAlertFoundation")]
public partial class AddDashboardAlertFoundation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AlertRules",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Code = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                Severity = table.Column<int>(type: "int", nullable: false),
                IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                Threshold = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                Baseline = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                WindowMinutes = table.Column<int>(type: "int", nullable: false),
                CooldownMinutes = table.Column<int>(type: "int", nullable: false),
                BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                ConfigJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AlertRules", x => x.Id);
                table.ForeignKey("FK_AlertRules_Tenants_TenantId", x => x.TenantId, "Tenants", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.AddColumn<Guid>(name: "AcknowledgedBy", table: "Alerts", type: "uniqueidentifier", nullable: true);
        migrationBuilder.AddColumn<string>(name: "DedupKey", table: "Alerts", type: "nvarchar(450)", maxLength: 450, nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<decimal>(name: "DetectedValue", table: "Alerts", type: "decimal(18,2)", precision: 18, scale: 2, nullable: true);
        migrationBuilder.AddColumn<string>(name: "Message", table: "Alerts", type: "nvarchar(2000)", maxLength: 2000, nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<Guid>(name: "ResolvedBy", table: "Alerts", type: "uniqueidentifier", nullable: true);
        migrationBuilder.AddColumn<string>(name: "RuleCode", table: "Alerts", type: "nvarchar(32)", maxLength: 32, nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<Guid>(name: "RuleId", table: "Alerts", type: "uniqueidentifier", nullable: true);
        migrationBuilder.AddColumn<decimal>(name: "ThresholdValue", table: "Alerts", type: "decimal(18,2)", precision: 18, scale: 2, nullable: true);

        migrationBuilder.AlterColumn<string>(name: "Type", table: "Alerts", type: "nvarchar(64)", maxLength: 64, nullable: false, oldClrType: typeof(string), oldType: "nvarchar(max)");
        migrationBuilder.AlterColumn<string>(name: "Title", table: "Alerts", type: "nvarchar(250)", maxLength: 250, nullable: false, oldClrType: typeof(string), oldType: "nvarchar(max)");

        migrationBuilder.Sql("""
            UPDATE Alerts
            SET RuleCode = CASE Type
                WHEN 'cancellation-spike' THEN 'AL-01'
                WHEN 'revenue-drop' THEN 'AL-02'
                WHEN 'low-stock' THEN 'AL-03'
                WHEN 'employee-cancellation' THEN 'AL-04'
                WHEN 'abnormal-discount' THEN 'AL-05'
                ELSE 'LEGACY' END,
                Message = Title,
                DedupKey = LOWER(REPLACE(CONVERT(nvarchar(36), TenantId), '-', '')) + ':' +
                    COALESCE(LOWER(REPLACE(CONVERT(nvarchar(36), BranchId), '-', '')), 'all') + ':' + Type + ':legacy'
            WHERE RuleCode = '';
            """);

        migrationBuilder.CreateIndex(name: "IX_AlertRules_TenantId_BranchId_IsEnabled", table: "AlertRules", columns: new[] { "TenantId", "BranchId", "IsEnabled" });
        migrationBuilder.CreateIndex(name: "IX_AlertRules_TenantId_Code", table: "AlertRules", columns: new[] { "TenantId", "Code" }, unique: true);
        migrationBuilder.CreateIndex(name: "IX_Alerts_RuleId", table: "Alerts", column: "RuleId");
        migrationBuilder.CreateIndex(name: "IX_Alerts_TenantId_BranchId_Severity_DetectedAt", table: "Alerts", columns: new[] { "TenantId", "BranchId", "Severity", "DetectedAt" });
        migrationBuilder.CreateIndex(name: "IX_Alerts_TenantId_DedupKey_DetectedAt", table: "Alerts", columns: new[] { "TenantId", "DedupKey", "DetectedAt" });
        migrationBuilder.AddForeignKey(name: "FK_Alerts_AlertRules_RuleId", table: "Alerts", column: "RuleId", principalTable: "AlertRules", principalColumn: "Id", onDelete: ReferentialAction.SetNull);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(name: "FK_Alerts_AlertRules_RuleId", table: "Alerts");
        migrationBuilder.DropIndex(name: "IX_Alerts_RuleId", table: "Alerts");
        migrationBuilder.DropIndex(name: "IX_Alerts_TenantId_BranchId_Severity_DetectedAt", table: "Alerts");
        migrationBuilder.DropIndex(name: "IX_Alerts_TenantId_DedupKey_DetectedAt", table: "Alerts");
        migrationBuilder.DropColumn(name: "AcknowledgedBy", table: "Alerts");
        migrationBuilder.DropColumn(name: "DedupKey", table: "Alerts");
        migrationBuilder.DropColumn(name: "DetectedValue", table: "Alerts");
        migrationBuilder.DropColumn(name: "Message", table: "Alerts");
        migrationBuilder.DropColumn(name: "ResolvedBy", table: "Alerts");
        migrationBuilder.DropColumn(name: "RuleCode", table: "Alerts");
        migrationBuilder.DropColumn(name: "RuleId", table: "Alerts");
        migrationBuilder.DropColumn(name: "ThresholdValue", table: "Alerts");
        migrationBuilder.AlterColumn<string>(name: "Type", table: "Alerts", type: "nvarchar(max)", nullable: false, oldClrType: typeof(string), oldType: "nvarchar(64)", oldMaxLength: 64);
        migrationBuilder.AlterColumn<string>(name: "Title", table: "Alerts", type: "nvarchar(max)", nullable: false, oldClrType: typeof(string), oldType: "nvarchar(250)", oldMaxLength: 250);
        migrationBuilder.DropTable(name: "AlertRules");
    }
}
