using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ProyectoUno.API.DTOs
{
    public class IniciarPartidaDto
    {
        [Required]
        [MinLength(3)]
        [MaxLength(3)]
        public List<string> NombresJugadores { get; set; } = new List<string>();
    }

    public class RegistrarJugadaDto
    {
        [Range(1, int.MaxValue)]
        public int IdPartida { get; set; }

        [Range(1, int.MaxValue)]
        public int IdJugador { get; set; }

        [Required]
        [StringLength(100)]
        public string? NombreJugador { get; set; }

        [Required]
        public CartaDto? Carta { get; set; }
    }

    public class RegistrarTurnoDto
    {
        [Range(1, int.MaxValue)]
        public int IdPartida { get; set; }

        [Range(1, int.MaxValue)]
        public int IdJugador { get; set; }

        [Required]
        [StringLength(100)]
        public string? NombreJugador { get; set; }
    }

    public class CartaDto
    {
        [Required]
        [StringLength(30)]
        public string? Color { get; set; }

        [Required]
        [StringLength(30)]
        public string? Valor { get; set; }

        [Required]
        [StringLength(30)]
        public string? Tipo { get; set; }
    }

    public class RegistrarAccionEspecialDto
    {
        [Range(1, int.MaxValue)]
        public int IdPartida { get; set; }

        [Range(1, int.MaxValue)]
        public int IdJugador { get; set; }

        [Required]
        [StringLength(100)]
        public string? NombreJugador { get; set; }

        [Required]
        [StringLength(30)]
        public string? TipoAccion { get; set; }
    }

    public class RegistrarCambioColorDto
    {
        [Range(1, int.MaxValue)]
        public int IdPartida { get; set; }

        [Range(1, int.MaxValue)]
        public int IdJugador { get; set; }

        [Required]
        [StringLength(100)]
        public string? NombreJugador { get; set; }

        [Required]
        [StringLength(30)]
        public string? NuevoColor { get; set; }
    }

    public class FinalizarPartidaDto
    {
        [Range(1, int.MaxValue)]
        public int IdPartida { get; set; }

        [Range(1, int.MaxValue)]
        public int IdJugadorGanador { get; set; }

        [StringLength(100)]
        public string? NombreGanador { get; set; }

        [Required]
        [MinLength(3)]
        [MaxLength(3)]
        public List<ResultadoJugadorDto> Resultados { get; set; }
            = new List<ResultadoJugadorDto>();
    }

    public class ResultadoJugadorDto
    {
        [Range(1, int.MaxValue)]
        public int IdJugador { get; set; }

        [StringLength(100)]
        public string? NombreJugador { get; set; }

        [Range(0, int.MaxValue)]
        public int CartasRestantes { get; set; }
    }
}
