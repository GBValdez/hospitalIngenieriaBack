using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace fletesProyect.Migrations
{
    /// <inheritdoc />
    public partial class seed_disease_or_injury_catalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                INSERT INTO ""DiseaseOrInjuries"" (""name"", ""description"", ""createAt"")
                SELECT name, description, NOW()
                FROM (VALUES
                    ('Resfriado comun', 'Infeccion viral leve de vias respiratorias superiores.'),
                    ('Gripe', 'Infeccion respiratoria aguda compatible con influenza.'),
                    ('Faringitis', 'Inflamacion o infeccion de la garganta.'),
                    ('Bronquitis', 'Inflamacion de los bronquios con tos persistente.'),
                    ('Neumonia', 'Infeccion pulmonar que requiere seguimiento clinico.'),
                    ('Gastritis', 'Inflamacion de la mucosa gastrica.'),
                    ('Gastroenteritis', 'Cuadro digestivo con diarrea, vomitos o dolor abdominal.'),
                    ('Infeccion urinaria', 'Infeccion del tracto urinario.'),
                    ('Hipertension arterial', 'Elevacion de la presion arterial.'),
                    ('Diabetes mellitus', 'Trastorno metabolico asociado a glucosa elevada.'),
                    ('Migrana', 'Cefalea recurrente de intensidad moderada o severa.'),
                    ('Dolor lumbar', 'Dolor localizado en region baja de la espalda.'),
                    ('Esguince', 'Lesion ligamentaria por torcedura o esfuerzo.'),
                    ('Fractura', 'Ruptura parcial o completa de un hueso.'),
                    ('Contusion', 'Golpe o traumatismo sin herida abierta importante.'),
                    ('Herida superficial', 'Lesion abierta limitada a piel o tejido superficial.'),
                    ('Quemadura leve', 'Quemadura menor o superficial.'),
                    ('Dermatitis', 'Inflamacion de la piel con irritacion o prurito.'),
                    ('Alergia', 'Reaccion alergica leve o moderada.'),
                    ('Conjuntivitis', 'Inflamacion o infeccion de la conjuntiva.')
                ) AS seed(name, description)
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM ""DiseaseOrInjuries"" current
                    WHERE LOWER(current.""name"") = LOWER(seed.name)
                      AND current.""deleteAt"" IS NULL
                );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE FROM ""DiseaseOrInjuries""
                WHERE ""name"" IN (
                    'Resfriado comun',
                    'Gripe',
                    'Faringitis',
                    'Bronquitis',
                    'Neumonia',
                    'Gastritis',
                    'Gastroenteritis',
                    'Infeccion urinaria',
                    'Hipertension arterial',
                    'Diabetes mellitus',
                    'Migrana',
                    'Dolor lumbar',
                    'Esguince',
                    'Fractura',
                    'Contusion',
                    'Herida superficial',
                    'Quemadura leve',
                    'Dermatitis',
                    'Alergia',
                    'Conjuntivitis'
                );
            ");
        }
    }
}
