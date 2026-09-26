using System.ComponentModel.DataAnnotations;

namespace PlataformaIncidencias.Models;

public class Incidencia
{
    public int Id { get; set; }

    [Required]
    [Display(Name = "Estación")]
    public string Estacion { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Descripción")]
    public string Descripcion { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Prioridad")]
    public string Prioridad { get; set; } = "Media"; // Alta / Media / Baja

    [Required]
    [Display(Name = "Estado")]
    public string Estado { get; set; } = "Abierta"; // "Abierta" / "Cerrada"

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}
