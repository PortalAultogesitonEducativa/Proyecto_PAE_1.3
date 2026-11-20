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
        public DbSet<Asistencia> Asistencias => ASISTENCIAS;
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
        public DbSet<Logro> Logros { get; set; }

        // --- CURSOS Y HORARIOS ---
        public DbSet<Curso> Cursos { get; set; }
        public DbSet<CursoMateria> CURSO_MATERIA { get; set; }

        // --- ACTIVIDADES EXTRACURRICULARES ---
        public DbSet<ExtraCurso> ExtraCursos { get; set; }
        public DbSet<ExtraInscripcion> ExtraInscripciones { get; set; }
        public DbSet<ExtraDocumentoInscripcion> ExtraDocumentosInscripciones { get; set; }

        // --- ACTIVIDADES ACADÉMICAS Y ENTREGAS ---
        public DbSet<Actividad> Actividades { get; set; }
        public DbSet<EntregaActividad> EntregasActividades { get; set; }
        public DbSet<ComentarioActividad> ComentariosActividades { get; set; }

        // ── Recuperación de contraseña ──
        public DbSet<GU_RECUPERACION_PASSWORD> RecuperacionesPassword { get; set; }
        public DbSet<GU_HISTORIAL_PASSWORD> HistorialPasswords { get; set; }

        public DbSet<GU_IntentosLogin> IntentosLogin { get; set; }
        // --- FIRMA DE CERTIFICADOS ---
        public DbSet<Certificado> Certificados { get; set; }
        public DbSet<CertificadoDetalle> CertificadosDetalle { get; set; }
        public DbSet<CertificadoEstudioExterno> CertificadosEstudioExterno { get; set; }


        // --- NOTIFICACIONES Y ANUNCIOS GLOBALES ---
        public DbSet<Notificacion> Notificaciones { get; set; }
        public DbSet<NotificacionLeida> NotificacionesLeidas { get; set; }

        // --- OBSERVADOR DEL ALUMNO ---
        public DbSet<ObservacionEstudiante> ObservacionesEstudiante { get; set; }

        // --- ACCIONES DE MEJORA Y SEGUIMIENTO ---
        public DbSet<AccionMejora> AccionesMejora { get; set; }

        // --- SISTEMA PQR (PETICIONES, QUEJAS Y RECLAMOS) ---
        public DbSet<Pqr> PQRs { get; set; }
        public DbSet<SolicitudCertificado> SolicitudesCertificado { get; set; }
        public DbSet<CambioCorreo> CambiosCorreo { get; set; }

        // --- PERIODOS DE SUBIDA DE NOTAS (COORDINACIÓN) ---
        public DbSet<PeriodoSubidaNota> PeriodosSubidaNotas { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Informar a EF Core que GU_Usuario tiene triggers de base de datos
            // para que no use la cláusula OUTPUT en INSERT/UPDATE.
            modelBuilder.Entity<Usuario>()
                .ToTable("GU_Usuario", tb => tb.HasTrigger("trg_GU_Usuario_InvalidarTokensPorDesactivacion"));

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

            // Relación directa entre ESTUDIANTE y PADRE_TUTOR
            modelBuilder.Entity<Estudiante>()
                .HasOne(e => e.Padre)
                .WithMany()
                .HasForeignKey(e => e.id_padre)
                .OnDelete(DeleteBehavior.Restrict);

            // PROFESOR_MATERIA tiene clave compuesta id_profesor + id_materia
            modelBuilder.Entity<ProfesorMateria>()
                .HasKey(pm => new { pm.id_profesor, pm.id_materia });
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

                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ESTUDIANTE]') AND name = 'id_padre')
                    BEGIN
                        EXEC(N'ALTER TABLE [dbo].[ESTUDIANTE] ADD [id_padre] INT NULL;');
                    END

                    IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_ESTUDIANTE_PADRE_DIRECT')
                    BEGIN
                        EXEC(N'ALTER TABLE [dbo].[ESTUDIANTE] WITH CHECK ADD CONSTRAINT [FK_ESTUDIANTE_PADRE_DIRECT] FOREIGN KEY([id_padre]) REFERENCES [dbo].[PADRE_TUTOR] ([id_padre]);');
                    END

                    EXEC(N'
                        UPDATE e
                        SET e.id_padre = ep.id_padre
                        FROM [dbo].[ESTUDIANTE] e
                        INNER JOIN (
                            SELECT id_estudiante, id_padre,
                                   ROW_NUMBER() OVER (PARTITION BY id_estudiante ORDER BY es_principal DESC, id_padre ASC) as rn
                            FROM [dbo].[ESTUDIANTE_PADRE]
                        ) ep ON e.id_estudiante = ep.id_estudiante AND ep.rn = 1
                        WHERE e.id_padre IS NULL;
                    ');

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

        /// <summary>
        /// Garantiza que la tabla HORARIO tenga la columna 'publicado' para manejar borradores antes de confirmar.
        /// Los registros antiguos creados antes de esta actualización quedan en 1 (publicados).
        /// </summary>
        public void AsegurarEsquemaHorarios()
        {
            try
            {
                Database.ExecuteSqlRaw(@"
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[HORARIO]') AND name = 'publicado')
                    BEGIN
                        ALTER TABLE [dbo].[HORARIO] ADD [publicado] BIT NOT NULL CONSTRAINT DF_HORARIO_PUBLICADO DEFAULT 1;
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

                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('ACTIVIDAD') AND name = 'es_mejora')
                    BEGIN
                        ALTER TABLE [dbo].[ACTIVIDAD] ADD [es_mejora] [bit] NOT NULL DEFAULT 0;
                    END

                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'COMENTARIO_ACTIVIDAD')
                    BEGIN
                        CREATE TABLE [dbo].[COMENTARIO_ACTIVIDAD](
                            [id_comentario] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
                            [id_actividad] [int] NOT NULL,
                            [id_usuario] [int] NOT NULL,
                            [nombre_usuario] [nvarchar](150) NOT NULL,
                            [rol_usuario] [nvarchar](50) NOT NULL,
                            [mensaje] [nvarchar](max) NOT NULL,
                            [fecha] [datetime] NOT NULL DEFAULT GETDATE(),
                            [es_privado] [bit] NOT NULL DEFAULT 0,
                            [id_estudiante_privado] [int] NULL
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

        /// <summary>
        /// Crea la tabla LOGRO si no existe y precarga logros base para grados 6° a 11° y períodos 1 a 4.
        /// </summary>
        public void AsegurarEsquemaLogros()
        {
            try
            {
                Database.ExecuteSqlRaw(@"
                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'LOGRO')
                    BEGIN
                        CREATE TABLE [dbo].[LOGRO](
                            [id_logro] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
                            [grado] [int] NOT NULL,
                            [materia] [nvarchar](100) NOT NULL,
                            [periodo] [int] NOT NULL DEFAULT 1,
                            [codigo_logro] [nvarchar](30) NULL,
                            [descripcion] [nvarchar](500) NOT NULL,
                            [activo] [bit] NOT NULL DEFAULT 1
                        );
                    END
                ");

                if (!Logros.Any())
                {
                    var logrosIniciales = new List<Logro>();

                    // Banco base por materia y grado (6° a 11°) con progresión para los 4 períodos
                    var catalogo = new Dictionary<string, Dictionary<int, string[]>>
                    {
                        ["Matemáticas"] = new()
                        {
                            [6] = new[] {
                                "Resuelve operaciones y problemas cotidianos con números naturales y sus propiedades.",
                                "Aplica criterios de divisibilidad, MCM y MCD en la solución de situaciones problema.",
                                "Opera números fraccionarios y decimales interpretando su representación gráfica y numérica.",
                                "Calcula perímetros, áreas de figuras planas y analiza datos estadísticos básicos."
                            },
                            [7] = new[] {
                                "Realiza operaciones con números enteros y los ubica correctamente en la recta numérica.",
                                "Resuelve problemas cotidianos aplicando operaciones básicas con números racionales.",
                                "Aplica razones, proporciones y regla de tres simple en contextos reales.",
                                "Calcula porcentajes, probabilidad básica y medidas de tendencia central."
                            },
                            [8] = new[] {
                                "Identifica y opera expresiones algebraicas (monomios y polinomios) con precisión.",
                                "Aplica productos y cocientes notables en la simplificación de expresiones algebraicas.",
                                "Descompone expresiones mediante los principales casos de factorización.",
                                "Plantea y resuelve ecuaciones lineales de primer grado con una incógnita."
                            },
                            [9] = new[] {
                                "Resuelve sistemas de ecuaciones lineales 2x2 mediante métodos gráficos y algebraicos.",
                                "Aplica propiedades de potenciación, radicación y números complejos en ejercicios prácticos.",
                                "Analiza y grafica funciones lineales y cuadráticas identificando sus elementos clave.",
                                "Resuelve ecuaciones cuadráticas y aplica teoremas geométricos de semejanza."
                            },
                            [10] = new[] {
                                "Convierte medidas angulares y aplica razones trigonométricas en triángulos rectángulos.",
                                "Resuelve triángulos oblicuángulos aplicando los teoremas del seno y del coseno.",
                                "Grafica funciones trigonométricas identificando amplitud, período y desplazamiento.",
                                "Demuestra identidades trigonométricas fundamentales y analiza secciones cónicas."
                            },
                            [11] = new[] {
                                "Analiza inecuaciones, intervalos y propiedades de funciones reales de variable real.",
                                "Calcula límites de funciones aplicando propiedades algebraicas y analiza continuidad.",
                                "Aplica reglas de derivación para determinar tasas de cambio y rectas tangentes.",
                                "Resuelve problemas de optimización y análisis de gráficas mediante criterios de derivada."
                            }
                        },
                        ["Español y Literatura"] = new()
                        {
                            [6] = new[] {
                                "Identifica elementos y estructura de textos narrativos como mitos, leyendas y cuentos.",
                                "Reconoce categorías gramaticales básicas y aplica reglas generales de acentuación.",
                                "Comprende textos informativos y expositivos extrayendo ideas principales y secundarias.",
                                "Produce textos cortos coherentes empleando signos de puntuación adecuadamente."
                            },
                            [7] = new[] {
                                "Analiza géneros literarios narrativos y líricos reconociendo figuras retóricas básicas.",
                                "Redacta párrafos expositivos con cohesión, conectores lógicos y buena ortografía.",
                                "Interpreta textos periodísticos y diferencia hechos de opiniones en medios de comunicación.",
                                "Participa en exposiciones orales argumentando sus ideas con claridad y respeto."
                            },
                            [8] = new[] {
                                "Analiza obras de la literatura precolombina y colonial en Colombia.",
                                "Comprende textos del Romanticismo y Costumbrismo relacionándolos con su contexto histórico.",
                                "Produce reseñas críticas y textos argumentativos con estructura tesis-argumentos.",
                                "Aplica normas ortográficas avanzadas y mejora su expresión oral en debates grupales."
                            },
                            [9] = new[] {
                                "Interpreta movimientos literarios latinoamericanos desde el Modernismo hasta el Boom.",
                                "Analiza recursos estilísticos y simbólicos en textos narrativos y poéticos.",
                                "Redacta ensayos cortos sustentando posturas críticas con fuentes confiables.",
                                "Evalúa discursos argumentativos identificando falacias e intención comunicativa."
                            },
                            [10] = new[] {
                                "Analiza obras representativas de la literatura española desde la Edad Media al Siglo de Oro.",
                                "Interpreta textos filosóficos y ensayísticos aplicando lectura inferencial y crítica.",
                                "Produce textos argumentativos formales con rigor conceptual y citación adecuada.",
                                "Sustenta ponencias académicas demostrando dominio discursivo y pensamiento crítico."
                            },
                            [11] = new[] {
                                "Analiza obras de la literatura universal clásica y contemporánea con enfoque crítico.",
                                "Desarrolla estrategias avanzadas de lectura crítica orientadas a pruebas Saber 11.",
                                "Redacta ensayos argumentativos estructurados con coherencia, cohesión y estilo propio.",
                                "Evalúa críticamente discursos mediáticos, culturales y literarios contemporáneos."
                            }
                        },
                        ["Biología"] = new()
                        {
                            [6] = new[] {
                                "Identifica la estructura y función de los organelos en células procariotas y eucariotas.",
                                "Clasifica los seres vivos en reinos biológicos según sus características celulares.",
                                "Explica los niveles de organización ecológica y las relaciones tróficas en ecosistemas.",
                                "Comprende la importancia de conservar los recursos naturales y la biodiversidad local."
                            },
                            [7] = new[] {
                                "Explica los procesos de nutrición y digestión en plantas, animales y el ser humano.",
                                "Compara los sistemas circulatorio y respiratorio en diferentes grupos de organismos.",
                                "Analiza los mecanismos de excreción y osmorregulación para mantener el equilibrio interno.",
                                "Relaciona hábitos de alimentación y actividad física con el cuidado de la salud."
                            },
                            [8] = new[] {
                                "Comprende los procesos de mitosis y meiosis y su papel en la reproducción celular.",
                                "Explica la reproducción humana, el desarrollo embrionario y la salud sexual responsable.",
                                "Identifica la estructura y función del sistema nervioso y los órganos de los sentidos.",
                                "Analiza la regulación hormonal del sistema endocrino e inmunológico humano."
                            },
                            [9] = new[] {
                                "Resuelve problemas de herencia genética aplicando las leyes de Gregorio Mendel.",
                                "Explica la estructura del ADN, ARN y los procesos de transcripción y síntesis de proteínas.",
                                "Analiza las teorías evolutivas y las evidencias de la selección natural en las especies.",
                                "Comprende el origen de la biodiversidad y la taxonomía moderna de los seres vivos."
                            },
                            [10] = new[] {
                                "Analiza rutas metabólicas celulares como fotosíntesis y respiración aerobia/anaerobia.",
                                "Evalúa el flujo de energía y los ciclos biogeoquímicos en los ecosistemas terrestres.",
                                "Analiza la dinámica poblacional y el impacto de las actividades humanas en el ambiente.",
                                "Propone soluciones sostenibles frente al cambio climático y la pérdida de biodiversidad."
                            },
                            [11] = new[] {
                                "Analiza aplicaciones modernas de la biotecnología, ingeniería genética y bioética.",
                                "Comprende mecanismos avanzados de homeostasis celular y fisiología humana.",
                                "Evalúa problemáticas ambientales globales desde un enfoque científico y sistémico.",
                                "Interpreta gráficas, experimentos y situaciones problema de ciencias naturales tipo ICFES."
                            }
                        },
                        ["Ciencias Sociales"] = new()
                        {
                            [6] = new[] {
                                "Reconoce la estructura de la Tierra, coordenadas geográficas y formas del relieve.",
                                "Analiza el origen del ser humano y el surgimiento de las primeras civilizaciones.",
                                "Comprende el legado cultural y político de Grecia y Roma en el mundo occidental.",
                                "Identifica derechos, deberes y mecanismos de participación en el gobierno escolar."
                            },
                            [7] = new[] {
                                "Analiza la organización política, social y económica del feudalismo en la Edad Media.",
                                "Explica las transformaciones culturales del Renacimiento y la expansión europea.",
                                "Comprende el proceso de conquista y colonización de América y su impacto social.",
                                "Analiza la diversidad geográfica y cultural de las regiones naturales de Colombia."
                            },
                            [8] = new[] {
                                "Explica las causas y consecuencias de la Revolución Industrial y la Revolución Francesa.",
                                "Analiza los procesos de independencia en América Latina y la formación de repúblicas.",
                                "Comprende los conflictos políticos y territoriales de Colombia en el siglo XIX.",
                                "Evalúa el impacto ambiental y social de la urbanización e industrialización."
                            },
                            [9] = new[] {
                                "Analiza los principales hechos políticos y económicos de Colombia en el siglo XX.",
                                "Comprende las causas y consecuencias de las guerras mundiales y la Guerra Fría.",
                                "Evalúa la evolución de los derechos humanos y la Constitución Política de 1991.",
                                "Analiza problemáticas sociales contemporáneas proponiendo alternativas de convivencia."
                            },
                            [10] = new[] {
                                "Analiza los conflictos geopolíticos contemporáneos y el papel de organismos internacionales.",
                                "Comprende la estructura del Estado colombiano y los mecanismos de defensa de derechos.",
                                "Evalúa el impacto de la globalización económica y cultural en países en desarrollo.",
                                "Argumenta posturas críticas frente a fenómenos de migración, pobreza y desarrollo."
                            },
                            [11] = new[] {
                                "Analiza procesos de paz, memoria histórica y reconciliación en la sociedad colombiana.",
                                "Evalúa políticas económicas, deuda externa y modelos de desarrollo sostenible.",
                                "Interpreta fuentes históricas, mapas y contextos ciudadanos con pensamiento crítico.",
                                "Ejerce liderazgo democrático y participación ciudadana responsable en su entorno."
                            }
                        },
                        ["Inglés"] = new()
                        {
                            [6] = new[] {
                                "Utiliza saludos, presentaciones personales y vocabulario básico del aula en inglés.",
                                "Construye oraciones afirmativas y negativas en presente simple con el verbo To Be.",
                                "Describe rutinas diarias y horarios utilizando adverbios de frecuencia básicos.",
                                "Comprende textos cortos y diálogos sencillos sobre familia, hogar y pasatiempos."
                            },
                            [7] = new[] {
                                "Contrasta acciones habituales y acciones en progreso usando presente simple y continuo.",
                                "Expresa cantidades, precios y preferencias utilizando sustantivos contables e incontables.",
                                "Relata eventos pasados sencillos empleando verbos regulares e irregulares en pasado simple.",
                                "Formula planes futuros inmediatos usando Going To en conversaciones cotidianas."
                            },
                            [8] = new[] {
                                "Realiza comparaciones entre personas, lugares y objetos usando comparativos y superlativos.",
                                "Expresa obligaciones, consejos y habilidades empleando verbos modales (should, must, can).",
                                "Comprende textos narrativos breves identificando secuencia de eventos en pasado.",
                                "Redacta párrafos descriptivos coherentes sobre experiencias personales y viajes."
                            },
                            [9] = new[] {
                                "Utiliza el presente perfecto para hablar de experiencias de vida y acciones recientes.",
                                "Construye oraciones condicionales de primer tipo para expresar causas y consecuencias.",
                                "Comprende audios y lecturas de mediana extensión sobre cultura, ciencia y tecnología.",
                                "Participa en conversaciones estructuradas expresando opiniones y acuerdos."
                            },
                            [10] = new[] {
                                "Aplica tiempos narrativos (pasado simple, continuo y perfecto) en relatos estructurados.",
                                "Utiliza voz pasiva y segundo condicional en contextos académicos y formales.",
                                "Analiza artículos de opinión identificando postura del autor y vocabulario técnico.",
                                "Realiza presentaciones orales fluidas defendiendo puntos de vista con argumentos."
                            },
                            [11] = new[] {
                                "Emplea estructuras gramaticales avanzadas (reported speech y condicionales mixtos).",
                                "Comprende textos argumentativos complejos aplicando lectura inferencial en inglés.",
                                "Redacta ensayos cortos y cartas formales con vocabulario preciso y conectores avanzados.",
                                "Interactúa con fluidez en discusiones sobre temas globales, académicos y vocacionales."
                            }
                        },
                        ["Tecnología e Informática"] = new()
                        {
                            [6] = new[] {
                                "Identifica componentes de hardware, software y sistemas operativos en equipos de cómputo.",
                                "Elabora documentos estructurados en procesador de texto aplicando normas básicas de formato.",
                                "Crea presentaciones multimedia claras integrando texto, imágenes y diseño visual.",
                                "Practica hábitos de ciudadanía digital, huella digital y navegación segura en internet."
                            },
                            [7] = new[] {
                                "Organiza datos y aplica fórmulas básicas en hojas de cálculo para resolver problemas.",
                                "Representa información estadística mediante gráficos dinámicos en hojas de cálculo.",
                                "Reconoce conceptos básicos de redes, internet y seguridad de la información.",
                                "Diseña proyectos digitales colaborativos respetando derechos de autor."
                            },
                            [8] = new[] {
                                "Comprende el concepto de algoritmo y representa soluciones mediante diagramas de flujo.",
                                "Desarrolla programas básicos utilizando variables, operadores y estructuras condicionales.",
                                "Aplica funciones lógicas y de búsqueda en hojas de cálculo de nivel intermedio.",
                                "Analiza el impacto tecnológico y ambiental de los sistemas informáticos modernos."
                            },
                            [9] = new[] {
                                "Implementa estructuras repetitivas (ciclos) y funciones en un lenguaje de programación.",
                                "Diseña páginas web estáticas estructuradas mediante etiquetas semánticas HTML5.",
                                "Aplica estilos visuales y diseño responsivo básico utilizando hojas de estilo CSS3.",
                                "Documenta soluciones algorítmicas verificando entradas, procesos y salidas."
                            },
                            [10] = new[] {
                                "Modela bases de datos relacionales identificando entidades, atributos y llaves.",
                                "Construye consultas SQL básicas para insertar, consultar y filtrar información.",
                                "Desarrolla interactividad en páginas web utilizando fundamentos de JavaScript.",
                                "Aplica metodologías de trabajo colaborativo y control de versiones en proyectos."
                            },
                            [11] = new[] {
                                "Integra interfaces web con lógica de servidor y bases de datos en un proyecto funcional.",
                                "Ejecuta pruebas funcionales para detectar y corregir errores en aplicaciones de software.",
                                "Analiza tendencias tecnológicas como inteligencia artificial, nube y ciberseguridad.",
                                "Sustenta un proyecto tecnológico aplicado demostrando rigor técnico y documental."
                            }
                        },
                        ["Educación Física"] = new()
                        {
                            [6] = new[] { "Desarrolla coordinación visomanual y equilibrio en juegos predeportivos.", "Aplica fundamentos básicos de atletismo y gimnasia formativa.", "Participa en deportes de conjunto respetando reglas y compañeros.", "Reconoce la importancia del calentamiento y la hidratación física." },
                            [7] = new[] { "Mejora capacidades físicas condicionales de resistencia y velocidad.", "Ejecuta gestos técnicos básicos en baloncesto y voleibol.", "Demuestra juego limpio y trabajo en equipo durante torneos internos.", "Adopta posturas corporales adecuadas en la actividad física diaria." },
                            [8] = new[] { "Aplica fundamentos técnicos y tácticos básicos en fútbol y baloncesto.", "Desarrolla flexibilidad y fuerza general mediante circuitos motrices.", "Lidera actividades grupales fomentando inclusión y respeto.", "Evalúa su frecuencia cardíaca antes, durante y después del ejercicio." },
                            [9] = new[] { "Ejecuta sistemas tácticos ofensivos y defensivos en deportes de conjunto.", "Planifica sesiones básicas de acondicionamiento físico saludable.", "Demuestra autocontrol emocional en situaciones competitivas.", "Analiza los beneficios del deporte en la prevención del sedentarismo." },
                            [10] = new[] { "Diseña rutinas personales de entrenamiento físico según sus capacidades.", "Aplica reglamentos oficiales y arbitraje básico en disciplinas deportivas.", "Promueve hábitos de vida saludable y nutrición equilibrada.", "Ejerce liderazgo positivo en la organización de eventos recreativos." },
                            [11] = new[] { "Evalúa y ajusta su plan personal de actividad física y bienestar integral.", "Demuestra dominio técnico-táctico y cooperación en deportes colectivos.", "Organiza y gestiona actividades deportivas y recreativas institucionales.", "Integra la actividad física como hábito permanente de autocuidado." }
                        },
                        ["Ética y Valores"] = new()
                        {
                            [6] = new[] { "Reconoce su identidad, autoestima y dignidad como persona.", "Practica el respeto y la escucha activa en la convivencia escolar.", "Identifica normas básicas de convivencia y solución pacífica de conflictos.", "Valora la importancia de la familia y la solidaridad en su entorno." },
                            [7] = new[] { "Comprende la empatía y el respeto por la diferencia cultural y social.", "Asume responsabilidad frente a sus deberes académicos y personales.", "Aplica el diálogo como herramienta para resolver desacuerdos.", "Reflexiona sobre el uso ético y respetuoso de las redes sociales." },
                            [8] = new[] { "Analiza dilemas morales cotidianos tomando decisiones con criterio.", "Promueve la equidad, inclusión y no discriminación en el aula.", "Reconoce la importancia de la honestidad y la integridad personal.", "Participa en iniciativas solidarias en beneficio de su comunidad." },
                            [9] = new[] { "Reflexiona sobre la autonomía moral y la responsabilidad social.", "Analiza la relación entre libertad, deberes y derechos fundamentales.", "Fortalece habilidades socioemocionales para la toma de decisiones.", "Construye metas personales a corto y mediano plazo." },
                            [10] = new[] { "Evalúa problemáticas éticas contemporáneas desde los derechos humanos.", "Asume posturas críticas frente a la corrupción y la justicia social.", "Desarrolla liderazgo ético y compromiso ciudadano en su institución.", "Proyecta su vocación profesional y personal con sentido de propósito." },
                            [11] = new[] { "Argumenta decisiones éticas complejas con responsabilidad y autonomía.", "Ejerce ciudadanía activa basada en la legalidad, respeto y solidaridad.", "Consolida su proyecto de vida profesional, académico y personal.", "Demuestra compromiso ético en su transición hacia la educación superior." }
                        }
                    };

                    // Textos genéricos progresivos por período para materias adicionales
                    string[] plantillasPeriodo = new[]
                    {
                        "Comprende los conceptos fundamentales de {0} correspondientes al grado {1}° y los aplica en ejercicios guiados.",
                        "Analiza situaciones problema y desarrolla habilidades prácticas propias de {0} en grado {1}°.",
                        "Argumenta y relaciona los contenidos teóricos de {0} con contextos reales y experimentos de grado {1}°.",
                        "Integra los aprendizajes de {0} demostrando dominio conceptual, autonomía y rigor académico en grado {1}°."
                    };

                    // 1. Insertar el catálogo principal (6° a 11°, períodos 1 a 4)
                    foreach (var matEntry in catalogo)
                    {
                        string nombreMat = matEntry.Key;
                        string prefijo = new string(nombreMat.Where(char.IsLetter).Take(3).ToArray()).ToUpper();

                        foreach (var gradoEntry in matEntry.Value)
                        {
                            int grado = gradoEntry.Key;
                            string[] descs = gradoEntry.Value;

                            for (int p = 1; p <= 4; p++)
                            {
                                logrosIniciales.Add(new Logro
                                {
                                    grado = grado,
                                    materia = nombreMat,
                                    periodo = p,
                                    codigo_logro = $"LOG-{grado}-{prefijo}-P{p}",
                                    descripcion = descs[p - 1],
                                    activo = true
                                });
                            }
                        }
                    }

                    // 2. Garantizar que cualquier otra materia registrada en la tabla MATERIA también tenga logros (6° a 11°)
                    var materiasEnBD = MATERIA.Select(m => m.nombre_materia).Distinct().ToList();
                    foreach (var matBD in materiasEnBD)
                    {
                        if (string.IsNullOrWhiteSpace(matBD)) continue;
                        bool yaExiste = logrosIniciales.Any(l => l.materia.Equals(matBD.Trim(), StringComparison.OrdinalIgnoreCase));
                        if (!yaExiste)
                        {
                            string prefijo = new string(matBD.Where(char.IsLetter).Take(3).ToArray()).ToUpper();
                            for (int g = 6; g <= 11; g++)
                            {
                                for (int p = 1; p <= 4; p++)
                                {
                                    logrosIniciales.Add(new Logro
                                    {
                                        grado = g,
                                        materia = matBD.Trim(),
                                        periodo = p,
                                        codigo_logro = $"LOG-{g}-{prefijo}-P{p}",
                                        descripcion = string.Format(plantillasPeriodo[p - 1], matBD.Trim(), g),
                                        activo = true
                                    });
                                }
                            }
                        }
                    }

                    Logros.AddRange(logrosIniciales);
                    SaveChanges();
                }
            }
            catch { }
        }

        public void AsegurarEsquemaPeriodosSubidaNotas()
        {
            try
            {
                var sql = @"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PERIODOS_SUBIDA_NOTAS')
                BEGIN
                    CREATE TABLE [dbo].[PERIODOS_SUBIDA_NOTAS] (
                        [id] INT IDENTITY(1,1) PRIMARY KEY,
                        [periodo] INT NOT NULL,
                        [nombre] NVARCHAR(100) NOT NULL,
                        [fecha_inicio] DATETIME NOT NULL DEFAULT GETDATE(),
                        [fecha_cierre] DATETIME NOT NULL DEFAULT DATEADD(day, 30, GETDATE()),
                        [activo] BIT NOT NULL DEFAULT 1,
                        [porcentaje] INT NOT NULL DEFAULT 25,
                        [observaciones] NVARCHAR(500) NULL
                    );

                    INSERT INTO [dbo].[PERIODOS_SUBIDA_NOTAS] ([periodo], [nombre], [fecha_inicio], [fecha_cierre], [activo], [porcentaje], [observaciones])
                    VALUES 
                    (1, 'Primer Periodo Académico', GETDATE(), DATEADD(day, 30, GETDATE()), 1, 25, 'Subida ordinaria de calificaciones P1'),
                    (2, 'Segundo Periodo Académico', DATEADD(day, 31, GETDATE()), DATEADD(day, 60, GETDATE()), 0, 25, 'Pendiente de apertura'),
                    (3, 'Tercer Periodo Académico', DATEADD(day, 61, GETDATE()), DATEADD(day, 90, GETDATE()), 0, 25, 'Pendiente de apertura'),
                    (4, 'Cuarto Periodo Académico', DATEADD(day, 91, GETDATE()), DATEADD(day, 120, GETDATE()), 0, 25, 'Pendiente de apertura');
                END";
                Database.ExecuteSqlRaw(sql);
            }
            catch { }

            AsegurarLimpiezaCalificacionesDuplicadas();
        }

        public void AsegurarLimpiezaCalificacionesDuplicadas()
        {
            try
            {
                var sql = @"
                IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Calificaciones')
                BEGIN
                    WITH CTE AS (
                        SELECT c.ID_Calificacion,
                               ROW_NUMBER() OVER(
                                   PARTITION BY c.ID_Estudiante, c.Materia, c.Periodo 
                                   ORDER BY 
                                       CASE WHEN EXISTS(SELECT 1 FROM sys.tables WHERE name = 'CERTIFICADO_DETALLE') 
                                            AND EXISTS(SELECT 1 FROM CERTIFICADO_DETALLE cd WHERE cd.id_calificacion = c.ID_Calificacion) 
                                            THEN 1 ELSE 0 END DESC,
                                       c.ID_Calificacion DESC
                               ) as rn
                        FROM Calificaciones c
                    )
                    DELETE FROM CTE WHERE rn > 1;
                END";
                Database.ExecuteSqlRaw(sql);
            }
            catch
            {
                try
                {
                    var duplicados = Calificaciones
                        .AsEnumerable()
                        .GroupBy(c => new { c.ID_Estudiante, MateriaNorm = (c.Materia ?? "").Trim().ToLower(), c.Periodo })
                        .Where(g => g.Count() > 1)
                        .SelectMany(g => g.OrderByDescending(x => x.ID_Calificacion).Skip(1))
                        .ToList();

                    if (duplicados.Any())
                    {
                        Calificaciones.RemoveRange(duplicados);
                        SaveChanges();
                    }
                }
                catch { }
            }
        }

        private static bool _esquemaSolicitudesCertificadoListo;

        /// <summary>
        /// Crea la tabla SOLICITUD_CERTIFICADO si todavía no existe (mismo script que Scripts/SolicitudCertificado.sql).
        /// Solo se verifica una vez por ejecución de la aplicación.
        /// </summary>
        public void AsegurarEsquemaSolicitudesCertificado()
        {
            if (_esquemaSolicitudesCertificadoListo)
                return;

            try
            {
                Database.ExecuteSqlRaw(@"
                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SOLICITUD_CERTIFICADO')
                    BEGIN
                        CREATE TABLE [dbo].[SOLICITUD_CERTIFICADO](
                            [id_solicitud]              [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
                            [codigo_radicado]           [nvarchar](50)  NOT NULL,
                            [id_estudiante]             [int]           NOT NULL,
                            [nombre_estudiante]         [nvarchar](150) NOT NULL,
                            [tipo_certificado]          [nvarchar](50)  NOT NULL,
                            [observaciones_solicitante] [nvarchar](500) NULL,
                            [id_solicitante]            [int]           NOT NULL,
                            [nombre_solicitante]        [nvarchar](150) NOT NULL,
                            [rol_solicitante]           [nvarchar](50)  NOT NULL,
                            [fecha_solicitud]           [datetime]      NOT NULL,
                            [fecha_limite]              [datetime]      NOT NULL,
                            [estado]                    [nvarchar](30)  NOT NULL DEFAULT 'Pendiente',
                            [fecha_entrega]             [datetime]      NULL,
                            [id_usuario_entrega]        [int]           NULL,
                            [id_certificado]            [int]           NULL,
                            [motivo_rechazo]            [nvarchar](500) NULL,
                            [observaciones_cierre]      [nvarchar](500) NULL,
                            [activo]                    [bit]           NOT NULL DEFAULT 1
                        );

                        CREATE INDEX IX_SOLICITUD_CERT_estado_limite ON [dbo].[SOLICITUD_CERTIFICADO]([estado], [fecha_limite]);
                        CREATE INDEX IX_SOLICITUD_CERT_estudiante    ON [dbo].[SOLICITUD_CERTIFICADO]([id_estudiante]);
                    END
                ");
                _esquemaSolicitudesCertificadoListo = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Error AsegurarEsquemaSolicitudesCertificado]: {ex.Message}");
            }
        }

        private static bool _esquemaCambioCorreoListo;

        /// <summary>
        /// Crea la tabla GU_CAMBIO_CORREO si todavía no existe (mismo script que Scripts/CambioCorreo.sql).
        /// Solo se verifica una vez por ejecución de la aplicación.
        /// </summary>
        public void AsegurarEsquemaCambioCorreo()
        {
            if (_esquemaCambioCorreoListo)
                return;

            try
            {
                Database.ExecuteSqlRaw(@"
                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'GU_CAMBIO_CORREO')
                    BEGIN
                        CREATE TABLE [dbo].[GU_CAMBIO_CORREO](
                            [id_cambio]          [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
                            [id_usuario]         [int]           NOT NULL,
                            [correo_anterior]    [nvarchar](150) NOT NULL,
                            [correo_nuevo]       [nvarchar](150) NOT NULL,
                            [token_hash]         [nvarchar](64)  NOT NULL,
                            [fecha_solicitud]    [datetime]      NOT NULL,
                            [fecha_expiracion]   [datetime]      NOT NULL,
                            [usado]              [bit]           NOT NULL DEFAULT 0,
                            [fecha_confirmacion] [datetime]      NULL
                        );

                        CREATE UNIQUE INDEX UX_CAMBIO_CORREO_token   ON [dbo].[GU_CAMBIO_CORREO]([token_hash]);
                        CREATE INDEX        IX_CAMBIO_CORREO_usuario ON [dbo].[GU_CAMBIO_CORREO]([id_usuario], [usado]);
                    END
                ");
                _esquemaCambioCorreoListo = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Error AsegurarEsquemaCambioCorreo]: {ex.Message}");
            }
        }
    }
}
