using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace fletesProyect.Migrations
{
    /// <inheritdoc />
    public partial class add_exam_diagnoses_and_exam_type_diagnoses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExamDiseaseOrInjuries",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    examId = table.Column<long>(type: "bigint", nullable: false),
                    diseaseOrInjuryId = table.Column<long>(type: "bigint", nullable: false),
                    userUpdateId = table.Column<string>(type: "text", nullable: true),
                    deleteAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    createAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updateAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamDiseaseOrInjuries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExamDiseaseOrInjuries_AspNetUsers_userUpdateId",
                        column: x => x.userUpdateId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ExamDiseaseOrInjuries_DiseaseOrInjuries_diseaseOrInjuryId",
                        column: x => x.diseaseOrInjuryId,
                        principalTable: "DiseaseOrInjuries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExamDiseaseOrInjuries_Exams_examId",
                        column: x => x.examId,
                        principalTable: "Exams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExamTypeDiseaseOrInjuries",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    examTypeId = table.Column<long>(type: "bigint", nullable: false),
                    diseaseOrInjuryId = table.Column<long>(type: "bigint", nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true),
                    userUpdateId = table.Column<string>(type: "text", nullable: true),
                    deleteAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    createAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updateAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamTypeDiseaseOrInjuries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExamTypeDiseaseOrInjuries_AspNetUsers_userUpdateId",
                        column: x => x.userUpdateId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ExamTypeDiseaseOrInjuries_DiseaseOrInjuries_diseaseOrInjury~",
                        column: x => x.diseaseOrInjuryId,
                        principalTable: "DiseaseOrInjuries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExamTypeDiseaseOrInjuries_ExamTypes_examTypeId",
                        column: x => x.examTypeId,
                        principalTable: "ExamTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExamDiseaseOrInjuries_diseaseOrInjuryId",
                table: "ExamDiseaseOrInjuries",
                column: "diseaseOrInjuryId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamDiseaseOrInjuries_examId_diseaseOrInjuryId",
                table: "ExamDiseaseOrInjuries",
                columns: new[] { "examId", "diseaseOrInjuryId" },
                unique: true,
                filter: "\"deleteAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ExamDiseaseOrInjuries_userUpdateId",
                table: "ExamDiseaseOrInjuries",
                column: "userUpdateId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamTypeDiseaseOrInjuries_diseaseOrInjuryId",
                table: "ExamTypeDiseaseOrInjuries",
                column: "diseaseOrInjuryId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamTypeDiseaseOrInjuries_examTypeId_diseaseOrInjuryId",
                table: "ExamTypeDiseaseOrInjuries",
                columns: new[] { "examTypeId", "diseaseOrInjuryId" },
                unique: true,
                filter: "\"deleteAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ExamTypeDiseaseOrInjuries_userUpdateId",
                table: "ExamTypeDiseaseOrInjuries",
                column: "userUpdateId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExamDiseaseOrInjuries");

            migrationBuilder.DropTable(
                name: "ExamTypeDiseaseOrInjuries");
        }
    }
}
