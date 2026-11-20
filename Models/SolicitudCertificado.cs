using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ProyectoPAE.Servicios;

namespace ProyectoPAE.Models
{
    /// <summary>
    /// Solicitud de un certificado con plazo de entrega. El certificado en sí (CERTIFICADO) se genera
    /// al entregar la solicitud y queda enlazado en IdCertificado.
    /// </summary>
    [Table("SOLICITUD_CERTIFICADO")]
    public class SolicitudCertificado
    {
        [Key]
        [Column("id_solicitud")]
        public int IdSolicitud { get; set; }

        [Required]
        [StringLength(50)]
        [Column("codigo_radicado")]
        public string CodigoRadicado { get; set; } = string.Empty;

        /// <summary>ID_Usuario del estudiante al que pertenece el certificado.</summary>
        [Column("id_estudiante")]
        public int IdEstudiante { get; set; }

        [Required]
        [StringLength(150)]
        [Column("nombre_estudiante")]
        public string NombreEstudiante { get; set; } = string.Empty;

        /// <summary>"Matrícula" o "Notas".</summary>
        [Required]
        [StringLength(50)]
        [Column("tipo_certificado")]
        public string TipoCertificado { get; set; } = EstadoSolicitudCertificado.TipoMatricula;

        [StringLength(500)]
        [Column("observaciones_solicitante")]
        public string? ObservacionesSolicitante { get; set; }

        [Column("id_solicitante")]
        public int IdSolicitante { get; set; }

        [Required]
        [StringLength(150)]
        [Column("nombre_solicitante")]
        public string NombreSolicitante { get; set; } = string.Empty;

        /// <summary>Rol de quien registró la solicitud (acudiente, coordinador o admin).</summary>
        [Required]
        [StringLength(50)]
        [Column("rol_solicitante")]
        public string RolSolicitante { get; set; } = string.Empty;

        [Column("fecha_solicitud")]
        public DateTime FechaSolicitud { get; set; } = DateTime.Now;

        [Column("fecha_limite")]
        public DateTime FechaLimite { get; set; }

        /// <summary>Pendiente, En proceso, Entregada o Cancelada. El vencimiento no se guarda: se calcula.</summary>
        [Required]
        [StringLength(30)]
        [Column("estado")]
        public string Estado { get; set; } = EstadoSolicitudCertificado.Pendiente;

        [Column("fecha_entrega")]
        public DateTime? FechaEntrega { get; set; }

        [Column("id_certificado")]
        public int? IdCertificado { get; set; }

        [Column("id_usuario_gestor")]
        public int? IdUsuarioGestor { get; set; }

        [StringLength(150)]
        [Column("nombre_gestor")]
        public string? NombreGestor { get; set; }

        /// <summary>Bitácora de la gestión: ajustes de plazo, cancelaciones y notas de entrega.</summary>
        [Column("observaciones_gestion")]
        public string? ObservacionesGestion { get; set; }

        [Column("activo")]
        public bool Activo { get; set; } = true;
    }

    /// <summary>Valores permitidos para el estado y el tipo de una solicitud.</summary>
    public static class EstadoSolicitudCertificado
    {
        public const string Pendiente = "Pendiente";
        public const string EnProceso = "En proceso";
        public const string Entregada = "Entregada";
        public const string Cancelada = "Cancelada";

        public const string TipoMatricula = "Matrícula";
        public const string TipoNotas = "Notas";

        public static string NombreTipo(string? tipo)
        {
            return tipo == TipoNotas ? "Certificado de notas" : "Constancia de matrícula";
        }
    }

    /// <summary>Una solicitud junto con la evaluación de su plazo (calculada al momento de mostrarla).</summary>
    public class SolicitudCertificadoItem
    {
        public SolicitudCertificado Solicitud { get; set; } = new SolicitudCertificado();
        public EstadoPlazo Plazo { get; set; } = new EstadoPlazo();
    }

    public class OpcionEstudiante
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
    }

    /// <summary>Datos del formulario para registrar una solicitud.</summary>
    public class SolicitudCertificadoCrearViewModel
    {
        /// <summary>Solo para coordinador o admin: documento del estudiante.</summary>
        public string? NumeroDocumento { get; set; }

        /// <summary>Solo para acudiente: estudiante (hijo) elegido.</summary>
        public int? IdEstudiante { get; set; }

        public string TipoCertificado { get; set; } = EstadoSolicitudCertificado.TipoMatricula;

        [StringLength(500, ErrorMessage = "Las observaciones no pueden superar 500 caracteres.")]
        public string? Observaciones { get; set; }

        /// <summary>Solo coordinador o admin: permite registrar la fecha real en que se pidió el certificado.</summary>
        public DateTime? FechaSolicitud { get; set; }

        /// <summary>Solo coordinador o admin: si se deja vacía se calcula con el plazo estándar.</summary>
        public DateTime? FechaLimite { get; set; }

        // Datos de apoyo para pintar el formulario (no se envían desde la pantalla)
        public bool EsGestor { get; set; }
        public List<OpcionEstudiante> Hijos { get; set; } = new List<OpcionEstudiante>();
    }

    public class SolicitudCertificadoDetalleViewModel
    {
        public SolicitudCertificado Solicitud { get; set; } = new SolicitudCertificado();
        public EstadoPlazo Plazo { get; set; } = new EstadoPlazo();
        public Certificado? Certificado { get; set; }
        public bool EsGestor { get; set; }
    }
}