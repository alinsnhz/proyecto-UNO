using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ProyectoUno.Servidor
{
    // clase que muestra el error en consola si la API falla y permite al servidor seguir funcionando
    public class ServicioAPI
    {
        private static readonly HttpClient cliente = new HttpClient
        {
            BaseAddress = new Uri("http://localhost:5198/")
        };

        public async Task<RespuestaInicioAPI> IniciarPartidaAsync(
            List<string> nombres)
        {
            var datos = new
            {
                NombresJugadores = nombres
            };

            string json = JsonConvert.SerializeObject(datos);

            try
            {
                using (var contenido = new StringContent(
                    json, Encoding.UTF8, "application/json"))
                {
                    HttpResponseMessage respuesta = await cliente.PostAsync(
                        "api/Partida/iniciar", contenido);

                    string resultado =
                        await respuesta.Content.ReadAsStringAsync();

                    if (!respuesta.IsSuccessStatusCode)
                    {
                        Console.WriteLine(
                            "Error al iniciar partida en API: " + resultado);
                        return null;
                    }

                    return JsonConvert.DeserializeObject<RespuestaInicioAPI>(resultado);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    "No se pudo conectar con la API: " + ex.Message);
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

        public async Task RegistrarEfectoAsync(
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

            await EnviarAsync("api/Partida/log-efecto", datos);
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
}
