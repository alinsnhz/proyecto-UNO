using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace proyectoUNO
{
    public class APICliente
    {
        private static readonly HttpClient client = new HttpClient
        {
            BaseAddress = new Uri("https://localhost:44357/")
        };

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

    public class RespuestaInicioDto
    {
        public int IdPartida { get; set; }
        public List<int> IdsJugadores { get; set; } = new List<int>();
    }
}