using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProyectoPAE.Models
{
    [Table("DOCUMENTO")]
    public class Documento
    {
        [Key]
        [Column("id_documento")]
        public int IdDocumento { get; set; }

        [Required]
        [StringLength(200)]
        [Column("titulo")]
        public string Titulo { get; set; } = string.Empty;

        [Column("descripcion")]
        public string? Descripcion { get; set; }

        [Required]
        [StringLength(80)]
        [Column("tipo_documento")]
        public string TipoDocumento { get; set; } = "General"; // Actividad, Notas, Certificado, PQRS, Extracurricular, etc.

        [Required]
        [StringLength(50)]
        [Column("estado_documento")]
        public string EstadoDocumento { get; set; } = "Activo"; // Activo, Inactivo, Archivado

        [Column("version")]
        public int Version { get; set; } = 1;

        [Column("fecha_subida")]
        public DateTime FechaSubida { get; set; } = DateTime.Today;

        [Column("fecha_actualizacion")]
        public DateTime? FechaActualizacion { get; set; }

        [Column("id_usuario_autor")]
        public int IdUsuarioAutor { get; set; }

        [Required]
        [StringLength(255)]
        [Column("nombre_archivo")]
        public string NombreArchivo { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        [Column("archivo_url")]
        public string ArchivoUrl { get; set; } = string.Empty;

        [Column("es_publico")]
        public bool EsPublico { get; set; } = false;

        [Column("id_curso")]
        public int? IdCurso { get; set; }

        [Column("id_materia")]
        public int? IdMateria { get; set; }
    }

    [Table("DOCUMENTO_VERSION")]
    public class DocumentoVersion
    {
        [Key]
        [Column("id_version")]
        public int IdVersion { get; set; }

        [Column("id_documento")]
        public int IdDocumento { get; set; }

        [Column("numero_version")]
        public int NumeroVersion { get; set; } = 1;

        [Required]
        [StringLength(500)]
        [Column("archivo_url")]
        public string ArchivoUrl { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        [Column("nombre_archivo")]
        public string NombreArchivo { get; set; } = string.Empty;

        [Column("fecha_version")]
        public DateTime FechaVersion { get; set; } = DateTime.Today;

        [Column("id_usuario")]
        public int IdUsuario { get; set; }

        [Column("notas_version")]
        public string? NotasVersion { get; set; }
    }

    [Table("DOCUMENTO_ETIQUETA")]
    public class DocumentoEtiqueta
    {
        [Key]
        [Column("id_etiqueta")]
        public int IdEtiqueta { get; set; }

        [Column("id_documento")]
        public int IdDocumento { get; set; }

        /// <summary>
        /// Nombre de columna física 'atiqueta' en SQL Server.
        /// Almacena etiquetas como 'Actividad', 'Notas', 'Certificado',
        /// o la etiqueta crítica 'Estado Crítico - Dato Menor de Edad (Dec. 1074/2015)'
        /// </summary>
        [Required]
        [StringLength(80)]
        [Column("atiqueta")]
        public string Etiqueta { get; set; } = string.Empty;
    }
}
