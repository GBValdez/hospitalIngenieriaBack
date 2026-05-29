using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace fletesProyect.Migrations
{
    /// <inheritdoc />
    public partial class add_appointment_status_catalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppointmentStatuses",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    userUpdateId = table.Column<string>(type: "text", nullable: true),
                    deleteAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    createAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updateAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppointmentStatuses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppointmentStatuses_AspNetUsers_userUpdateId",
                        column: x => x.userUpdateId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.Sql("""
                INSERT INTO "AppointmentStatuses" (name, description, "createAt")
                VALUES
                    ('ACTIVO', 'ACTIVO', NOW()),
                    ('REAGENDAR', 'REAGENDAR', NOW()),
                    ('CANCELAR', 'CANCELAR', NOW()),
                    ('FINALIZADA', 'FINALIZADA', NOW()),
                    ('AUSENTE', 'AUSENTE', NOW());
                """);

            migrationBuilder.AddColumn<long>(
                name: "previousStatusId",
                table: "AppointmentStatusHistories",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "statusId",
                table: "AppointmentStatusHistories",
                type: "bigint",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "AppointmentStatusHistories" h
                SET "previousStatusId" = s."Id"
                FROM "AppointmentStatuses" s
                WHERE UPPER(
                    CASE
                        WHEN h."previousStatus" = 'REAGENDADA' THEN 'REAGENDAR'
                        WHEN h."previousStatus" = 'CANCELADA' THEN 'CANCELAR'
                        ELSE h."previousStatus"
                    END
                ) = s.name;
                """);

            migrationBuilder.Sql("""
                UPDATE "AppointmentStatusHistories" h
                SET "statusId" = s."Id"
                FROM "AppointmentStatuses" s
                WHERE UPPER(
                    CASE
                        WHEN h.status = 'REAGENDADA' THEN 'REAGENDAR'
                        WHEN h.status = 'CANCELADA' THEN 'CANCELAR'
                        ELSE h.status
                    END
                ) = s.name;
                """);

            migrationBuilder.Sql("""
                UPDATE "AppointmentStatusHistories" h
                SET "statusId" = s."Id"
                FROM "AppointmentStatuses" s
                WHERE h."statusId" IS NULL AND s.name = 'ACTIVO';
                """);

            migrationBuilder.AlterColumn<long>(
                name: "statusId",
                table: "AppointmentStatusHistories",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "previousStatus",
                table: "AppointmentStatusHistories");

            migrationBuilder.DropColumn(
                name: "status",
                table: "AppointmentStatusHistories");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentStatusHistories_previousStatusId",
                table: "AppointmentStatusHistories",
                column: "previousStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentStatusHistories_statusId",
                table: "AppointmentStatusHistories",
                column: "statusId");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentStatuses_userUpdateId",
                table: "AppointmentStatuses",
                column: "userUpdateId");

            migrationBuilder.AddForeignKey(
                name: "FK_AppointmentStatusHistories_AppointmentStatuses_previousStat~",
                table: "AppointmentStatusHistories",
                column: "previousStatusId",
                principalTable: "AppointmentStatuses",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AppointmentStatusHistories_AppointmentStatuses_statusId",
                table: "AppointmentStatusHistories",
                column: "statusId",
                principalTable: "AppointmentStatuses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AppointmentStatusHistories_AppointmentStatuses_previousStat~",
                table: "AppointmentStatusHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_AppointmentStatusHistories_AppointmentStatuses_statusId",
                table: "AppointmentStatusHistories");

            migrationBuilder.DropIndex(
                name: "IX_AppointmentStatusHistories_previousStatusId",
                table: "AppointmentStatusHistories");

            migrationBuilder.DropIndex(
                name: "IX_AppointmentStatusHistories_statusId",
                table: "AppointmentStatusHistories");

            migrationBuilder.AddColumn<string>(
                name: "previousStatus",
                table: "AppointmentStatusHistories",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "AppointmentStatusHistories",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("""
                UPDATE "AppointmentStatusHistories" h
                SET "previousStatus" = s.name
                FROM "AppointmentStatuses" s
                WHERE h."previousStatusId" = s."Id";
                """);

            migrationBuilder.Sql("""
                UPDATE "AppointmentStatusHistories" h
                SET status = s.name
                FROM "AppointmentStatuses" s
                WHERE h."statusId" = s."Id";
                """);

            migrationBuilder.DropColumn(
                name: "previousStatusId",
                table: "AppointmentStatusHistories");

            migrationBuilder.DropColumn(
                name: "statusId",
                table: "AppointmentStatusHistories");

            migrationBuilder.DropTable(
                name: "AppointmentStatuses");
        }
    }
}
