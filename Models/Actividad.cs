using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace ProyectoPAE.Models
{
    [Table("ACTIVIDAD")]
    public class Actividad
    {
        [Key]
        [Column("id_actividad")]
        public int IdActividad { get; set; }

        [Required]
        [Column("titulo")]
        [StringLength(150)]
        public string Titulo { get; set; } = string.Empty;

        [Column("descripcion")]
        public string? Descripcion { get; set; }

        [Required]
        [Column("materia")]
        [StringLength(100)]
        public string Materia { get; set; } = string.Empty;

        [Column("grado")]
        [StringLength(50)]
        public string? Grado { get; set; }

        [Column("fecha_limite")]
        public DateTime FechaLimite { get; set; }

        [Column("id_docente")]
        public int? IdDocente { get; set; }

        [Column("archivo_adjunto")]
        [StringLength(255)]
        public string? ArchivoAdjunto { get; set; }

        [Column("fecha_creacion")]
        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        [Column("es_mejora")]
        public bool EsMejora { get; set; } = false;

        [Column("activo")]
        public bool Activo { get; set; } = true;
    }

    [Table("COMENTARIO_ACTIVIDAD")]
    public class ComentarioActividad
    {
        [Key]
        [Column("id_comentario")]
        public int IdComentario { get; set; }

        [Column("id_actividad")]
        public int IdActividad { get; set; }

        [Column("id_usuario")]
        public int IdUsuario { get; set; }

        [Required]
        [Column("nombre_usuario")]
        [StringLength(150)]
        public string NombreUsuario { get; set; } = string.Empty;

        [Required]
        [Column("rol_usuario")]
        [StringLength(50)]
        public string RolUsuario { get; set; } = string.Empty;

        [Required]
        [Column("mensaje")]
        public string Mensaje { get; set; } = string.Empty;

        [Column("fecha")]
        public DateTime Fecha { get; set; } = DateTime.Now;

        [Column("es_privado")]
        public bool EsPrivado { get; set; } = false;

        [Column("id_estudiante_privado")]
        public int? IdEstudiantePrivado { get; set; }
    }

    [Table("ENTREGA_ACTIVIDAD")]
    public class EntregaActividad
    {
        [Key]
        [Column("id_entrega")]
        public int IdEntrega { get; set; }

        [Column("id_actividad")]
        public int IdActividad { get; set; }

        [Column("id_estudiante")]
        public int IdEstudiante { get; set; }

        [Column("archivo_ruta")]
        [StringLength(255)]
        public string? ArchivoRuta { get; set; }

        [Column("archivo_nombre")]
        [StringLength(255)]
        public string? ArchivoNombre { get; set; }

        [Column("comentario")]
        public string? Comentario { get; set; }

        [Column("fecha_entrega")]
        public DateTime FechaEntrega { get; set; } = DateTime.Now;

        [Column("calificacion", TypeName = "decimal(3, 1)")]
        public decimal? Calificacion { get; set; }

        [Column("retroalimentacion")]
        public string? Retroalimentacion { get; set; }

        [Column("estado")]
        [StringLength(50)]
        public string Estado { get; set; } = "Entregado";
    }

    public class ActividadItemViewModel
    {
        public int IdActividad { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public string Materia { get; set; } = string.Empty;
        public string? Grado { get; set; }
        public DateTime FechaLimite { get; set; }
        public string? ArchivoAdjunto { get; set; }
        public bool EstaVencida => DateTime.Now > FechaLimite;
        public bool Entregada { get; set; }
        public int? IdEntrega { get; set; }
        public DateTime? FechaEntrega { get; set; }
        public string? ArchivoNombreEntrega { get; set; }
        public string? ArchivoRutaEntrega { get; set; }
        public string? ComentarioEntrega { get; set; }
        public decimal? Calificacion { get; set; }
        public string? Retroalimentacion { get; set; }
        public string EstadoEntrega { get; set; } = "Pendiente";
        public bool EsMejora { get; set; }
    }

    public class NotasPorMateriaViewModel
    {
        public string Materia { get; set; } = string.Empty;
        public List<CalificacionItemDetalle> Calificaciones { get; set; } = new List<CalificacionItemDetalle>();
        public double Promedio { get; set; }
        public bool Aprobado => Promedio >= 3.0;
        public string Desempeno => Promedio >= 4.6 ? "Superior" : (Promedio >= 4.0 ? "Alto" : (Promedio >= 3.0 ? "Básico" : "Bajo"));
    }

    public class CalificacionItemDetalle
    {
        public int IdCalificacion { get; set; }
        public int Periodo { get; set; }
        public decimal Nota { get; set; }
        public DateTime FechaRegistro { get; set; }
        public string? Observacion { get; set; }
    }
}
