using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace fletesProyect.Migrations
{
    /// <inheritdoc />
    public partial class add_appointment_disease_injuries_remove_text_diagnosis_treatment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "diagnosis",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "treatment",
                table: "Appointments");

            migrationBuilder.CreateTable(
                name: "AppointmentDiseaseOrInjuries",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    appointmentId = table.Column<long>(type: "bigint", nullable: false),
                    diseaseOrInjuryId = table.Column<long>(type: "bigint", nullable: false),
                    userUpdateId = table.Column<string>(type: "text", nullable: true),
                    deleteAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    createAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updateAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppointmentDiseaseOrInjuries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppointmentDiseaseOrInjuries_Appointments_appointmentId",
                        column: x => x.appointmentId,
                        principalTable: "Appointments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AppointmentDiseaseOrInjuries_AspNetUsers_userUpdateId",
                        column: x => x.userUpdateId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AppointmentDiseaseOrInjuries_DiseaseOrInjuries_diseaseOrInj~",
                        column: x => x.diseaseOrInjuryId,
                        principalTable: "DiseaseOrInjuries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentDiseaseOrInjuries_appointmentId_diseaseOrInjuryId",
                table: "AppointmentDiseaseOrInjuries",
                columns: new[] { "appointmentId", "diseaseOrInjuryId" },
                unique: true,
                filter: "\"deleteAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentDiseaseOrInjuries_diseaseOrInjuryId",
                table: "AppointmentDiseaseOrInjuries",
                column: "diseaseOrInjuryId");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentDiseaseOrInjuries_userUpdateId",
                table: "AppointmentDiseaseOrInjuries",
                column: "userUpdateId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppointmentDiseaseOrInjuries");

            migrationBuilder.AddColumn<string>(
                name: "diagnosis",
                table: "Appointments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "treatment",
                table: "Appointments",
                type: "text",
                nullable: true);
        }
    }
}
