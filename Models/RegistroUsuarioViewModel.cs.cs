using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ProyectoPAE.Models
{
    public class RegistroUsuarioViewModel
    {
        // Datos personales
        [Required]
        public string NOMBRES { get; set; } = "";

        [Required]
        public string APELLIDOS { get; set; } = "";

        [Required]
        public string TIPO_DOCUMENTO { get; set; } = ""; // RC, TI, CC, CE, PA

        [Required]
        public string NUM_DOCUMENTO { get; set; } = "";

        [Required]
        [DataType(DataType.Date)]
        public DateTime FECHA_NACIMIENTO { get; set; }

        public int EDAD { get; set; } // se recalcula en el servidor por seguridad

        [Required]
        public string GENERO { get; set; } = ""; // M, F, O

        public string? LUGAR_NACIMIENTO { get; set; }

        // Contacto
        [Required, EmailAddress]
        public string CORREO_ELECTRONICO { get; set; } = "";

        public string? TELEFONO_FIJO { get; set; }

        [Required]
        public string CELULAR { get; set; } = "";

        public string? DIRECCION { get; set; }
        public string? CIUDAD { get; set; }
        public string? BARRIO { get; set; }

        // Rol / académico
        [Required]
        public string ROL { get; set; } = ""; // Estudiante, Docente, Coordinador

        public int? CursoSeleccionado { get; set; } // requerido solo si ROL == Estudiante
        public string? AREA_ASIGNATURA { get; set; }  // requerido solo si ROL == Docente
        public string? COLEGIO_PROCEDENCIA { get; set; }

        // Salud
        public string? TIPO_SANGRE { get; set; }
        public string? EPS { get; set; }
        public string? ALERGIAS { get; set; }
        public string? CONDICIONES_MEDICAS { get; set; }
        public string? OBSERVACIONES { get; set; }

        // Acudientes (solo aplica si ROL == Estudiante)
        public List<AcudienteViewModel> Acudientes { get; set; } = new List<AcudienteViewModel>();

        // Seguridad
        [Required]
        [DataType(DataType.Password)]
        public string CONTRASEÑA { get; set; } = "";

        public bool FORZAR_CAMBIO_CLAVE { get; set; } = true;
        public bool ESTADO_ACTIVO { get; set; } = true;
    }

    public class AcudienteViewModel
    {
        public string? NOMBRES { get; set; }
        public string? PARENTESCO { get; set; }
        public string? DOCUMENTO { get; set; }
        public string? CELULAR { get; set; }
        public string? CORREO { get; set; }
        public bool ES_PRINCIPAL { get; set; }
    }
}