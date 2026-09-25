using PlataformaCreditos.Models;

namespace PlataformaCreditos.Services
{
    public static class SolicitudValidator
    {
        public static bool PuedeAprobarse(SolicitudCredito solicitud, Cliente cliente)
        {
            return solicitud.MontoSolicitado <= cliente.IngresosMensuales * 5;
        }
    }
}