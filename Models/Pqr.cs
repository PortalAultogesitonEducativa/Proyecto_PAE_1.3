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
        public string Tipo { get; set; } = "Petición"; // Petición, Queja, Reclamo, Sugerencia, Consulta

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

        #region Propiedades Calculadas de SLA y Términos Legales (Ley 1755 de 2015 / CPACA)

        /// <summary>
        /// Plazo máximo en días hábiles según la tipología legal de la PQR:
        /// - Petición de información/documentos: 10 días hábiles
        /// - Queja: 15 días hábiles
        /// - Reclamo: 15 días hábiles
        /// - Sugerencia: 10 días hábiles
        /// - Consulta: 30 días hábiles
        /// </summary>
        [NotMapped]
        public int DiasHabilesPlazo
        {
            get
            {
                var t = Tipo?.Trim().ToLowerInvariant() ?? "";
                if (t.Contains("sugerencia")) return 10;
                if (t.Contains("consulta")) return 30;
                if (t.Contains("queja") || t.Contains("reclamo")) return 15;
                if (t.Contains("petición") || t.Contains("peticion")) return 10; // Plazo legal general de documentos/información
                return 15;
            }
        }

        /// <summary>
        /// Calcula la fecha límite de vencimiento sumando únicamente días hábiles (lunes a viernes).
        /// </summary>
        [NotMapped]
        public DateTime FechaLimiteRespuesta
        {
            get
            {
                DateTime fecha = FechaRegistro.Date;
                int dias = DiasHabilesPlazo;
                while (dias > 0)
                {
                    fecha = fecha.AddDays(1);
                    if (fecha.DayOfWeek != DayOfWeek.Saturday && fecha.DayOfWeek != DayOfWeek.Sunday)
                    {
                        dias--;
                    }
                }
                return fecha;
            }
        }

        /// <summary>
        /// Días hábiles restantes para dar respuesta oportuna.
        /// Retorna valor negativo si la solicitud ya está vencida.
        /// </summary>
        [NotMapped]
        public int DiasHabilesRestantes
        {
            get
            {
                if (Estado == "Respondida" || Estado == "Cerrada") return 0;
                DateTime hoy = DateTime.Today;
                DateTime limite = FechaLimiteRespuesta.Date;

                if (hoy > limite)
                {
                    int vencidos = 0;
                    DateTime temp = limite;
                    while (temp < hoy)
                    {
                        temp = temp.AddDays(1);
                        if (temp.DayOfWeek != DayOfWeek.Saturday && temp.DayOfWeek != DayOfWeek.Sunday)
                        {
                            vencidos++;
                        }
                    }
                    return -vencidos;
                }
                else
                {
                    int restantes = 0;
                    DateTime temp = hoy;
                    while (temp < limite)
                    {
                        temp = temp.AddDays(1);
                        if (temp.DayOfWeek != DayOfWeek.Saturday && temp.DayOfWeek != DayOfWeek.Sunday)
                        {
                            restantes++;
                        }
                    }
                    return restantes;
                }
            }
        }

        /// <summary>
        /// Clasificación del estado de atención según el SLA:
        /// - "Respondida"
        /// - "Vencida" (días hábiles vencidos)
        /// - "Por vencer" (2 días hábiles o menos)
        /// - "A tiempo"
        /// </summary>
        [NotMapped]
        public string EstadoSla
        {
            get
            {
                if (Estado == "Respondida" || Estado == "Cerrada") return "Respondida";
                int rest = DiasHabilesRestantes;
                if (rest < 0) return "Vencida";
                if (rest <= 2) return "Por vencer";
                return "A tiempo";
            }
        }

        /// <summary>
        /// Clase CSS de insignia visual para el SLA
        /// </summary>
        [NotMapped]
        public string BadgeClaseSla
        {
            get
            {
                return EstadoSla switch
                {
                    "Respondida" => "bg-success-subtle text-success border border-success-subtle",
                    "A tiempo" => "bg-primary-subtle text-primary border border-primary-subtle",
                    "Por vencer" => "bg-warning text-dark fw-bold",
                    "Vencida" => "bg-danger text-white fw-bold",
                    _ => "bg-light text-dark"
                };
            }
        }

        #endregion
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
