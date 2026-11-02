using Newtonsoft.Json;

namespace ProyectoUno.Protocolo
{
    public class Mensajes
    {
        //indica que acción o respuesta representa el mensaje
        [JsonProperty("tipo")]
        public string Tipo { get; set; }

        //permite relacionar una respuesta con la solicitud que la originó
        [JsonProperty("idSolicitud")]
        public string IdSolicitud { get; set; }

        //devolver id del jugador
        [JsonProperty("jugadorId")]
        public int? JugadorId { get; set; }

        //identifica al jugador que solicita la acción
        [JsonProperty("jugadorNombre")]
        public string JugadorNombre { get; set; }

        //identifica la carta que se quiere jugar
        [JsonProperty("cartaId")]
        public string CartaId { get; set; }

        //indica si el servidor acpeta la solicitud (null si no aplica)
        [JsonProperty("aceptado")]
        public bool? Aceptado { get; set; }

        //contiene una explicación del resultado (o error)
        [JsonProperty("mensaje")]
        public string Mensaje { get; set; }

        //puede contener una actualización del estado de la partida
        [JsonProperty("estadoPartida")]
        public EstadoPartidaDto EstadoPartida { get; set; }
    }
}