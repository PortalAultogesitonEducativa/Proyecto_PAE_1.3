using System;
using System.Collections.Generic;
using System.Linq;

namespace ProyectoPAE.Models
{
    public class SupervisionDocentesViewModel
    {
        public List<SupervisionDocenteItem> Docentes { get; set; } = new();

        public List<PeriodoSubidaNota> Periodos { get; set; } = new();
        public int PeriodoActivo { get; set; } = 1;

        public int TotalClases => Docentes.Sum(d => d.Clases);
        public int TotalActividadesVigentes => Docentes.Sum(d => d.ActividadesVigentes);
        public int TotalEntregasSinCalificar => Docentes.Sum(d => d.EntregasSinCalificar);
        public int TotalEstudiantes { get; set; }

        public int DocentesConNotasAlDia => Docentes.Count(d => d.Clases == 0 || d.TieneNotasRegistradas);
        public int DocentesConNotasPendientes => Docentes.Count(d => d.Clases > 0 && !d.TieneNotasRegistradas);
    }

    public class SupervisionDocenteItem
    {
        public int IdUsuario { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public int Clases { get; set; }
        public List<string> Materias { get; set; } = new();
        public List<string> Cursos { get; set; } = new();
        public int ActividadesVigentes { get; set; }
        public int ActividadesVencidas { get; set; }
        public int EntregasSinCalificar { get; set; }
        public DateTime? UltimaActividad { get; set; }

        public int NotasRegistradasPeriodoActivo { get; set; }
        public bool TieneNotasRegistradas => NotasRegistradasPeriodoActivo > 0;
    }
}
