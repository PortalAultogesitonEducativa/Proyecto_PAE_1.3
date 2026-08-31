using Microsoft.EntityFrameworkCore;
using ProyectoPAE.Models;

namespace ProyectoPAE.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<EstudiantePadre> ESTUDIANTE_PADRE { get; set; }
        public DbSet<Asistencia> ASISTENCIAS { get; set; }
        public DbSet<Evaluacion> EVALUACIONES { get; set; }
        public DbSet<Citacion> CITACIONES { get; set; }
        public DbSet<Calificacion> Calificaciones { get; set; }
        public DbSet<Matricula> Matriculas { get; set; }
        public DbSet<Estudiante> ESTUDIANTE { get; set; }
        public DbSet<PadreTutor> PADRE_TUTOR { get; set; }
        public DbSet<Profesor> PROFESOR { get; set; }
        public DbSet<Materia> MATERIA { get; set; }
        public DbSet<Horario> HORARIOS { get; set; }
        public DbSet<Aula> AULA { get; set; }
        public DbSet<ProfesorMateria> PROFESOR_MATERIA { get; set; }

        // ── Recuperación de contraseña ──
        public DbSet<GU_RECUPERACION_PASSWORD> RecuperacionesPassword { get; set; }
        public DbSet<GU_HISTORIAL_PASSWORD> HistorialPasswords { get; set; }

        // --- FIRMA DE CERTIFICADOS ---
        public DbSet<Certificado> Certificados { get; set; }
        public DbSet<CertificadoDetalle> CertificadosDetalle { get; set; }

        // --- NOTIFICACIONES Y ANUNCIOS GLOBALES ---
        public DbSet<Notificacion> Notificaciones { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ESTUDIANTE_PADRE no tiene PK propia en la base de datos:
            // se usa la combinación de ID_PADRE + ID_ESTUDIANTE como clave compuesta.
            modelBuilder.Entity<EstudiantePadre>()
                .HasKey(ep => new { ep.ID_PADRE, ep.ID_ESTUDIANTE });

            // ID_PADRE referencia a la tabla legacy PADRE_TUTOR (confirmado por FK_EP_PADRE),
            // no directamente a GU_Usuario.
            modelBuilder.Entity<EstudiantePadre>()
                .HasOne<PadreTutor>()
                .WithMany()
                .HasForeignKey(ep => ep.ID_PADRE)
                .OnDelete(DeleteBehavior.Restrict);

            // ID_ESTUDIANTE referencia a la tabla legacy ESTUDIANTE (confirmado por FK_EP_EST),
            // no a GU_Usuario.
            modelBuilder.Entity<EstudiantePadre>()
                .HasOne<Estudiante>()
                .WithMany()
                .HasForeignKey(ep => ep.ID_ESTUDIANTE)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}