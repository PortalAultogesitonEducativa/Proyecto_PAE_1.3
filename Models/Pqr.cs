using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProyectoPAE.Models
{
    [Table("PQR")]
    public class Pqr
    {
        [Key]
        [Column("id_pqr")]
        public int IdPqr { get; set; }

        [Required]
        [StringLength(50)]
        [Column("codigo_radicado")]
        public string CodigoRadicado { get; set; } = string.Empty;

        [Column("id_usuario")]
        public int IdUsuario { get; set; }

        [Required]
        [StringLength(150)]
        [Column("nombre_usuario")]
        public string NombreUsuario { get; set; } = string.Empty;

        [StringLength(150)]
        [Column("correo_usuario")]
        public string? CorreoUsuario { get; set; }

        [Required]
        [StringLength(50)]
        [Column("rol_usuario")]
        public string RolUsuario { get; set; } = string.Empty;

        [Required(ErrorMessage = "Seleccione el tipo de solicitud.")]
        [StringLength(50)]
        [Column("tipo")]
        public string Tipo { get; set; } = "Petición"; // Petición, Queja, Reclamo

        [Required(ErrorMessage = "El asunto es obligatorio.")]
        [StringLength(200, MinimumLength = 5, ErrorMessage = "El asunto debe tener entre 5 y 200 caracteres.")]
        [Column("asunto")]
        public string Asunto { get; set; } = string.Empty;

        [Required(ErrorMessage = "La descripción de la solicitud es obligatoria.")]
        [StringLength(4000, MinimumLength = 10, ErrorMessage = "La descripción debe tener al menos 10 caracteres.")]
        [Column("descripcion")]
        public string Descripcion { get; set; } = string.Empty;

        [Column("fecha_registro")]
        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        [Required]
        [StringLength(50)]
        [Column("estado")]
        public string Estado { get; set; } = "Recibida"; // Recibida, En revisión, Respondida, Cerrada

        [Column("respuesta")]
        public string? Respuesta { get; set; }

        [Column("fecha_respuesta")]
        public DateTime? FechaRespuesta { get; set; }

        [StringLength(150)]
        [Column("respondido_por")]
        public string? RespondidoPor { get; set; }

        [Column("activo")]
        public bool Activo { get; set; } = true;
    }

    public class PqrCrearViewModel
    {
        [Required(ErrorMessage = "Seleccione el tipo de solicitud.")]
        [Display(Name = "Tipo de solicitud")]
        public string Tipo { get; set; } = "Petición";

        [Required(ErrorMessage = "El asunto es obligatorio.")]
        [StringLength(200, MinimumLength = 5, ErrorMessage = "El asunto debe tener entre 5 y 200 caracteres.")]
        [Display(Name = "Asunto")]
        public string Asunto { get; set; } = string.Empty;

        [Required(ErrorMessage = "La descripción de la solicitud es obligatoria.")]
        [StringLength(4000, MinimumLength = 10, ErrorMessage = "La descripción debe tener entre 10 y 4000 caracteres.")]
        [Display(Name = "Descripción detallada")]
        public string Descripcion { get; set; } = string.Empty;
    }

    public class PqrResponderViewModel
    {
        public int IdPqr { get; set; }

        [Required(ErrorMessage = "La respuesta a la solicitud es obligatoria.")]
        [StringLength(4000, MinimumLength = 5, ErrorMessage = "La respuesta debe tener al menos 5 caracteres.")]
        [Display(Name = "Respuesta o Dictamen")]
        public string Respuesta { get; set; } = string.Empty;

        [Required(ErrorMessage = "Seleccione el nuevo estado.")]
        [Display(Name = "Estado")]
        public string Estado { get; set; } = "Respondida";
    }

    public class PqrCambiarEstadoViewModel
    {
        public int IdPqr { get; set; }

        [Required(ErrorMessage = "Seleccione un estado válido.")]
        public string Estado { get; set; } = "En revisión";
    }

    public class PqrFiltroViewModel
    {
        public string? Busqueda { get; set; }
        public string? Tipo { get; set; } = "todos";
        public string? Estado { get; set; } = "todos";
    }
}
