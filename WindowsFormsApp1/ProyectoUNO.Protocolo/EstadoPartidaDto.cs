
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

        [JsonProperty("ganador")]
        public string Ganador { get; set; }

        [JsonProperty("jugadores")]
        public List<JugadorEstadoDto> Jugadores { get; set; }
            = new List<JugadorEstadoDto>();
    }

    public class JugadorEstadoDto
    {
        [JsonProperty("nombre")]
        public string Nombre { get; set; }

        [JsonProperty("cantidadCartas")]
        public int CantidadCartas { get; set; }

        [JsonProperty("esTurnoActual")]
        public bool EsTurnoActual { get; set; }

        [JsonProperty("debeDeclararUNO")]
        public bool DebeDeclararUNO { get; set; }
    }
}
