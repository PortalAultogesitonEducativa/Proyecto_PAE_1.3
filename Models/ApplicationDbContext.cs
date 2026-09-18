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
        public DbSet<ExtraDocumentoInscripcion> ExtraDocumentosInscripciones { get; set; }

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

        // --- OBSERVADOR DEL ALUMNO ---
        public DbSet<ObservacionEstudiante> ObservacionesEstudiante { get; set; }

        // --- ACCIONES DE MEJORA Y SEGUIMIENTO ---
        public DbSet<AccionMejora> AccionesMejora { get; set; }

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

        public void AsegurarEsquemaEstudiantes()
        {
            try
            {
                Database.ExecuteSqlRaw(@"
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ESTUDIANTE]') AND name = 'id_curso')
                    BEGIN
                        EXEC(N'ALTER TABLE [dbo].[ESTUDIANTE] ADD [id_curso] INT NULL;');
                    END

                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ESTUDIANTE]') AND name = 'curso_asignado')
                    BEGIN
                        EXEC(N'ALTER TABLE [dbo].[ESTUDIANTE] ADD [curso_asignado] NVARCHAR(50) NULL;');
                    END

                    IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ESTUDIANTE]') AND name = 'id_curso')
                    BEGIN
                        EXEC(N'
                            UPDATE e
                            SET e.id_curso = m.id_curso,
                                e.curso_asignado = c.nombre_curso
                            FROM [dbo].[ESTUDIANTE] e
                            INNER JOIN [dbo].[MATRICULA] m ON e.id_estudiante = m.id_estudiante
                            LEFT JOIN [dbo].[CURSO] c ON m.id_curso = c.id_curso
                            WHERE e.id_curso IS NULL;
                        ');
                    END
                ");
            }
            catch { }
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
                            [id_docente] [int] NULL,
                            [tipo_curso] [nvarchar](50) NULL DEFAULT 'Deportivo',
                            [documentos_requeridos] [nvarchar](500) NULL
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
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[EXTRA_CURSO]') AND name = 'tipo_curso')
                            ALTER TABLE [dbo].[EXTRA_CURSO] ADD [tipo_curso] NVARCHAR(50) NULL DEFAULT 'Deportivo';
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[EXTRA_CURSO]') AND name = 'documentos_requeridos')
                            ALTER TABLE [dbo].[EXTRA_CURSO] ADD [documentos_requeridos] NVARCHAR(500) NULL;
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

                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'EXTRA_DOCUMENTO_INSCRIPCION')
                    BEGIN
                        CREATE TABLE [dbo].[EXTRA_DOCUMENTO_INSCRIPCION](
                            [id_documento] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
                            [id_extra_inscripcion] [int] NOT NULL,
                            [tipo_documento] [nvarchar](100) NOT NULL,
                            [archivo_ruta] [nvarchar](255) NOT NULL,
                            [archivo_nombre] [nvarchar](255) NOT NULL,
                            [fecha_subida] [datetime] NOT NULL DEFAULT GETDATE(),
                            [estado] [nvarchar](30) NOT NULL DEFAULT 'Cargado'
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

        public void AsegurarEsquemaObservador()
        {
            try
            {
                Database.ExecuteSqlRaw(@"
                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'OBSERVACION_ESTUDIANTE')
                    BEGIN
                        CREATE TABLE [dbo].[OBSERVACION_ESTUDIANTE](
                            [id_observacion] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
                            [id_estudiante] [int] NOT NULL,
                            [docente] [nvarchar](150) NULL,
                            [tipo_nota] [nvarchar](50) NOT NULL,
                            [descripcion] [nvarchar](max) NOT NULL,
                            [aspectos_mejorar] [nvarchar](max) NULL,
                            [quien_registra] [nvarchar](150) NULL,
                            [periodo] [int] NOT NULL DEFAULT 1,
                            [fecha] [datetime] NOT NULL DEFAULT GETDATE()
                        );
                    END
                    ELSE
                    BEGIN
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[OBSERVACION_ESTUDIANTE]') AND name = 'aspectos_mejorar')
                            ALTER TABLE [dbo].[OBSERVACION_ESTUDIANTE] ADD [aspectos_mejorar] NVARCHAR(MAX) NULL;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[OBSERVACION_ESTUDIANTE]') AND name = 'quien_registra')
                            ALTER TABLE [dbo].[OBSERVACION_ESTUDIANTE] ADD [quien_registra] NVARCHAR(150) NULL;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[OBSERVACION_ESTUDIANTE]') AND name = 'periodo')
                            ALTER TABLE [dbo].[OBSERVACION_ESTUDIANTE] ADD [periodo] INT NOT NULL DEFAULT 1;
                    END

                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ASISTENCIA]') AND name = 'justificacion')
                    BEGIN
                        ALTER TABLE [dbo].[ASISTENCIA] ADD [justificacion] NVARCHAR(MAX) NULL;
                    END
                ");
            }
            catch { }
        }

        public void AsegurarEsquemaAccionesMejora()
        {
            try
            {
                Database.ExecuteSqlRaw(@"
                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ACCION_MEJORA')
                    BEGIN
                        CREATE TABLE [dbo].[ACCION_MEJORA](
                            [id_mejora] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
                            [id_estudiante] [int] NOT NULL,
                            [nombre_estudiante] [nvarchar](150) NULL,
                            [id_docente] [int] NULL,
                            [docente] [nvarchar](150) NULL,
                            [materia] [nvarchar](100) NOT NULL,
                            [grado] [nvarchar](50) NULL,
                            [periodo] [int] NOT NULL DEFAULT 1,
                            [aspecto_mejorar] [nvarchar](max) NOT NULL,
                            [compromiso_estudiante] [nvarchar](max) NOT NULL,
                            [fecha_registro] [datetime] NOT NULL DEFAULT GETDATE(),
                            [fecha_compromiso] [datetime] NULL,
                            [estado_seguimiento] [nvarchar](50) NOT NULL DEFAULT 'En Proceso',
                            [observacion_seguimiento] [nvarchar](max) NULL,
                            [fecha_seguimiento] [datetime] NULL,
                            [respuesta_estudiante] [nvarchar](max) NULL,
                            [fecha_respuesta_estudiante] [datetime] NULL,
                            [activo] [bit] NOT NULL DEFAULT 1
                        );
                    END
                    ELSE
                    BEGIN
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ACCION_MEJORA]') AND name = 'respuesta_estudiante')
                            ALTER TABLE [dbo].[ACCION_MEJORA] ADD [respuesta_estudiante] NVARCHAR(MAX) NULL;
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ACCION_MEJORA]') AND name = 'fecha_respuesta_estudiante')
                            ALTER TABLE [dbo].[ACCION_MEJORA] ADD [fecha_respuesta_estudiante] DATETIME NULL;
                    END
                ");
            }
            catch { }
        }
    }
}