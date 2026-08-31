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
        public IActionResult GuardarNota(int? ID_Estudiante, string Materia, string Nota, int? Periodo)
        {
            try
            {
                // --- (David) GUARDADO Y PROCESAMIENTO DE CALIFICACIONES CON NOTIFICACIONES ---
                string materiaSel = !string.IsNullOrEmpty(Materia) ? Materia : Request.Form["selectAsignatura"].ToString();
                if (string.IsNullOrEmpty(materiaSel)) materiaSel = "Matemáticas - 6°A";

                int periodoVal = Periodo ?? 1;
                if (Request.Form.ContainsKey("selectPeriodo") && int.TryParse(Request.Form["selectPeriodo"], out int pParsed))
                {
                    periodoVal = pParsed;
                }

                // 1. Si se envía una nota individual
                if (ID_Estudiante.HasValue && !string.IsNullOrEmpty(Nota))
                {
                    decimal notaDecimal = decimal.Parse(Nota.Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture);
                    if (notaDecimal >= 0 && notaDecimal <= 5)
                    {
                        var nuevaNota = new Calificacion
                        {
                            ID_Estudiante = ID_Estudiante.Value,
                            Materia = materiaSel,
                            Nota = notaDecimal,
                            Periodo = periodoVal,
                            FechaRegistro = DateTime.Now
                        };
                        _context.Calificaciones.Add(nuevaNota);
                    }
                }
                // 2. Si se envía el formulario desde la planilla
                else
                {
                    var estudianteIds = Request.Form["estudiante_id"];
                    var notasDefinitivas = Request.Form["nota_valor"];

                    if (estudianteIds.Count > 0)
                    {
                        for (int i = 0; i < estudianteIds.Count; i++)
                        {
                            if (int.TryParse(estudianteIds[i], out int estId))
                            {
                                string strNota = (notasDefinitivas.Count > i) ? notasDefinitivas[i] : "0.0";
                                if (decimal.TryParse(strNota.Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture, out decimal dNota))
                                {
                                    var calif = new Calificacion
                                    {
                                        ID_Estudiante = estId,
                                        Materia = materiaSel,
                                        Nota = dNota,
                                        Periodo = periodoVal,
                                        FechaRegistro = DateTime.Now
                                    };
                                    _context.Calificaciones.Add(calif);
                                }
                            }
                        }
                    }
                }

                _context.SaveChanges();

                // --- (David) NOTIFICACIÓN DE ÉXITO AL GUARDAR ---
                TempData["MensajeNota"] = "¡Las calificaciones han sido guardadas exitosamente!";
            }
            catch (Exception ex)
            {
                // --- (David) NOTIFICACIÓN EN CASO DE ERROR ---
                TempData["ErrorNota"] = "Ocurrió un error al guardar las calificaciones: " + ex.Message;
            }

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
                    IdEstudiante = res.u.ID_Usuario,
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
