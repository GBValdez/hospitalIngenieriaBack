using fletesProyect.DoctorSpecialty;
using fletesProyect.LaboratoryAttendantExamType;
using fletesProyect.Patient;
using fletesProyect.Worker;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using project.Models;
using project.roles;

namespace project.users
{
    public static class testUserSeeder
    {
        private const string DefaultPassword = "Hospital@123";
        private static readonly string[] Roles = { "ADMINISTRATOR", "userNormal", "DOCTOR", "NURSE", "LAB_ATTENDANT" };

        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using IServiceScope scope = serviceProvider.CreateScope();
            DBProyContext context = scope.ServiceProvider.GetRequiredService<DBProyContext>();
            UserManager<userEntity> userManager = scope.ServiceProvider.GetRequiredService<UserManager<userEntity>>();
            RoleManager<rolEntity> roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<rolEntity>>();

            await context.Database.MigrateAsync();

            await EnsureRoles(roleManager);
            SeedCatalogues(context);
            await context.SaveChangesAsync();

            long sexId = await context.Sexs.Where(x => x.deleteAt == null).OrderBy(x => x.Id).Select(x => x.Id).FirstAsync();
            long nationalityId = await context.Nationalities.Where(x => x.deleteAt == null).OrderBy(x => x.Id).Select(x => x.Id).FirstAsync();
            long specialtyId = await context.Specialtys.Where(x => x.deleteAt == null).OrderBy(x => x.Id).Select(x => x.Id).FirstAsync();
            long examTypeId = await context.ExamTypes.Where(x => x.deleteAt == null).OrderBy(x => x.Id).Select(x => x.Id).FirstAsync();

            foreach (string role in Roles)
            {
                for (int index = 1; index <= 10; index++)
                {
                    userEntity user = await EnsureUser(userManager, role, index);
                    await EnsureUserRole(userManager, user, role);
                    await EnsureProfile(context, role, index, user.Id, sexId, nationalityId, specialtyId, examTypeId);
                }
            }

            await context.SaveChangesAsync();
        }

