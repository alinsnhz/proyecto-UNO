using System.Collections.Generic;
using Newtonsoft.Json;

namespace ProyectoUno.Protocolo
{
    public class EstadoPartidaDto
    {
        [JsonProperty("jugadorActual")]
        public string JugadorActual { get; set; }

        [JsonProperty("cartaActual")]
        public string CartaActual { get; set; }

        [JsonProperty("sentido")]
        public int Sentido { get; set; }

        [JsonProperty("partidaTerminada")]
        public bool PartidaTerminada { get; set; }

        [JsonProperty("partidaIniciada")]
        public bool PartidaIniciada { get; set; }

        [JsonProperty("jugadoresRegistrados")]
        public int JugadoresRegistrados { get; set; }

        [JsonProperty("ganador")]
        public string Ganador { get; set; }

        [JsonProperty("cartaCentro")]
        public CartaEstadoDto CartaCentro { get; set; }

        [JsonProperty("jugadores")]
        public List<JugadorEstadoDto> Jugadores { get; set; }
            = new List<JugadorEstadoDto>();

        // contiene únicamente las cartas del jugador al que se le está enviando el estado
        [JsonProperty("misCartas")]
        public List<CartaEstadoDto> MisCartas { get; set; }
            = new List<CartaEstadoDto>();
    }

    public class JugadorEstadoDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("nombre")]
        public string Nombre { get; set; }

        [JsonProperty("cantidadCartas")]
        public int CantidadCartas { get; set; }

        [JsonProperty("esTurnoActual")]
        public bool EsTurnoActual { get; set; }

        [JsonProperty("debeDeclararUNO")]
        public bool DebeDeclararUNO { get; set; }
    }

    public class CartaEstadoDto
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("color")]
        public string Color { get; set; }

        [JsonProperty("valor")]
        public string Valor { get; set; }

        [JsonProperty("tipo")]
        public string Tipo { get; set; }
    }
}
