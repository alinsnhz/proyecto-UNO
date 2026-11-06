using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ProyectoUno.Servidor
{
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
                {
                    HttpResponseMessage respuesta = await cliente.PostAsync("api/Partida/iniciar", contenido);
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
            string nombreGanador,
            List<ResultadoJugadorAPI> resultados)
        {
            var datos = new
            {
                IdPartida = idPartida,
                NombreGanador = nombreGanador,
                Resultados = resultados
            };

            return await EnviarAsync("api/Partida/finalizar", datos);
        }

        private async Task<bool> EnviarAsync(string endpoint, object datos)
        {
            try
            {
                string json = JsonConvert.SerializeObject(datos);

                using (var contenido = new StringContent(json, Encoding.UTF8, "application/json"))
                {
                    HttpResponseMessage respuesta = await cliente.PostAsync(endpoint, contenido);

                    if (!respuesta.IsSuccessStatusCode)
                    {
                        string error = await respuesta.Content.ReadAsStringAsync();
                        Console.WriteLine("Error de API en " + endpoint + ": " + error);
                        return false;
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error enviando información a la API: " + ex.Message);
                return false;
            }
        }
    }

    public class RespuestaInicioAPI
    {
        public int IdPartida { get; set; }
        public List<int> IdsJugadores { get; set; }
    }

    public class ResultadoJugadorAPI
    {
        public string NombreJugador { get; set; }
        public int CartasRestantes { get; set; }
    }
}