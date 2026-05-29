using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace fletesProyect.Migrations
{
    /// <inheritdoc />
    public partial class add_disease_injury_catalog_and_medicine_dosages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DiseaseOrInjuries",
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
                    table.PrimaryKey("PK_DiseaseOrInjuries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiseaseOrInjuries_AspNetUsers_userUpdateId",
                        column: x => x.userUpdateId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "MedicineDiseaseOrInjuryDosages",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    medicineId = table.Column<long>(type: "bigint", nullable: false),
                    diseaseOrInjuryId = table.Column<long>(type: "bigint", nullable: false),
                    recommendedAmount = table.Column<int>(type: "integer", nullable: false),
                    maximumAmount = table.Column<int>(type: "integer", nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true),
                    userUpdateId = table.Column<string>(type: "text", nullable: true),
                    deleteAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    createAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updateAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicineDiseaseOrInjuryDosages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MedicineDiseaseOrInjuryDosages_AspNetUsers_userUpdateId",
                        column: x => x.userUpdateId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MedicineDiseaseOrInjuryDosages_DiseaseOrInjuries_diseaseOrI~",
                        column: x => x.diseaseOrInjuryId,
                        principalTable: "DiseaseOrInjuries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MedicineDiseaseOrInjuryDosages_Medicines_medicineId",
                        column: x => x.medicineId,
                        principalTable: "Medicines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DiseaseOrInjuries_userUpdateId",
                table: "DiseaseOrInjuries",
                column: "userUpdateId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicineDiseaseOrInjuryDosages_diseaseOrInjuryId",
                table: "MedicineDiseaseOrInjuryDosages",
                column: "diseaseOrInjuryId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicineDiseaseOrInjuryDosages_medicineId_diseaseOrInjuryId",
                table: "MedicineDiseaseOrInjuryDosages",
                columns: new[] { "medicineId", "diseaseOrInjuryId" },
                unique: true,
                filter: "\"deleteAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MedicineDiseaseOrInjuryDosages_userUpdateId",
                table: "MedicineDiseaseOrInjuryDosages",
                column: "userUpdateId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MedicineDiseaseOrInjuryDosages");

            migrationBuilder.DropTable(
                name: "DiseaseOrInjuries");
        }
    }
}
