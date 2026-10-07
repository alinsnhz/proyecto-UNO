using System.Collections.Generic;

namespace ProyectoUno.API.DTOs
{
    public class IniciarPartidaDto
    {
        public List<string> NombresJugadores { get; set; } = new List<string>();
    }

    public class RegistrarJugadaDto
    {
        public int IdPartida { get; set; }
        public int IdJugador { get; set; }
        public string? NombreJugador { get; set; }
        public CartaDto? Carta { get; set; }
    }

    public class CartaDto
    {
        public string? Color { get; set; }
        public string? Valor { get; set; }
        public string? Tipo { get; set; }
    }
}