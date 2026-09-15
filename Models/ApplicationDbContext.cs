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

        // --- CURSOS Y HORARIOS ---
        public DbSet<Curso> Cursos { get; set; }

        // --- ACTIVIDADES EXTRACURRICULARES ---
        public DbSet<ExtraCurso> ExtraCursos { get; set; }
        public DbSet<ExtraInscripcion> ExtraInscripciones { get; set; }

        // --- ACTIVIDADES ACADÉMICAS Y ENTREGAS ---
        public DbSet<Actividad> Actividades { get; set; }
        public DbSet<EntregaActividad> EntregasActividades { get; set; }

        // ── Recuperación de contraseña ──
        public DbSet<GU_RECUPERACION_PASSWORD> RecuperacionesPassword { get; set; }
        public DbSet<GU_HISTORIAL_PASSWORD> HistorialPasswords { get; set; }

        public DbSet<GU_IntentosLogin> IntentosLogin { get; set; }
        // --- FIRMA DE CERTIFICADOS ---
        public DbSet<Certificado> Certificados { get; set; }
        public DbSet<CertificadoDetalle> CertificadosDetalle { get; set; }

        // --- NOTIFICACIONES Y ANUNCIOS GLOBALES ---
        public DbSet<Notificacion> Notificaciones { get; set; }
        public DbSet<NotificacionLeida> NotificacionesLeidas { get; set; }

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

        public void AsegurarEsquemaNotificaciones()
        {
            try
            {
                Database.ExecuteSqlRaw(@"
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[GU_NOTIFICACION]') AND name = 'Prioridad')
                    BEGIN
                        ALTER TABLE [dbo].[GU_NOTIFICACION] ADD [Prioridad] NVARCHAR(20) NOT NULL DEFAULT 'leve';
                    END

                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[GU_NOTIFICACION]') AND name = 'FechaExpiracion')
                    BEGIN
                        ALTER TABLE [dbo].[GU_NOTIFICACION] ADD [FechaExpiracion] DATETIME NULL;
                    END

                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'GU_NOTIFICACION_LEIDA')
                    BEGIN
                        CREATE TABLE [dbo].[GU_NOTIFICACION_LEIDA](
                            [ID_NotificacionLeida] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
                            [ID_Notificacion] [int] NOT NULL,
                            [ID_Usuario] [int] NOT NULL,
                            [FechaLeido] [datetime] NOT NULL DEFAULT GETDATE()
                        );
                    END
                ");
            }
            catch { }
        }

        public void AsegurarEsquemaExtracurriculares()
        {
            try
            {
                Database.ExecuteSqlRaw(@"
                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'EXTRA_CURSO')
                    BEGIN
                        CREATE TABLE [dbo].[EXTRA_CURSO](
                            [id_extra_curso] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
                            [nombre] [nvarchar](100) NOT NULL,
                            [descripcion] [nvarchar](500) NULL,
                            [instructor] [nvarchar](150) NULL,
                            [cupos_totales] [int] NOT NULL,
                            [cupos_disponibles] [int] NOT NULL,
                            [fecha_inicio] [datetime] NOT NULL,
                            [fecha_fin] [datetime] NOT NULL,
                            [fecha_inicio_inscripcion] [datetime] NOT NULL,
                            [fecha_fin_inscripcion] [datetime] NOT NULL,
                            [activo] [bit] NOT NULL DEFAULT 1,
                            [id_periodo] [int] NULL,
                            [id_aula] [int] NULL,
                            [grado_min] [int] NULL DEFAULT 6,
                            [grado_max] [int] NULL DEFAULT 11,
                            [horario] [nvarchar](100) NULL,
                            [id_docente] [int] NULL
                        );
                    END
                    ELSE
                    BEGIN
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[EXTRA_CURSO]') AND name = 'grado_min')
                            ALTER TABLE [dbo].[EXTRA_CURSO] ADD [grado_min] INT NULL DEFAULT 6;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[EXTRA_CURSO]') AND name = 'grado_max')
                            ALTER TABLE [dbo].[EXTRA_CURSO] ADD [grado_max] INT NULL DEFAULT 11;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[EXTRA_CURSO]') AND name = 'horario')
                            ALTER TABLE [dbo].[EXTRA_CURSO] ADD [horario] NVARCHAR(100) NULL;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[EXTRA_CURSO]') AND name = 'id_docente')
                            ALTER TABLE [dbo].[EXTRA_CURSO] ADD [id_docente] INT NULL;
                    END

                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'EXTRA_INSCRIPCION')
                    BEGIN
                        CREATE TABLE [dbo].[EXTRA_INSCRIPCION](
                            [id_extra_inscripcion] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
                            [id_estudiante] [int] NOT NULL,
                            [id_extra_curso] [int] NOT NULL,
                            [fecha_inscripcion] [datetime] NOT NULL DEFAULT GETDATE(),
                            [estado] [nvarchar](30) NOT NULL DEFAULT 'Inscrito',
                            [fecha_cancelacion] [datetime] NULL,
                            [motivo_cancelacion] [nvarchar](300) NULL,
                            [id_extra_curso_nuevo] [int] NULL
                        );
                    END
                ");
            }
            catch { }
        }

        public void AsegurarEsquemaActividades()
        {
            try
            {
                Database.ExecuteSqlRaw(@"
                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ACTIVIDAD')
                    BEGIN
                        CREATE TABLE [dbo].[ACTIVIDAD](
                            [id_actividad] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
                            [titulo] [nvarchar](150) NOT NULL,
                            [descripcion] [nvarchar](max) NULL,
                            [materia] [nvarchar](100) NOT NULL,
                            [grado] [nvarchar](50) NULL,
                            [fecha_limite] [datetime] NOT NULL,
                            [id_docente] [int] NULL,
                            [archivo_adjunto] [nvarchar](255) NULL,
                            [fecha_creacion] [datetime] NOT NULL DEFAULT GETDATE(),
                            [activo] [bit] NOT NULL DEFAULT 1
                        );
                    END

                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ENTREGA_ACTIVIDAD')
                    BEGIN
                        CREATE TABLE [dbo].[ENTREGA_ACTIVIDAD](
                            [id_entrega] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
                            [id_actividad] [int] NOT NULL,
                            [id_estudiante] [int] NOT NULL,
                            [archivo_ruta] [nvarchar](255) NULL,
                            [archivo_nombre] [nvarchar](255) NULL,
                            [comentario] [nvarchar](max) NULL,
                            [fecha_entrega] [datetime] NOT NULL DEFAULT GETDATE(),
                            [calificacion] [decimal](3,1) NULL,
                            [retroalimentacion] [nvarchar](max) NULL,
                            [estado] [nvarchar](50) NOT NULL DEFAULT 'Entregado'
                        );
                    END
                ");
            }
            catch { }
        }
    }
}