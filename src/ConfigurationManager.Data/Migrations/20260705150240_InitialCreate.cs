using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConfigurationManager.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditLogEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EntityName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    EntityId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Action = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    OldValue = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    NewValue = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    Details = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    ChangedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ChangedBy = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ConfigurationSections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfigurationSections", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Environments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Environments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IntegrationTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tenants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantCode = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ConfigurationSubsections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SectionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfigurationSubsections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConfigurationSubsections_ConfigurationSections_SectionId",
                        column: x => x.SectionId,
                        principalTable: "ConfigurationSections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Integrations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    IntegrationName = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    IntegrationTypeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Integrations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Integrations_IntegrationTypes_IntegrationTypeId",
                        column: x => x.IntegrationTypeId,
                        principalTable: "IntegrationTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ConfigurationKeys",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    KeyName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    DataType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    DefaultValue = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    IsRequired = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsSensitive = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    VisibilityGroup = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    IntegrationTypeId = table.Column<Guid>(type: "TEXT", nullable: true),
                    SectionId = table.Column<Guid>(type: "TEXT", nullable: true),
                    SubsectionId = table.Column<Guid>(type: "TEXT", nullable: true),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    ValidationRules = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    ExampleValue = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    HelpText = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfigurationKeys", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConfigurationKeys_ConfigurationSections_SectionId",
                        column: x => x.SectionId,
                        principalTable: "ConfigurationSections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConfigurationKeys_ConfigurationSubsections_SubsectionId",
                        column: x => x.SubsectionId,
                        principalTable: "ConfigurationSubsections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConfigurationKeys_IntegrationTypes_IntegrationTypeId",
                        column: x => x.IntegrationTypeId,
                        principalTable: "IntegrationTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ConfigurationValues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ConfigurationKeyId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: true),
                    EnvironmentId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Value = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    EncryptedValue = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfigurationValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConfigurationValues_ConfigurationKeys_ConfigurationKeyId",
                        column: x => x.ConfigurationKeyId,
                        principalTable: "ConfigurationKeys",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ConfigurationValues_Environments_EnvironmentId",
                        column: x => x.EnvironmentId,
                        principalTable: "Environments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConfigurationValues_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogEntries_EntityName_EntityId",
                table: "AuditLogEntries",
                columns: new[] { "EntityName", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_ConfigurationKeys_IntegrationTypeId",
                table: "ConfigurationKeys",
                column: "IntegrationTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ConfigurationKeys_KeyName",
                table: "ConfigurationKeys",
                column: "KeyName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConfigurationKeys_SectionId",
                table: "ConfigurationKeys",
                column: "SectionId");

            migrationBuilder.CreateIndex(
                name: "IX_ConfigurationKeys_SubsectionId",
                table: "ConfigurationKeys",
                column: "SubsectionId");

            migrationBuilder.CreateIndex(
                name: "IX_ConfigurationSections_Name",
                table: "ConfigurationSections",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConfigurationSubsections_SectionId_Name",
                table: "ConfigurationSubsections",
                columns: new[] { "SectionId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConfigurationValues_ConfigurationKeyId_TenantId_EnvironmentId",
                table: "ConfigurationValues",
                columns: new[] { "ConfigurationKeyId", "TenantId", "EnvironmentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConfigurationValues_EnvironmentId",
                table: "ConfigurationValues",
                column: "EnvironmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ConfigurationValues_TenantId",
                table: "ConfigurationValues",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Environments_Name",
                table: "Environments",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Integrations_IntegrationName",
                table: "Integrations",
                column: "IntegrationName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Integrations_IntegrationTypeId",
                table: "Integrations",
                column: "IntegrationTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationTypes_Name",
                table: "IntegrationTypes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_TenantCode",
                table: "Tenants",
                column: "TenantCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogEntries");

            migrationBuilder.DropTable(
                name: "ConfigurationValues");

            migrationBuilder.DropTable(
                name: "Integrations");

            migrationBuilder.DropTable(
                name: "ConfigurationKeys");

            migrationBuilder.DropTable(
                name: "Environments");

            migrationBuilder.DropTable(
                name: "Tenants");

            migrationBuilder.DropTable(
                name: "ConfigurationSubsections");

            migrationBuilder.DropTable(
                name: "IntegrationTypes");

            migrationBuilder.DropTable(
                name: "ConfigurationSections");
        }
    }
}
