using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace PAE_APP_MOVIL_1._0.Models
{
    [Table("GU_Usuario")]
    public class Usuario
    {
        [Key]
        public int ID_Usuario { get; set; }

        // Ya existentes
        public string NOMBRE_USUARIO { get; set; } = string.Empty;
        public string CONTRASEÑA { get; set; } = string.Empty;
        public string? NOMBRES { get; set; }
        public string? APELLIDOS { get; set; }
        public string? CORREO_ELECTRONICO { get; set; }
        public DateTime? FECHA_CREACION { get; set; }
        public bool ACTIVO { get; set; }
        public string? ROL { get; set; }
        public string? TELEFONO { get; set; }
        public string? DIRECCION { get; set; }
        public string? FOTO_URL { get; set; }

        // Nuevos: identificación
        public string? TIPO_DOCUMENTO { get; set; }
        public string? NUM_DOCUMENTO { get; set; }
        public DateTime? FECHA_NACIMIENTO { get; set; }
        public string? GENERO { get; set; }
        public string? LUGAR_NACIMIENTO { get; set; }

        // Nuevos: contacto adicional
        public string? CIUDAD { get; set; }
        public string? BARRIO { get; set; }

        // Nuevos: académico
        public string? AREA_ASIGNATURA { get; set; }      // aplica a Docente
        public string? COLEGIO_PROCEDENCIA { get; set; }  // aplica a Estudiante

        // Nuevos: salud
        public string? TIPO_SANGRE { get; set; }
        public string? EPS { get; set; }
        public string? ALERGIAS { get; set; }
        public string? CONDICIONES_MEDICAS { get; set; }
        public string? OBSERVACIONES { get; set; }

        // Nuevos: seguridad
        public bool FORZAR_CAMBIO_CLAVE { get; set; }
    }

    [Table("ESTUDIANTE_PADRE")]
    public class EstudiantePadre
    {
        public int ID_PADRE { get; set; }
        public int ID_ESTUDIANTE { get; set; }
        public string? relacion { get; set; }
        public bool ES_PRINCIPAL { get; set; }
    }

    [Table("EVALUACION")]
    public class Evaluacion
    {
        [Key]
        public int id_evaluacion { get; set; }
        public int id_estudiante { get; set; }
        public decimal nota { get; set; }
        public int id_materia { get; set; }
        public int id_curso { get; set; }
        public int id_calificador { get; set; }
        public DateTime fecha_evaluacion { get; set; }
        public string? tipo_evaluacion { get; set; }
    }

    [Table("CITACION")]
    public class Citacion
    {
        [Key]
        public int id_citacion { get; set; }
        public int id_estudiante { get; set; }
        public string? remitente { get; set; }
        public string? asunto { get; set; }
        public string? mensaje { get; set; }
        public DateTime fecha { get; set; }
    }
}