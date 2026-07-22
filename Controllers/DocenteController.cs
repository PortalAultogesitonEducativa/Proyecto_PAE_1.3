using Microsoft.AspNetCore.Mvc;
using ProyectoPAE.Models;
using Microsoft.EntityFrameworkCore;
using Rotativa.AspNetCore;

namespace ProyectoPAE.Controllers
{
    public class DocenteController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DocenteController(ApplicationDbContext context) { _context = context; }

        public IActionResult Planilla()
        {
            // Verificamos sesión y rol (usando UserRol que corregimos antes)
            var rol = HttpContext.Session.GetString("UserRol");
            if (rol != "docente" && rol != "admin") return RedirectToAction("Index", "Login");

            // Traemos solo a los usuarios con rol 'estudiante'
            var estudiantes = _context.Usuarios.Where(u => u.ROL == "estudiante").ToList();
            return View(estudiantes);
        }

        [HttpPost]
        public IActionResult GuardarNota(int ID_Estudiante, string Materia, string Nota, int Periodo)
        {
            // 1. Convertimos el texto de la nota a decimal usando el formato universal (punto)
            decimal notaDecimal = decimal.Parse(Nota.Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture);

            var nuevaNota = new Calificacion
            {
                ID_Estudiante = ID_Estudiante,
                Materia = Materia,
                Nota = notaDecimal, // Usamos el valor ya convertido
                Periodo = Periodo,
                FechaRegistro = DateTime.Now
            };

            _context.Calificaciones.Add(nuevaNota);
            _context.SaveChanges();

            return RedirectToAction("Planilla");
        }
        public IActionResult ReporteGrupal(string materia, int? cursoId)
        {
            // Consultamos las notas incluyendo los datos del estudiante
            var reporte = _context.Calificaciones
                .Join(_context.Usuarios,
                      c => c.ID_Estudiante,
                      u => u.ID_Usuario,
                      (c, u) => new { c, u })
                .Select(res => new {
                    NombreEstudiante = res.u.NOMBRES + " " + res.u.APELLIDOS,
                    Materia = res.c.Materia,
                    Nota = res.c.Nota,
                    Periodo = res.c.Periodo
                }).ToList();

            // Filtros opcionales (RF 5.5)
            if (!string.IsNullOrEmpty(materia))
            {
                reporte = reporte.Where(r => r.Materia == materia).ToList();
            }

            return View(reporte);
        }
        public IActionResult DescargarObservadorPdf()
        {
            // Aquí posteriormente buscaremos los datos reales de la tabla 'Observador' con SQL
            // Por ahora, pasaremos la acción hacia la vista que se transformará en PDF

            return new ViewAsPdf("ReporteObservadorPdf")
            {
                FileName = $"Reporte_Observador_{DateTime.Now:yyyyMMdd}.pdf",
                PageSize = Rotativa.AspNetCore.Options.Size.A4,
                PageOrientation = Rotativa.AspNetCore.Options.Orientation.Portrait
            };
        }
    }
}

