using Newtonsoft.Json;
using ProyectoUno.Protocolo;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace ProyectoUno.Servidor
{
    public class ServidorSocket
    {
        private const int Puerto = 11000;
        private const int MaxJugadores = 3;
        private TcpListener listener;
        private readonly PartidaServidor partida = new PartidaServidor();
        private readonly ConcurrentDictionary<int, JugadorServidor> clientes = new ConcurrentDictionary<int, JugadorServidor>();
        private readonly SemaphoreSlim lugares =  new SemaphoreSlim(MaxJugadores, MaxJugadores);
        private int siguienteId = 0;
        private volatile bool detenido = false;
        private readonly ServicioAPI servicioAPI = new ServicioAPI();
        private int idPartidaBD = 0;
        private int partidaFinalizadaBD = 0;
        private readonly ConcurrentDictionary<int, int> idsJugadoresBD = new ConcurrentDictionary<int, int>();

        public async Task IniciarAsync()
        {
            listener = new TcpListener(IPAddress.Any, Puerto);

            listener.Start();

            detenido = false;

            Console.WriteLine(" SERVIDOR UNO INICIADO");

            Console.WriteLine("Puerto: " + Puerto);
            Console.WriteLine("Esperando jugadores...");

            try
            {
                while (!detenido)
                {
                    TcpClient cliente =
                        await listener.AcceptTcpClientAsync();

                    if (!await lugares.WaitAsync(0))
                    {
                        await RechazarConexionAsync(cliente);
                        continue;
                    }

                    int id = Interlocked.Increment(ref siguienteId);

                    JugadorServidor jugador =
                        new JugadorServidor(id, cliente);

                    clientes.TryAdd(id, jugador);

                    Console.WriteLine("Nueva conexión. ID asignado: " + id);

                    Console.WriteLine("Conexiones: " + clientes.Count + "/" + MaxJugadores);

                    = AtenderClienteAsync(jugador);
                }
            }
            catch (ObjectDisposedException)
            {
                // El servidor fue detenido.
            }
            catch (SocketException)
            {
                if (!detenido)
                    throw;
            }
        }
        private async Task AtenderClienteAsync(JugadorServidor jugador)
        {
            try
            {
                while (true)
                {
                    string texto = await jugador.RecibirAsync();

                    if (texto == null)
                        break;

                    Console.WriteLine("Mensaje recibido del jugador " + jugador.Id + ": " + texto);

                    Mensajes mensaje;

                    try
                    {
                        mensaje = JsonConvert.DeserializeObject<Mensajes>(texto);
                    }
                    catch (JsonException)
                    {
                        await EnviarRespuestaAsync(jugador, "Error", false, "El mensaje JSON no es válido");

                        continue;
                    }

                    if (mensaje == null)
                        continue;

                    if (!jugador.Registrado)
                    {
                        await ProcesarRegistroAsync(jugador, mensaje);

                        continue;
                    }

                    Mensajes respuesta = partida.ProcesarAccion(jugador.Id, mensaje);

                    respuesta.JugadorId = jugador.Id;

                    if (respuesta.Aceptado == true)
                    {
                        await RegistrarAccionBDAsync(jugador, mensaje);
                    }

                    await jugador.EnviarAsync(respuesta);

                    if (respuesta.Aceptado == true)
                    {
                        await NotificarEstadoPartidaAsync();

                        if (partida.Juego.Ganador != null)
                        {
                            await FinalizarPartidaBDAsync();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    "Error con jugador " +
                    jugador.Id + ": " + ex.Message
                );
            }
            finally
            {
                bool estabaRegistrado = jugador.Registrado;

                partida.EliminarJugador(jugador);

                clientes.TryRemove(jugador.Id, out _);

                jugador.Desconectar();

                lugares.Release();

                Console.WriteLine(
                    "Conexiones restantes: " +
                    clientes.Count + "/" + MaxJugadores
                );

                if (partida.Interrumpida)
                {
                    await NotificarInterrupcionAsync();
                }
                else if (estabaRegistrado &&
                        partida.CantidadJugadores > 0)
                {
                    await NotificarSalaAsync();
                }

                if (partida.CantidadJugadores == 0)
                {
                    Console.WriteLine("Esperando una nueva partida...");
                }
            }
        }

        private async Task ProcesarRegistroAsync(JugadorServidor jugador, Mensajes mensaje)
        {
            if (mensaje.Tipo == null ||
                !mensaje.Tipo.Equals("Conectar", StringComparison.OrdinalIgnoreCase))
            {
                await EnviarRespuestaAsync(jugador, "Error", false, "Debes enviar primero un mensaje de conexión");

                return;
            }

            if (string.IsNullOrWhiteSpace(mensaje.JugadorNombre))
            {
                await EnviarRespuestaAsync(jugador, "Error", false, "Debes proporcionar un nombre de jugador");

                return;
            }

            jugador.Nombre = mensaje.JugadorNombre.Trim();

            bool registrado =
                partida.RegistrarJugador(jugador);

            if (!registrado)
            {
                await EnviarRespuestaAsync(jugador, "Error", false, "No fue posible registrar al jugador" 
                    + "La sala puede estar llena o la partida interrumpida");

                return;
            }

            Console.WriteLine("Jugador " + jugador.Id + " registrado con nombre: " + jugador.Nombre);
            if (partida.Iniciada)
            {
                var jugadoresPartida = partida.ObtenerJugadores();

                List<string> nombres = new List<string>();

                foreach (JugadorServidor participante in jugadoresPartida)
                {
                    nombres.Add(participante.Nombre);
                }

                RespuestaInicioAPI resultado =
                    await servicioAPI.IniciarPartidaAsync(nombres);

                if (resultado != null &&
                    resultado.IdPartida > 0 &&
                    resultado.IdsJugadores != null &&
                    resultado.IdsJugadores.Count == jugadoresPartida.Count)
                {
                    idPartidaBD = resultado.IdPartida;

                    for (int i = 0; i < jugadoresPartida.Count; i++)
                    {
                        idsJugadoresBD[jugadoresPartida[i].Id] =
                            resultado.IdsJugadores[i];
                    }

                    Console.WriteLine(
                        "Partida registrada en MySQL. ID: " + idPartidaBD);
                }
                else
                {
                    Console.WriteLine(
                        "No se pudo registrar la partida en la base de datos.");
                }
            }

            await jugador.EnviarAsync(
                new Mensajes
                {
                    Tipo = "Conectado",
                    JugadorId = jugador.Id,
                    Aceptado = true,
                    Mensaje = "Te has registrado correctamente."
                }
            );

            await NotificarSalaAsync();
        }

        private async Task EnviarRespuestaAsync(JugadorServidor jugador, string tipo, bool aceptado, string mensaje)
        {
            await jugador.EnviarAsync(
                new Mensajes
                {
                    Tipo = tipo,
                    JugadorId = jugador.Id,
                    Aceptado = aceptado,
                    Mensaje = mensaje
                }
            );
        }

        // Envía a cada jugador su propio estado personalizado.
        private async Task NotificarEstadoPartidaAsync()
        {
            var jugadores = partida.ObtenerJugadores();

            foreach (JugadorServidor jugador in jugadores)
            {
                try
                {
                    EstadoPartidaDto estado = partida.CrearEstadoPara(jugador.Id);

                    await jugador.EnviarAsync(
                        new Mensajes
                        {
                            Tipo = "ActualizacionPartida",
                            Aceptado = true,
                            Mensaje = "Estado actualizado.",
                            EstadoPartida = estado
                        }
                    );
                }
                catch (Exception ex)
                {
                    Console.WriteLine("No se pudo actualizar al jugador " + jugador.Id + ": " + ex.Message);
                }
            }
        }

        private async Task NotificarSalaAsync()
        {
            var jugadores = partida.ObtenerJugadores();

            foreach (JugadorServidor jugador in jugadores)
            {
                try
                {
                    EstadoPartidaDto estado =
                        partida.CrearEstadoPara(jugador.Id);

                    string tipo = partida.Iniciada
                        ? "PartidaIniciada"
                        : "EsperandoJugadores";

                    string mensaje = partida.Iniciada
                        ? "Los tres jugadores están conectados. " +
                          "¡Comienza la partida!"
                        : "Esperando a los demás jugadores.";

                    await jugador.EnviarAsync(
                        new Mensajes
                        {
                            Tipo = tipo,
                            Aceptado = true,
                            Mensaje = mensaje,
                            EstadoPartida = estado
                        }
                    );
                }
                catch (Exception ex)
                {
                    Console.WriteLine("No se pudo notificar al jugador " + jugador.Id + ": " + ex.Message);
                }
            }
        }

        private async Task NotificarInterrupcionAsync()
        {
            var jugadores = partida.ObtenerJugadores();

            foreach (JugadorServidor jugador in jugadores)
            {
                try
                {
                    await jugador.EnviarAsync(
                        new Mensajes
                        {
                            Tipo = "PartidaInterrumpida",
                            Aceptado = false,
                            Mensaje =
                                "Un jugador se desconectó " +
                                "La partida ha sido interrumpida"
                        }
                    );
                }
                catch (Exception ex)
                {
                    Console.WriteLine("No se pudo notificar al jugador " + jugador.Id + ": " + ex.Message);
                }
            }
        }

        private async Task RechazarConexionAsync(TcpClient cliente)
        {
            try
            {
                using (var escritor = new System.IO.StreamWriter(cliente.GetStream()))
                {
                    escritor.AutoFlush = true;

                    Mensajes respuesta = new Mensajes
                    {
                        Tipo = "ServidorLleno",
                        Aceptado = false,
                        Mensaje =
                            "La sala ya tiene tres jugadores."
                    };

                    string json = JsonConvert.SerializeObject(respuesta);

                    await escritor.WriteLineAsync(json);
                }
            }
            catch (Exception)
            {
                // El cliente pudo desconectarse
            }
            finally
            {
                cliente.Close();
            }
        }

        public void Detener()
        {
            detenido = true;

            listener?.Stop();

            foreach (var elemento in clientes)
                elemento.Value.Desconectar();

            clientes.Clear();
        }

        private async Task RegistrarAccionBDAsync (JugadorServidor jugadorServidor, Mensajes solicitud)
        {
            if (idPartidaBD <= 0 || partida.Juego == null)
                return;

            try
            {
                string accion = solicitud.Tipo.ToLowerInvariant();

                var jugadorJuego = partida.Juego.jugadores.Find(
                    j => j.ID == jugadorServidor.Id
                );

                if (jugadorJuego == null)
                    return;

                if (!idsJugadoresBD.TryGetValue(
                    jugadorJuego.ID, out int idJugadorBD))
                    return;

                // Registrar carta jugada.
                if (accion == "jugarcarta")
                {
                    var carta = partida.Juego.cartaActual;

                    if (carta != null)
                    {
                        await servicioAPI.RegistrarJugadaAsync(
                            idPartidaBD,
                            idJugadorBD,
                            jugadorJuego.Nombre,
                            carta.Color,
                            carta.Valor,
                            carta.Tipo
                        );
                    }
                }

                else if (accion == "robarcarta" || accion == "robar")
                {
                    var carta = partida.Juego.CartaRobada;
                    var jugadorRobo = partida.Juego.JugadorQueRobo;

                    if (carta != null &&
                        jugadorRobo != null &&
                        idsJugadoresBD.TryGetValue(
                            jugadorRobo.ID, out int idRoboBD))
                    {
                        await servicioAPI.RegistrarCartaRobadaAsync(
                            idPartidaBD,
                            idRoboBD,
                            jugadorRobo.Nombre,
                            carta.Color,
                            carta.Valor,
                            carta.Tipo
                        );
                    }
                }

                if (accion == "jugarcarta" ||
                    accion == "pasarturno" ||
                    accion == "pasar")
                {
                    if (partida.Juego.Ganador == null)
                    {
                        var siguienteJugador =
                            partida.Juego.obtenerJugadorActual();

                        if (siguienteJugador != null &&
                            idsJugadoresBD.TryGetValue(
                                siguienteJugador.ID, out int idSiguienteBD))
                        {
                            await servicioAPI.RegistrarTurnoAsync(
                                idPartidaBD,
                                idSiguienteBD,
                                siguienteJugador.Nombre
                            );
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al registrar la acción en la API: " + ex.Message
                );
            }
        }

        private async Task FinalizarPartidaBDAsync()
        {
            if (idPartidaBD <= 0 || partida.Juego.Ganador == null)
                return;

            if (Interlocked.CompareExchange(ref partidaFinalizadaBD, 1, 0) != 0)
                return;

            int idGanadorServidor = partida.Juego.Ganador.ID;

            if (!idsJugadoresBD.TryGetValue(idGanadorServidor, out int idGanadorBD))
            {
                Interlocked.Exchange(ref partidaFinalizadaBD, 0);
                return;
            }

            var resultados = new Dictionary<int, int>();

            foreach (var jugador in partida.Juego.jugadores)
            {
                if (!idsJugadoresBD.TryGetValue(jugador.ID, out int idJugadorBD))
                {
                    Interlocked.Exchange(ref partidaFinalizadaBD, 0);
                    return;
                }

                resultados[idJugadorBD] = jugador.Cartas.Count;
            }

            try
            {
                await servicioAPI.FinalizarPartidaAsync(
                    idPartidaBD,
                    idGanadorBD,
                    resultados
                );

                Console.WriteLine("Resultado de la partida guardado correctamente en MySQL."
                );
            }
            catch (Exception ex)
            {
                Interlocked.Exchange(ref partidaFinalizadaBD, 0);

                Console.WriteLine("Error al guardar el resultado de la partida: " + ex.Message);
            }
        }
    }
}
