
using Newtonsoft.Json;

namespace ProyectoUno.Protocolo
{
    public class Mensajes
    {
        [JsonProperty("tipo")]
        public string Tipo { get; set; }

        [JsonProperty("idSolicitud")]
        public string IdSolicitud { get; set; }

        [JsonProperty("jugadorNombre")]
        public string JugadorNombre { get; set; }

        [JsonProperty("jugadorId")]
        public int? JugadorId { get; set; }

        [JsonProperty("cartaId")]
        public string CartaId { get; set; }

        [JsonProperty("aceptado")]
        public bool? Aceptado { get; set; }

        [JsonProperty("mensaje")]
        public string Mensaje { get; set; }

        [JsonProperty("estadoPartida")]
        public EstadoPartidaDto EstadoPartida { get; set; }
    }
}
