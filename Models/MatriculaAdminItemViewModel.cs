using System;

namespace ProyectoPAE.Models
{
    public class MatriculaAdminItemViewModel
    {
        public int IdMatricula { get; set; }
        public int IdEstudiante { get; set; }
        public string NombreEstudiante { get; set; } = "";
        public string CodigoEstudiante { get; set; } = "";
        public int GradoSolicitado { get; set; }
        public int? IdCurso { get; set; }
        public string NombreCurso { get; set; } = "Sin asignar";
        public int Grado { get; set; } // Compatibilidad: GradoSolicitado o grado de curso
        public DateTime FechaMatricula { get; set; }
        public string PeriodoAcademico { get; set; } = "";
        public int Anio { get; set; }
        public string Estado { get; set; } = "Registrada"; // Estado de la matrícula
        public string EstadoAcademico { get; set; } = "Pendiente de resultado"; // Académico del grado cursado
        public string EstadoDocumental { get; set; } = "Pendiente de revisión"; // Administrativo/Documental
        public string EstadoAcademicoAnterior { get; set; } = "Sin antecedentes"; // Info grado previo
        public string? DocumentosPendientes { get; set; }
        public string? Observaciones { get; set; }
        public int CertificadosExternosVerificados { get; set; }
        public int CertificadosExternosPendientes { get; set; }
        public bool PuedeAsignarCurso => EstadoDocumental == "Aprobada" && (Estado == "Activa" || Estado == "Registrada");
    }
}
