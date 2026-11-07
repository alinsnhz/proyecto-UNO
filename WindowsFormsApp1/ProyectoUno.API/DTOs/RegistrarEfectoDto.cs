using System.ComponentModel.DataAnnotations;

namespace ProyectoUno.API.DTOs
{
    public class RegistrarEfectoDto
    {
        [Range(1, int.MaxValue)]
        public int IdPartida { get; set; }

        [Range(1, int.MaxValue)]
        public int IdJugador { get; set; }

        [Required]
        public string NombreJugador { get; set; }

        [Required]
        public string Efecto { get; set; }
    }
}