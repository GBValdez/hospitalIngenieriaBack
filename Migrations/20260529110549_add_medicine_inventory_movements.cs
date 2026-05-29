using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace fletesProyect.Migrations
{
    /// <inheritdoc />
    public partial class add_medicine_inventory_movements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MedicineInventories",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    medicineId = table.Column<long>(type: "bigint", nullable: false),
                    stock = table.Column<int>(type: "integer", nullable: false),
                    userUpdateId = table.Column<string>(type: "text", nullable: true),
                    deleteAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    createAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updateAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicineInventories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MedicineInventories_AspNetUsers_userUpdateId",
                        column: x => x.userUpdateId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MedicineInventories_Medicines_medicineId",
                        column: x => x.medicineId,
                        principalTable: "Medicines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MedicineInventoryMovements",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    medicineId = table.Column<long>(type: "bigint", nullable: false),
                    movementType = table.Column<string>(type: "text", nullable: false),
                    amount = table.Column<int>(type: "integer", nullable: false),
                    previousStock = table.Column<int>(type: "integer", nullable: false),
                    newStock = table.Column<int>(type: "integer", nullable: false),
                    unitPrice = table.Column<float>(type: "real", nullable: false),
                    reason = table.Column<string>(type: "text", nullable: false),
                    dispatchId = table.Column<long>(type: "bigint", nullable: true),
                    registeredByUserId = table.Column<string>(type: "text", nullable: true),
                    userUpdateId = table.Column<string>(type: "text", nullable: true),
                    deleteAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    createAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updateAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicineInventoryMovements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MedicineInventoryMovements_AspNetUsers_registeredByUserId",
                        column: x => x.registeredByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MedicineInventoryMovements_AspNetUsers_userUpdateId",
                        column: x => x.userUpdateId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MedicineInventoryMovements_Dispatchs_dispatchId",
                        column: x => x.dispatchId,
                        principalTable: "Dispatchs",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MedicineInventoryMovements_Medicines_medicineId",
                        column: x => x.medicineId,
                        principalTable: "Medicines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MedicineInventories_medicineId",
                table: "MedicineInventories",
                column: "medicineId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MedicineInventories_userUpdateId",
                table: "MedicineInventories",
                column: "userUpdateId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicineInventoryMovements_dispatchId",
                table: "MedicineInventoryMovements",
                column: "dispatchId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicineInventoryMovements_medicineId",
                table: "MedicineInventoryMovements",
                column: "medicineId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicineInventoryMovements_registeredByUserId",
                table: "MedicineInventoryMovements",
                column: "registeredByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicineInventoryMovements_userUpdateId",
                table: "MedicineInventoryMovements",
                column: "userUpdateId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MedicineInventories");

            migrationBuilder.DropTable(
                name: "MedicineInventoryMovements");
        }
    }
}
