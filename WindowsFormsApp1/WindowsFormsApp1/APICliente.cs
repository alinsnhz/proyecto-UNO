using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace proyectoUNO
{
    /*
     * Permite que la aplicación Windows Forms se comunique con la API por HTTP
    */
    public class APICliente
    {
        private static readonly HttpClient client = new HttpClient
        {
            BaseAddress = new Uri("https://localhost:7198/")
        };

        //envía los nombres de los jugadores a la API para crear la partida y devuelve la respuesta
        //con los identificadores; si hay error, muestra un mensaje y devuelve null
        public static async Task<RespuestaInicioDto> IniciarPartidaAsync(List<string> nombres)
        {
            try
            {
                var payload = new { NombresJugadores = nombres };
                var response = await client.PostAsJsonAsync("api/Partida/iniciar", payload);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<RespuestaInicioDto>();
                }
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show("Error conectando con la API: " + ex.Message);
            }
            return null;
        }

        //envía a la API los datos de la partida, jugador y carta jugada
        public static async Task RegistrarJugadaAsync(int idPartida, int idJugador, string nombreJugador, string color, string valor, string tipo)
        {
            try
            {
                var payload = new
                {
                    IdPartida = idPartida,
                    IdJugador = idJugador,
                    NombreJugador = nombreJugador,
                    Carta = new { Color = color, Valor = valor, Tipo = tipo }
                };
                await client.PostAsJsonAsync("api/Partida/log-jugada", payload);
            }
            catch { }
        }

        //envía a la API el identificador y nombre del jugador cuyo turno se registra
        public static async Task RegistrarTurnoAsync(int idPartida, int idJugador, string nombreJugador)
        {
            try
            {
                var payload = new
                {
                    IdPartida = idPartida,
                    IdJugador = idJugador,
                    NombreJugador = nombreJugador
                };
                await client.PostAsJsonAsync("api/Partida/log-turno", payload);
            }
            catch { }
        }

        //intenta enviar los datos de una carta robada; comprueba que exista la ruta
        //correspondiente en el controlador porque el cliente llama a api/Partida/log-robar
        public static async Task RegistrarCartaRobadaAsync(int idPartida, int idJugador, string nombreJugador, string color, string valor, string tipo)
        {
            try
            {
                var datos = new
                {
                    IdPartida = idPartida,
                    IdJugador = idJugador,
                    NombreJugador = nombreJugador,
                    Color = color,
                    Valor = valor,
                    Tipo = tipo
                };

                await client.PostAsJsonAsync("api/Partida/log-robar", datos);
            }
            catch { }
        }
    }

    //clase de respuesta que guarda el ID de la partida y la lista de IDs de jugadores
    public class RespuestaInicioDto
    {
        public int IdPartida { get; set; }
        public List<int> IdsJugadores { get; set; } = new List<int>();
    }
}