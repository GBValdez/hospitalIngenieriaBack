using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace fletesProyect.Migrations
{
    /// <inheritdoc />
    public partial class add_unique_patient_dpi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE ""Patients""
                SET dpi = 'D' || lpad(""Id""::text, 12, '0')
                WHERE ""deleteAt"" IS NULL
                  AND (dpi IS NULL OR btrim(dpi) = '');

                WITH ranked AS (
                    SELECT
                        ""Id"",
                        row_number() OVER (PARTITION BY btrim(dpi) ORDER BY ""Id"") AS row_number
                    FROM ""Patients""
                    WHERE ""deleteAt"" IS NULL
                      AND dpi IS NOT NULL
                      AND btrim(dpi) <> ''
                )
                UPDATE ""Patients"" patient
                SET dpi = 'D' || lpad(patient.""Id""::text, 12, '0')
                FROM ranked
                WHERE patient.""Id"" = ranked.""Id""
                  AND ranked.row_number > 1;
            ");

            migrationBuilder.CreateIndex(
                name: "IX_Patients_dpi",
                table: "Patients",
                column: "dpi",
                unique: true,
                filter: "\"deleteAt\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Patients_dpi",
                table: "Patients");
        }
    }
}
