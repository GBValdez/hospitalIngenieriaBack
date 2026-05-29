using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace fletesProyect.Migrations
{
    /// <inheritdoc />
    public partial class seed_default_medicines : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                INSERT INTO ""Brands"" (name, description, ""createAt"")
                SELECT 'Generica', 'Marca generica', NOW()
                WHERE NOT EXISTS (
                    SELECT 1 FROM ""Brands"" WHERE name = 'Generica'
                );

                INSERT INTO ""Medicines"" (name, description, price, ""brandId"", ""createAt"")
                SELECT medicine.name, medicine.description, medicine.price, brand.""Id"", NOW()
                FROM (
                    VALUES
                        ('Acetaminofen', 'Analgesico y antipiretico', 10.00),
                        ('Ibuprofeno', 'Antiinflamatorio no esteroideo', 12.00),
                        ('Amoxicilina', 'Antibiotico de amplio espectro', 25.00),
                        ('Loratadina', 'Antihistaminico', 8.00),
                        ('Omeprazol', 'Inhibidor de bomba de protones', 15.00),
                        ('Metformina', 'Antidiabetico oral', 18.00),
                        ('Losartan', 'Antihipertensivo', 20.00),
                        ('Salbutamol', 'Broncodilatador', 22.00)
                ) AS medicine(name, description, price)
                CROSS JOIN (
                    SELECT ""Id"" FROM ""Brands"" WHERE name = 'Generica' ORDER BY ""Id"" LIMIT 1
                ) AS brand
                WHERE NOT EXISTS (
                    SELECT 1 FROM ""Medicines"" WHERE ""Medicines"".name = medicine.name
                );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE FROM ""Medicines""
                WHERE name IN (
                    'Acetaminofen',
                    'Ibuprofeno',
                    'Amoxicilina',
                    'Loratadina',
                    'Omeprazol',
                    'Metformina',
                    'Losartan',
                    'Salbutamol'
                )
                AND NOT EXISTS (
                    SELECT 1 FROM ""Recipes"" WHERE ""Recipes"".""medicineId"" = ""Medicines"".""Id""
                );

                DELETE FROM ""Brands""
                WHERE name = 'Generica'
                AND NOT EXISTS (
                    SELECT 1 FROM ""Medicines"" WHERE ""Medicines"".""brandId"" = ""Brands"".""Id""
                );
            ");
        }
    }
}
