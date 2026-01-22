using ClinicaMedica.Backend.Models;
using ClinicaMedica.Backend.Models.Dto;
using Microsoft.EntityFrameworkCore;

namespace ClinicaMedica.Backend.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Usuario> Usuarios => Set<Usuario>();
        public DbSet<Turno> Turnos => Set<Turno>();
 
        public DbSet<DoctorAgenda> DoctorAgenda { get; set; }
        public DbSet<Estudio> Estudios => Set<Estudio>();
        public DbSet<ObraSocial> ObraSocial => Set<ObraSocial>();
        public DbSet<DoctorObraSocial> DoctoresObrasSociales => Set<DoctorObraSocial>();
        public DbSet<DoctorEspecialidad> DoctoresEspecialidades => Set<DoctorEspecialidad>();
        public DbSet<Especialidad> Especialidades => Set<Especialidad>();

   
        public DbSet<DoctorFechasBloqueadas> DoctorFechasBloqueadas => Set<DoctorFechasBloqueadas>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Usuario>().ToTable("Usuarios");
            modelBuilder.Entity<Turno>().ToTable("Turnos");
            modelBuilder.Entity<Estudio>().ToTable("Estudios");

            modelBuilder.Entity<DoctorAgenda>().ToTable("DoctorAgenda");
            modelBuilder.Entity<DoctorFechasBloqueadas>().ToTable("DoctorFechasBloqueadas");

           
            // Turnos
     

            // Evita duplicados al generar (un turno por doctor por fecha/hora)
            modelBuilder.Entity<Turno>()
          .HasIndex(t => new { t.IdDoctor, t.FechaHora })
          .IsUnique();

            modelBuilder.Entity<Turno>()
             .HasOne(t => t.Doctor)
             .WithMany(u => u.TurnosComoDoctor)
             .HasForeignKey(t => t.IdDoctor)
             .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Turno>()
                .HasOne(t => t.Paciente)
                .WithMany(u => u.TurnosComoPaciente)
                .HasForeignKey(t => t.IdPaciente)
                .OnDelete(DeleteBehavior.Restrict);


            // Estudios
        
            modelBuilder.Entity<Estudio>()
                .HasOne(e => e.Paciente)
                .WithMany()
                .HasForeignKey(e => e.IdPaciente)
                .OnDelete(DeleteBehavior.Cascade);



            modelBuilder.Entity<DoctorAgenda>()
             .HasOne(a => a.Doctor)
             .WithMany(u => u.Agendas)
             .HasForeignKey(a => a.DoctorID)
             .OnDelete(DeleteBehavior.Restrict);

            // DoctorFechasBloqueadas


            // Guardar solo fecha (DATE) y no DATETIME
            modelBuilder.Entity<DoctorFechasBloqueadas>()
                .Property(x => x.Fecha)
                .HasColumnType("date");

            // Evita bloquear la misma fecha dos veces para el mismo doctor
            modelBuilder.Entity<DoctorFechasBloqueadas>()
                .HasIndex(x => new { x.DoctorID, x.Fecha })
                .IsUnique();

            modelBuilder.Entity<DoctorFechasBloqueadas>()
                .HasOne(f => f.Doctor)
                .WithMany()
                .HasForeignKey(f => f.DoctorID)
                .OnDelete(DeleteBehavior.Restrict);

            // DoctorObraSocial  (tabla real: DoctoresObrasSociales)
          
            modelBuilder.Entity<DoctorObraSocial>()
                .ToTable("DoctoresObrasSociales");

            modelBuilder.Entity<DoctorObraSocial>()
                .HasKey(x => new { x.DoctorID, x.OSID });

            modelBuilder.Entity<DoctorObraSocial>()
                .HasOne(x => x.Doctor)
                .WithMany(u => u.ObrasSociales)
                .HasForeignKey(x => x.DoctorID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DoctorObraSocial>()
                .HasOne(x => x.ObraSocial)
                .WithMany()
                .HasForeignKey(x => x.OSID)
                .OnDelete(DeleteBehavior.Cascade);

            
            // DoctorEspecialidad (TIENE ID → PK normal)
            modelBuilder.Entity<DoctorEspecialidad>()
                .ToTable("DoctorEspecialidad");

            modelBuilder.Entity<DoctorEspecialidad>()
                .HasKey(x => x.ID);

            modelBuilder.Entity<DoctorEspecialidad>()
                .HasOne(x => x.Doctor)
                .WithMany(u => u.Especialidades)
                .HasForeignKey(x => x.DoctorID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DoctorEspecialidad>()
                .HasOne(x => x.Especialidad)
                .WithMany()
                .HasForeignKey(x => x.EspID)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
