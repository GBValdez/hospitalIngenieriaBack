using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace fletesProyect.Migrations
{
    /// <inheritdoc />
    public partial class seed_laboratory_attendants_by_exam_type : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                INSERT INTO ""Sexs"" (""name"", ""description"", ""createAt"")
                SELECT 'No especificado', 'Valor creado para seeds automaticos.', NOW()
                WHERE NOT EXISTS (SELECT 1 FROM ""Sexs"" WHERE ""deleteAt"" IS NULL);

                INSERT INTO ""Nationalities"" (""name"", ""description"", ""createAt"")
                SELECT 'No especificada', 'Valor creado para seeds automaticos.', NOW()
                WHERE NOT EXISTS (SELECT 1 FROM ""Nationalities"" WHERE ""deleteAt"" IS NULL);

                UPDATE ""AspNetRoles""
                SET ""deleteAt"" = NULL
                WHERE ""NormalizedName"" = 'LAB_ATTENDANT';

                INSERT INTO ""AspNetRoles"" (""Id"", ""Name"", ""NormalizedName"", ""ConcurrencyStamp"", ""createAt"")
                SELECT 'seed-role-lab-attendant', 'LAB_ATTENDANT', 'LAB_ATTENDANT', 'seed-role-lab-attendant', NOW()
                WHERE NOT EXISTS (SELECT 1 FROM ""AspNetRoles"" WHERE ""NormalizedName"" = 'LAB_ATTENDANT');

                WITH missing_exam_types AS (
                    SELECT et.""Id"", et.""name""
                    FROM ""ExamTypes"" et
                    WHERE et.""deleteAt"" IS NULL
                      AND NOT EXISTS (
                          SELECT 1
                          FROM ""LaboratoryAttendantExamTypes"" lat
                          INNER JOIN ""Workers"" w ON w.""Id"" = lat.""attendantId""
                          WHERE lat.""examTypeId"" = et.""Id""
                            AND lat.""deleteAt"" IS NULL
                            AND w.""deleteAt"" IS NULL
                      )
                )
                INSERT INTO ""AspNetUsers"" (
                    ""Id"",
                    ""UserName"",
                    ""NormalizedUserName"",
                    ""Email"",
                    ""NormalizedEmail"",
                    ""EmailConfirmed"",
                    ""SecurityStamp"",
                    ""ConcurrencyStamp"",
                    ""PhoneNumber"",
                    ""PhoneNumberConfirmed"",
                    ""TwoFactorEnabled"",
                    ""LockoutEnabled"",
                    ""AccessFailedCount"",
                    ""createAt""
                )
                SELECT
                    'seed-lab-attendant-examtype-' || m.""Id"",
                    'lab.examtype.' || m.""Id"",
                    UPPER('lab.examtype.' || m.""Id""),
                    'lab.examtype.' || m.""Id"" || '@hospital.local',
                    UPPER('lab.examtype.' || m.""Id"" || '@hospital.local'),
                    TRUE,
                    'seed-lab-attendant-examtype-' || m.""Id"",
                    'seed-lab-attendant-examtype-' || m.""Id"",
                    '0000000000',
                    FALSE,
                    FALSE,
                    TRUE,
                    0,
                    NOW()
                FROM missing_exam_types m
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM ""AspNetUsers"" u
                    WHERE u.""Id"" = 'seed-lab-attendant-examtype-' || m.""Id""
                );

                WITH seed_users AS (
                    SELECT u.""Id""
                    FROM ""AspNetUsers"" u
                    INNER JOIN ""ExamTypes"" et ON u.""Id"" = 'seed-lab-attendant-examtype-' || et.""Id""
                    WHERE et.""deleteAt"" IS NULL
                ),
                lab_role AS (
                    SELECT ""Id""
                    FROM ""AspNetRoles""
                    WHERE ""NormalizedName"" = 'LAB_ATTENDANT'
                    ORDER BY ""Id""
                    LIMIT 1
                )
                INSERT INTO ""AspNetUserRoles"" (""UserId"", ""RoleId"")
                SELECT su.""Id"", lr.""Id""
                FROM seed_users su
                CROSS JOIN lab_role lr
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM ""AspNetUserRoles"" ur
                    WHERE ur.""UserId"" = su.""Id""
                      AND ur.""RoleId"" = lr.""Id""
                );

                WITH missing_exam_types AS (
                    SELECT et.""Id"", et.""name""
                    FROM ""ExamTypes"" et
                    WHERE et.""deleteAt"" IS NULL
                      AND NOT EXISTS (
                          SELECT 1
                          FROM ""LaboratoryAttendantExamTypes"" lat
                          INNER JOIN ""Workers"" w ON w.""Id"" = lat.""attendantId""
                          WHERE lat.""examTypeId"" = et.""Id""
                            AND lat.""deleteAt"" IS NULL
                            AND w.""deleteAt"" IS NULL
                      )
                ),
                defaults AS (
                    SELECT
                        (SELECT ""Id"" FROM ""Sexs"" WHERE ""deleteAt"" IS NULL ORDER BY ""Id"" LIMIT 1) AS ""sexId"",
                        (SELECT ""Id"" FROM ""Nationalities"" WHERE ""deleteAt"" IS NULL ORDER BY ""Id"" LIMIT 1) AS ""nationalityId""
                )
                INSERT INTO ""Workers"" (
                    ""hiringDate"",
                    ""name"",
                    ""dpi"",
                    ""direction"",
                    ""birthday"",
                    ""sexId"",
                    ""nationalityId"",
                    ""userId"",
                    ""createAt""
                )
                SELECT
                    NOW(),
                    'Encargado de laboratorio - ' || m.""name"",
                    LPAD((9000000000000 + m.""Id"")::text, 13, '0'),
                    'Hospital',
                    (CURRENT_DATE - INTERVAL '25 years')::date,
                    d.""sexId"",
                    d.""nationalityId"",
                    'seed-lab-attendant-examtype-' || m.""Id"",
                    NOW()
                FROM missing_exam_types m
                CROSS JOIN defaults d
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM ""Workers"" w
                    WHERE w.""userId"" = 'seed-lab-attendant-examtype-' || m.""Id""
                );

                WITH missing_exam_types AS (
                    SELECT et.""Id""
                    FROM ""ExamTypes"" et
                    WHERE et.""deleteAt"" IS NULL
                      AND NOT EXISTS (
                          SELECT 1
                          FROM ""LaboratoryAttendantExamTypes"" lat
                          INNER JOIN ""Workers"" current_worker ON current_worker.""Id"" = lat.""attendantId""
                          WHERE lat.""examTypeId"" = et.""Id""
                            AND lat.""deleteAt"" IS NULL
                            AND current_worker.""deleteAt"" IS NULL
                      )
                )
                INSERT INTO ""LaboratoryAttendantExamTypes"" (""attendantId"", ""examTypeId"", ""createAt"")
                SELECT w.""Id"", m.""Id"", NOW()
                FROM missing_exam_types m
                INNER JOIN ""Workers"" w ON w.""userId"" = 'seed-lab-attendant-examtype-' || m.""Id""
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM ""LaboratoryAttendantExamTypes"" lat
                    WHERE lat.""attendantId"" = w.""Id""
                      AND lat.""examTypeId"" = m.""Id""
                      AND lat.""deleteAt"" IS NULL
                );
            ");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE FROM ""LaboratoryAttendantExamTypes"" lat
                USING ""Workers"" w
                WHERE lat.""attendantId"" = w.""Id""
                  AND w.""userId"" LIKE 'seed-lab-attendant-examtype-%'
                  AND NOT EXISTS (
                      SELECT 1
                      FROM ""Exams"" e
                      WHERE e.""attendantId"" = w.""Id""
                  );

                DELETE FROM ""Workers"" w
                WHERE w.""userId"" LIKE 'seed-lab-attendant-examtype-%'
                  AND NOT EXISTS (
                      SELECT 1
                      FROM ""Exams"" e
                      WHERE e.""attendantId"" = w.""Id""
                  );

                DELETE FROM ""AspNetUserRoles"" ur
                WHERE ur.""UserId"" LIKE 'seed-lab-attendant-examtype-%'
                  AND NOT EXISTS (
                      SELECT 1
                      FROM ""Workers"" w
                      WHERE w.""userId"" = ur.""UserId""
                  );

                DELETE FROM ""AspNetUsers"" u
                WHERE u.""Id"" LIKE 'seed-lab-attendant-examtype-%'
                  AND NOT EXISTS (
                      SELECT 1
                      FROM ""Workers"" w
                      WHERE w.""userId"" = u.""Id""
                  );
            ");

        }
    }
}
