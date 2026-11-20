using System;
using System.Collections.Generic;
using System.Linq;

namespace ProyectoPAE.Models
{
    /// <summary>Detalle de solo lectura de un docente para el coordinador.</summary>
    public class DetalleDocenteViewModel
    {
        public int IdUsuario { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public int? AnioEscolar { get; set; }
        public double HorasSemanales { get; set; }

        public List<ClaseDocenteItem> Clases { get; set; } = new();
        public List<ChoqueHorarioItem> Choques { get; set; } = new();
        public List<ActividadDocenteItem> Actividades { get; set; } = new();
        public List<PlanMejoraDocenteItem> Planes { get; set; } = new();

        public int ActividadesVigentes => Actividades.Count(a => a.Vigente);
        public int EntregasSinCalificar => Actividades.Sum(a => a.SinCalificar);
        public int EntregasAtrasadas => Actividades.Sum(a => a.SinCalificarAtrasadas);
        public int PlanesVencidos => Planes.Count(p => p.Vencido);
    }

    public class ClaseDocenteItem
    {
        public string Dia { get; set; } = string.Empty;
        public string Inicio { get; set; } = string.Empty;
        public string Fin { get; set; } = string.Empty;
        public string Curso { get; set; } = string.Empty;
        public string Materia { get; set; } = string.Empty;
        public string Aula { get; set; } = string.Empty;
    }

    public class ActividadDocenteItem
    {
        public string Titulo { get; set; } = string.Empty;
        public string Materia { get; set; } = string.Empty;
        public string Grado { get; set; } = string.Empty;
        public DateTime FechaLimite { get; set; }
        public bool Vigente { get; set; }
        public int Entregas { get; set; }
        public int SinCalificar { get; set; }
        public int SinCalificarAtrasadas { get; set; }
    }

    public class PlanMejoraDocenteItem
    {
        public string Estudiante { get; set; } = string.Empty;
        public string Materia { get; set; } = string.Empty;
        public string Grado { get; set; } = string.Empty;
        public int Periodo { get; set; }
        public string Estado { get; set; } = string.Empty;
        public DateTime? FechaCompromiso { get; set; }
        public bool Vencido { get; set; }
    }
}
