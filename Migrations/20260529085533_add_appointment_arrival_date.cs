using Microsoft.EntityFrameworkCore.Migrations;

using System;

#nullable disable

namespace fletesProyect.Migrations
{
    /// <inheritdoc />
    public partial class add_appointment_arrival_date : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "arrivalDate",
                table: "Appointments",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "arrivalDate",
                table: "Appointments");
        }
    }
}
