using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ProyectoUno.Servidor
{
    // Servicio responsable de comunicarse con la API.
    public class ServicioAPI
    {
        private static readonly HttpClient cliente = new HttpClient
        {
            BaseAddress = new Uri("http://localhost:5198/")
        };

        public async Task<RespuestaInicioAPI> IniciarPartidaAsync(List<string> nombres)
        {
            var datos = new
            {
                NombresJugadores = nombres
            };

            string json = JsonConvert.SerializeObject(datos);

            try
            {
                using (var contenido = new StringContent(json, Encoding.UTF8, "application/json"))
                using (HttpResponseMessage respuesta = await cliente.PostAsync("api/Partida/iniciar", contenido))
                {
                    string resultado = await respuesta.Content.ReadAsStringAsync();

                    if (!respuesta.IsSuccessStatusCode)
                    {
                        Console.WriteLine("Error al iniciar partida en API: " + resultado);
                        return null;
                    }

                    return JsonConvert.DeserializeObject<RespuestaInicioAPI>(resultado);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("No se pudo conectar con la API: " + ex.Message);
                return null;
            }
        }

        public async Task RegistrarJugadaAsync(
            int idPartida,
            int idJugador,
            string nombre,
            string color,
            string valor,
            string tipo)
        {
            var datos = new
            {
                IdPartida = idPartida,
                IdJugador = idJugador,
                NombreJugador = nombre,
                Carta = new
                {
                    Color = color,
                    Valor = valor,
                    Tipo = tipo
                }
            };

            await EnviarAsync("api/Partida/log-jugada", datos);
        }

        public async Task RegistrarCartaRobadaAsync(
            int idPartida,
            int idJugador,
            string nombre,
            string color,
            string valor,
            string tipo)
        {
            var datos = new
            {
                IdPartida = idPartida,
                IdJugador = idJugador,
                NombreJugador = nombre,
                Carta = new
                {
                    Color = color,
                    Valor = valor,
                    Tipo = tipo
                }
            };

            await EnviarAsync("api/Partida/log-robar", datos);
        }

        public async Task RegistrarTurnoAsync(
            int idPartida,
            int idJugador,
            string nombre)
        {
            var datos = new
            {
                IdPartida = idPartida,
                IdJugador = idJugador,
                NombreJugador = nombre,
                Carta = (object)null
            };

            await EnviarAsync("api/Partida/log-turno", datos);
        }

        public async Task FinalizarPartidaAsync(
            int idPartida,
            int idJugadorGanador,
            Dictionary<int, int> resultados)
        {
            var listaResultados = new List<object>();

            foreach (var resultado in resultados)
            {
                listaResultados.Add(new
                {
                    IdJugador = resultado.Key,
                    CartasRestantes = resultado.Value
                });
            }

            var datos = new
            {
                IdPartida = idPartida,
                IdJugadorGanador = idJugadorGanador,
                Resultados = listaResultados
            };

            await EnviarAsync("api/Partida/finalizar", datos);
        }

        private async Task EnviarAsync(string endpoint, object datos)
        {
            string json = JsonConvert.SerializeObject(datos);

            using (var contenido = new StringContent(json, Encoding.UTF8, "application/json"))
            using (HttpResponseMessage respuesta = await cliente.PostAsync(endpoint, contenido))
            {
                if (!respuesta.IsSuccessStatusCode)
                {
                    string error = await respuesta.Content.ReadAsStringAsync();

                    throw new Exception("Error de API en " + endpoint + " (HTTP " + (int)respuesta.StatusCode + "): " + error);
                }
            }
        }
    }

    public class RespuestaInicioAPI
    {
        public int IdPartida { get; set; }
        public List<int> IdsJugadores { get; set; }
    }
}