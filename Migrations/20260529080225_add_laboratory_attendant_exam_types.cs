using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace fletesProyect.Migrations
{
    /// <inheritdoc />
    public partial class add_laboratory_attendant_exam_types : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LaboratoryAttendantExamTypes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    attendantId = table.Column<long>(type: "bigint", nullable: false),
                    examTypeId = table.Column<long>(type: "bigint", nullable: false),
                    userUpdateId = table.Column<string>(type: "text", nullable: true),
                    deleteAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    createAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updateAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LaboratoryAttendantExamTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LaboratoryAttendantExamTypes_AspNetUsers_userUpdateId",
                        column: x => x.userUpdateId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_LaboratoryAttendantExamTypes_ExamTypes_examTypeId",
                        column: x => x.examTypeId,
                        principalTable: "ExamTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LaboratoryAttendantExamTypes_Workers_attendantId",
                        column: x => x.attendantId,
                        principalTable: "Workers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LaboratoryAttendantExamTypes_attendantId",
                table: "LaboratoryAttendantExamTypes",
                column: "attendantId");

            migrationBuilder.CreateIndex(
                name: "IX_LaboratoryAttendantExamTypes_examTypeId",
                table: "LaboratoryAttendantExamTypes",
                column: "examTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_LaboratoryAttendantExamTypes_userUpdateId",
                table: "LaboratoryAttendantExamTypes",
                column: "userUpdateId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LaboratoryAttendantExamTypes");
        }
    }
}
