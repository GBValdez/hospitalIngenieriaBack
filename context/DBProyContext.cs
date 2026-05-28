using back.catalogues;
using fletesProyect.Appointment;
using fletesProyect.catalogues;
using fletesProyect.Dispatch;
using fletesProyect.DoctorSpecialty;
using fletesProyect.Exam;
using fletesProyect.Medicine;
using fletesProyect.Patient;
using fletesProyect.Recipe;
using fletesProyect.Worker;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using project.ModelsDto;
using project.roles;
using project.users;
using project.users.Models;
using project.utils.catalogue;

namespace project.Models;

public partial class DBProyContext : IdentityDbContext<userEntity, rolEntity, string>
{
    IConfiguration _configuration;
    public DBProyContext(DbContextOptions<DBProyContext> options, IConfiguration configuration)
        : base(options)
    {
        _configuration = configuration;
    }
    public DbSet<binnacleBody> BinnacleBodies { get; set; }
    public DbSet<binnacleHeader> BinnacleHeaders { get; set; }
    public DbSet<Appointment> Appointments { get; set; }
    public DbSet<Recipe> Recipes { get; set; }
    public DbSet<Dispatch> Dispatchs { get; set; }
    public DbSet<Patient> Patients { get; set; }
    public DbSet<Worker> Workers { get; set; }
    public DbSet<Exam> Exams { get; set; }
    public DbSet<Medicine> Medicines { get; set; }
    public DbSet<Brand> Brands { get; set; }
    public DbSet<Sex> Sexs { get; set; }
    public DbSet<Nationality> Nationalities { get; set; }
    public DbSet<DoctorSpecialty> doctorSpecialties { get; set; }
    public DbSet<ExamType> ExamTypes { get; set; }
    public DbSet<Specialty> Specialtys { get; set; }
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseNpgsql(_configuration.GetConnectionString("DefaultConnection"));

}
