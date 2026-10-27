using System;

namespace ProyectoPAE.Models
{
    public class HistorialAcademicoEstudianteViewModel
    {
        public int IdEstudiante { get; set; }
        public string NombreCompleto { get; set; } = "";
        public string CodigoEstudiante { get; set; } = "";
        public string Documento { get; set; } = "";
        public int? UltimoGradoAprobado { get; set; }
        public int SiguienteGradoHabilitado { get; set; }
        public string EstadoAcademicoGeneral { get; set; } = "Sin historial";
        public List<HistorialItemViewModel> Registros { get; set; } = new();
    }

    public class HistorialItemViewModel
    {
        public int IdRegistro { get; set; }
        public string Tipo { get; set; } = "Matrícula"; // "Matrícula" o "Certificado Externo"
        public int Grado { get; set; }
        public string CursoOInstitucion { get; set; } = "";
        public int AnioLectivo { get; set; }
        public DateTime FechaRegistro { get; set; }
        public string EstadoMatricula { get; set; } = "-";
        public string ResultadoAcademico { get; set; } = "Pendiente de resultado"; // "Aprobado", "Reprobado", "Pendiente de resultado"
        public string EstadoDocumental { get; set; } = "-";
        public string? Observaciones { get; set; }
        public bool EsExterno { get; set; }
        public string? EstadoVerificacion { get; set; } // "Verificado", "Pendiente de verificación", "Rechazado"
        public string? RevisadoPor { get; set; }
        public DateTime? FechaRevision { get; set; }
        public string? ArchivoRuta { get; set; }
    }
}
