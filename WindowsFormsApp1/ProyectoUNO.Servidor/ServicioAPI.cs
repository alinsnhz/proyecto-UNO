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
            var datos = new { NombresJugadores = nombres };
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

        public async Task<bool> RegistrarJugadaAsync(
            int idPartida, int idJugador, string nombre,
            string color, string valor, string tipo)
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

            return await EnviarAsync("api/Partida/log-jugada", datos);
        }

        public async Task<bool> RegistrarCartaRobadaAsync(
            int idPartida, int idJugador, string nombre,
            string color, string valor, string tipo)
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

            return await EnviarAsync("api/Partida/log-robar", datos);
        }

        public async Task<bool> RegistrarTurnoAsync(
            int idPartida, int idJugador, string nombre)
        {
            var datos = new
            {
                IdPartida = idPartida,
                IdJugador = idJugador,
                NombreJugador = nombre
            };

            return await EnviarAsync("api/Partida/log-turno", datos);
        }

        public async Task<bool> RegistrarAccionEspecialAsync(
            int idPartida, int idJugador,
            string nombre, string tipoAccion)
        {
            var datos = new
            {
                IdPartida = idPartida,
                IdJugador = idJugador,
                NombreJugador = nombre,
                TipoAccion = tipoAccion
            };

            return await EnviarAsync("api/Partida/log-accion-especial", datos);
        }

        public async Task<bool> RegistrarCambioColorAsync(
            int idPartida, int idJugador,
            string nombre, string nuevoColor)
        {
            var datos = new
            {
                IdPartida = idPartida,
                IdJugador = idJugador,
                NombreJugador = nombre,
                NuevoColor = nuevoColor
            };

            return await EnviarAsync("api/Partida/log-cambio-color", datos);
        }

        public async Task<bool> FinalizarPartidaAsync(
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

            return await EnviarAsync("api/Partida/finalizar", datos);
        }

        private async Task<bool> EnviarAsync(string endpoint, object datos)
        {
            string json = JsonConvert.SerializeObject(datos);

            using (var contenido = new StringContent(json, Encoding.UTF8, "application/json"))
            using (HttpResponseMessage respuesta = await cliente.PostAsync(endpoint, contenido))
            {
                if (!respuesta.IsSuccessStatusCode)
                {
                    string error = await respuesta.Content.ReadAsStringAsync();

                    throw new Exception(
                        "Error de API en " + endpoint +
                        " (HTTP " + (int)respuesta.StatusCode + "): " + error);
                }

                return true;
            }
        }
    }

    public class RespuestaInicioAPI
    {
        public int IdPartida { get; set; }
        public List<int> IdsJugadores { get; set; }
    }

    // Se conserva temporalmente por compatibilidad con el código del servidor.
    public class ResultadoJugadorAPI
    {
        public string NombreJugador { get; set; }
        public int CartasRestantes { get; set; }
    }
}
