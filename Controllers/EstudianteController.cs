using Microsoft.AspNetCore.Mvc;
using ProyectoPAE.Models;
using Microsoft.EntityFrameworkCore;

public class EstudianteController : Controller
{
    private readonly ApplicationDbContext _context;
    public EstudianteController(ApplicationDbContext context) { _context = context; }
    // Solo debe existir una versión de este método en el archivo
    public IActionResult MisNotas(int? idEstudiante)
    {
        int userId;
        // Intentamos obtener el ID de la sesión del usuario actual
        var userIdStr = HttpContext.Session.GetString("UserIdStr");

        if (idEstudiante.HasValue)
        {
            // CASO ACUDIENTE: Se usa el ID del hijo pasado por la URL
            userId = idEstudiante.Value;
        }
        else if (int.TryParse(userIdStr, out int sessionUserId))
        {
            // CASO ESTUDIANTE: Se usa el ID de quien inició sesión
            userId = sessionUserId;
        }
        else
        {
            // Si no hay ID ni sesión, redirigir al Login
            return RedirectToAction("Index", "Login");
        }

        // Consulta a la base de datos filtrando por el ID determinado
        var notas = _context.Calificaciones
                            .Where(n => n.ID_Estudiante == userId)
                            .ToList();

        // RF 5.2: Cálculo automático del promedio para la vista
        double promedio = notas.Any() ? notas.Average(n => (double)n.Nota) : 0.0;
        ViewBag.PromedioGeneral = promedio;

        return View(notas);
    }
}
