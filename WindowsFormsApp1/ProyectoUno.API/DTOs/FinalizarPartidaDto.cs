using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ProyectoUno.API.DTOs
{
    public class FinalizarPartidaDto
    {
        [Range(1, int.MaxValue)]
        public int IdPartida { get; set; }

        [Range(1, int.MaxValue)]
        public int IdJugadorGanador { get; set; }

        [Required]
        public List<ResultadoJugadorFinalDto> Resultados { get; set; } = new List<ResultadoJugadorFinalDto>();
    }

    public class ResultadoJugadorFinalDto
    {
        [Range(1, int.MaxValue)]
        public int IdJugador { get; set; }

        [Range(0, int.MaxValue)]
        public int CartasRestantes { get; set; }
    }
}