using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace fletesProyect.Migrations
{
    /// <inheritdoc />
    public partial class add_appointment_status_en_curso : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO "AppointmentStatuses" (name, description, "createAt")
                SELECT 'EN_CURSO', 'EN_CURSO', NOW()
                WHERE NOT EXISTS (
                    SELECT 1 FROM "AppointmentStatuses" WHERE name = 'EN_CURSO'
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "AppointmentStatuses"
                WHERE name = 'EN_CURSO'
                    AND NOT EXISTS (
                        SELECT 1 FROM "AppointmentStatusHistories" h
                        WHERE h."statusId" = "AppointmentStatuses"."Id"
                            OR h."previousStatusId" = "AppointmentStatuses"."Id"
                    );
                """);
        }
    }
}
