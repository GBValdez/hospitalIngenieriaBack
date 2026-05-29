using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace fletesProyect.Migrations
{
    /// <inheritdoc />
    public partial class add_exam_status_history : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExamStatusHistories",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    examId = table.Column<long>(type: "bigint", nullable: false),
                    previousStatusId = table.Column<long>(type: "bigint", nullable: true),
                    statusId = table.Column<long>(type: "bigint", nullable: false),
                    comment = table.Column<string>(type: "text", nullable: true),
                    changedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    changedByUserId = table.Column<string>(type: "text", nullable: true),
                    userUpdateId = table.Column<string>(type: "text", nullable: true),
                    deleteAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    createAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updateAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamStatusHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExamStatusHistories_AppointmentStatuses_previousStatusId",
                        column: x => x.previousStatusId,
                        principalTable: "AppointmentStatuses",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ExamStatusHistories_AppointmentStatuses_statusId",
                        column: x => x.statusId,
                        principalTable: "AppointmentStatuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExamStatusHistories_AspNetUsers_changedByUserId",
                        column: x => x.changedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ExamStatusHistories_AspNetUsers_userUpdateId",
                        column: x => x.userUpdateId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ExamStatusHistories_Exams_examId",
                        column: x => x.examId,
                        principalTable: "Exams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExamStatusHistories_changedByUserId",
                table: "ExamStatusHistories",
                column: "changedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamStatusHistories_examId",
                table: "ExamStatusHistories",
                column: "examId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamStatusHistories_previousStatusId",
                table: "ExamStatusHistories",
                column: "previousStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamStatusHistories_statusId",
                table: "ExamStatusHistories",
                column: "statusId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamStatusHistories_userUpdateId",
                table: "ExamStatusHistories",
                column: "userUpdateId");

            migrationBuilder.Sql(@"
                INSERT INTO ""AppointmentStatuses"" (""name"", ""description"", ""createAt"")
                SELECT 'ACTIVO', 'ACTIVO', NOW()
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM ""AppointmentStatuses""
                    WHERE ""name"" = 'ACTIVO' AND ""deleteAt"" IS NULL
                );

                INSERT INTO ""ExamStatusHistories"" (""examId"", ""statusId"", ""comment"", ""changedAt"", ""createAt"")
                SELECT e.""Id"", s.""Id"", 'Examen existente marcado como activo.', NOW(), NOW()
                FROM ""Exams"" e
                CROSS JOIN (
                    SELECT ""Id""
                    FROM ""AppointmentStatuses""
                    WHERE ""name"" = 'ACTIVO' AND ""deleteAt"" IS NULL
                    ORDER BY ""Id""
                    LIMIT 1
                ) s
                WHERE e.""deleteAt"" IS NULL
                  AND NOT EXISTS (
                      SELECT 1
                      FROM ""ExamStatusHistories"" h
                      WHERE h.""examId"" = e.""Id"" AND h.""deleteAt"" IS NULL
                  );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExamStatusHistories");
        }
    }
}
