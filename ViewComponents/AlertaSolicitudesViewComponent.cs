using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ProyectoPAE.Servicios;

namespace ProyectoPAE.ViewComponents
{
    /// <summary>
    /// Banner de alerta con las solicitudes de certificados vencidas o próximas a vencer.
    /// Solo se muestra a quienes gestionan solicitudes (coordinador y admin) y únicamente si hay algo que advertir.
    /// </summary>
    public class AlertaSolicitudesViewComponent : ViewComponent
    {
        private readonly SolicitudCertificadoService _servicio;

        public AlertaSolicitudesViewComponent(SolicitudCertificadoService servicio)
        {
            _servicio = servicio;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            if (!Permisos.TienePermiso(HttpContext, Permisos.GestionarSolicitudesCertificado))
                return Content(string.Empty);

            var resumen = await _servicio.ObtenerResumenAlertasAsync();
            if (resumen.Vencidas == 0 && resumen.PorVencer == 0)
                return Content(string.Empty);

            return View(resumen);
        }
    }
}