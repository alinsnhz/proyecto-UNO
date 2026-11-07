
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

            return await EnviarAsync("api/Partida/log-jugada", datos);
        }

        public async Task<bool> RegistrarCartaRobadaAsync(
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

            return await EnviarAsync("api/Partida/log-robar", datos);
        }

        public async Task<bool> RegistrarTurnoAsync(
            int idPartida,
            int idJugador,
            string nombre)
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
            int idPartida,
            int idJugador,
            string nombre,
            string tipoAccion)
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

        // Se conserva el registro de cambio de color de la main.
        public async Task<bool> RegistrarCambioColorAsync(
            int idPartida,
            int idJugador,
            string nombre,
            string nuevoColor)
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

        // Registro de efectos especiales de la branch 66.
        public async Task<bool> RegistrarEfectoAsync(
            int idPartida,
            int idJugador,
            string nombre,
            string efecto)
        {
            var datos = new
            {
                IdPartida = idPartida,
                IdJugador = idJugador,
                NombreJugador = nombre,
                Efecto = efecto
            };

            return await EnviarAsync("api/Partida/log-efecto", datos);
        }

        // Finalización con los resultados identificados por ID.
        public async Task<bool> FinalizarPartidaAsync(
            int idPartida,
            int idJugadorGanador,
            List<ResultadoJugadorFinalAPI> resultados)
        {
            var datos = new
            {
                IdPartida = idPartida,
                IdJugadorGanador = idJugadorGanador,
                Resultados = resultados
            };

            return await EnviarAsync("api/Partida/finalizar", datos);
        }

        public async Task<bool> FinalizarPartidaAsync(int idPartida, int idJugadorGanador, Dictionary<int, int> resultados)
        {
            var lista = new List<ResultadoJugadorFinalAPI>();

            foreach (var resultado in resultados)
            {
                lista.Add(new ResultadoJugadorFinalAPI
                {
                    IdJugador = resultado.Key,
                    CartasRestantes = resultado.Value
                });
            }

            return await FinalizarPartidaAsync(
                idPartida,
                idJugadorGanador,
                lista);
        }

        // Método común para enviar información a la API.
        private async Task<bool> EnviarAsync(string endpoint, object datos)
        {
            string json = JsonConvert.SerializeObject(datos);

            try
            {
                using (var contenido = new StringContent(
                    json, Encoding.UTF8, "application/json"))
                using (HttpResponseMessage respuesta =
                    await cliente.PostAsync(endpoint, contenido))
                {
                    if (!respuesta.IsSuccessStatusCode)
                    {
                        string error = await respuesta.Content.ReadAsStringAsync();

                        Console.WriteLine(
                            "Error de API en " + endpoint + ": " + error);

                        return false;
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    "Error enviando información a la API: " + ex.Message);

                return false;
            }
        }
    }

    public class RespuestaInicioAPI
    {
        public int IdPartida { get; set; }
        public List<int> IdsJugadores { get; set; }
    }

    // Se conserva temporalmente por compatibilidad con código anterior.
    public class ResultadoJugadorAPI
    {
        public string NombreJugador { get; set; }
        public int CartasRestantes { get; set; }
    }
    public class ResultadoJugadorFinalAPI
    {
        public int IdJugador { get; set; }
        public int CartasRestantes { get; set; }
    }
}