        private static async Task EnsureRoles(RoleManager<rolEntity> roleManager)
        {
            foreach (string role in Roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new rolEntity { Name = role, createAt = DateTime.UtcNow });
            }
        }

        private static void SeedCatalogues(DBProyContext context)
        {
            if (!context.Sexs.Any(x => x.deleteAt == null))
                context.Sexs.Add(new fletesProyect.catalogues.Sex { name = "No especificado", description = "Valor creado para usuarios de prueba.", createAt = DateTime.UtcNow });

            if (!context.Nationalities.Any(x => x.deleteAt == null))
                context.Nationalities.Add(new fletesProyect.catalogues.Nationality { name = "Guatemalteca", description = "Valor creado para usuarios de prueba.", createAt = DateTime.UtcNow });

            if (!context.Specialtys.Any(x => x.deleteAt == null))
                context.Specialtys.Add(new fletesProyect.catalogues.Specialty { name = "Medicina general", description = "Valor creado para usuarios de prueba.", createAt = DateTime.UtcNow });

            if (!context.ExamTypes.Any(x => x.deleteAt == null))
                context.ExamTypes.Add(new fletesProyect.catalogues.ExamType { name = "Laboratorio general", description = "Valor creado para usuarios de prueba.", createAt = DateTime.UtcNow });
        }

        private static async Task<userEntity> EnsureUser(UserManager<userEntity> userManager, string role, int index)
        {
            string prefix = GetPrefix(role);
            string userName = $"{prefix}{index:00}";
            string email = $"{userName}@hospital.local";
            userEntity user = await userManager.FindByNameAsync(userName);

            if (user == null)
            {
                user = new userEntity
                {
                    UserName = userName,
                    Email = email,
                    EmailConfirmed = true,
                    PhoneNumber = $"5550{index:000000}",
                    createAt = DateTime.UtcNow
                };

                IdentityResult result = await userManager.CreateAsync(user, DefaultPassword);
                if (!result.Succeeded)
                    throw new InvalidOperationException($"No se pudo crear el usuario {userName}: {string.Join(", ", result.Errors.Select(x => x.Description))}");
            }
            else
            {
                user.EmailConfirmed = true;
                user.deleteAt = null;
                await userManager.UpdateAsync(user);
            }

            return user;
        }

        private static async Task EnsureUserRole(UserManager<userEntity> userManager, userEntity user, string role)
        {
            if (!await userManager.IsInRoleAsync(user, role))
                await userManager.AddToRoleAsync(user, role);
        }

        private static async Task EnsureProfile(
            DBProyContext context,
            string role,
            int index,
            string userId,
            long sexId,
            long nationalityId,
            long specialtyId,
            long examTypeId)
        {
            if (role == "userNormal")
            {
                if (!await context.Patients.AnyAsync(x => x.userId == userId))
                {
                    context.Patients.Add(new Patient
                    {
                        name = $"Paciente Prueba {index:00}",
                        dpi = $"9100000000{index:000}",
                        direction = "Ciudad de Guatemala",
                        birthday = new DateOnly(1990, 1, Math.Min(index, 28)),
                        sexId = sexId,
                        nationalityId = nationalityId,
                        userId = userId,
                        createAt = DateTime.UtcNow
                    });
                }

                return;
            }

            if (role != "DOCTOR" && role != "NURSE" && role != "LAB_ATTENDANT")
                return;

            Worker worker = await context.Workers.FirstOrDefaultAsync(x => x.userId == userId);
            if (worker == null)
            {
                worker = new Worker
                {
                    name = $"{GetDisplayRole(role)} Prueba {index:00}",
                    dpi = $"2000000001{RoleOffset(role)}{index:00}",
                    direction = "Hospital",
                    birthday = new DateOnly(1985, 1, Math.Min(index, 28)),
                    sexId = sexId,
                    nationalityId = nationalityId,
                    hiringDate = DateTime.UtcNow.AddYears(-1),
                    userId = userId,
                    createAt = DateTime.UtcNow
                };

                context.Workers.Add(worker);
                await context.SaveChangesAsync();
            }
            else
            {
                worker.deleteAt = null;
            }

            if (role == "DOCTOR" && !await context.doctorSpecialties.AnyAsync(x => x.doctorId == worker.Id && x.deleteAt == null))
            {
                context.doctorSpecialties.Add(new DoctorSpecialty
                {
                    doctorId = worker.Id,
                    specialtyId = specialtyId,
                    licenseNumber = $"MED-{index:0000}",
                    createAt = DateTime.UtcNow
                });
            }

            if (role == "LAB_ATTENDANT" && !await context.LaboratoryAttendantExamTypes.AnyAsync(x => x.attendantId == worker.Id && x.deleteAt == null))
            {
                context.LaboratoryAttendantExamTypes.Add(new LaboratoryAttendantExamType
                {
                    attendantId = worker.Id,
                    examTypeId = examTypeId,
                    createAt = DateTime.UtcNow
                });
            }
        }

        private static string GetPrefix(string role)
        {
            return role switch
            {
                "ADMINISTRATOR" => "admin.prueba",
                "userNormal" => "paciente.prueba",
                "DOCTOR" => "doctor.prueba",
                "NURSE" => "enfermera.prueba",
                "LAB_ATTENDANT" => "laboratorio.prueba",
                _ => "usuario.prueba"
            };
        }

        private static string GetDisplayRole(string role)
        {
            return role switch
            {
                "DOCTOR" => "Doctor",
                "NURSE" => "Enfermera",
                "LAB_ATTENDANT" => "Laboratorio",
                _ => "Usuario"
            };
        }

        private static int RoleOffset(string role)
        {
            return role switch
            {
                "DOCTOR" => 1,
                "NURSE" => 2,
                "LAB_ATTENDANT" => 3,
                _ => 9
            };
        }
    }
}

