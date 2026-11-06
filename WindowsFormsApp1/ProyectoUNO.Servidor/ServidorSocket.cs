
using Newtonsoft.Json;
using ProyectoUno.Protocolo;
using proyectoUNO;
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
        private readonly SemaphoreSlim lugares = new SemaphoreSlim(MaxJugadores, MaxJugadores);
        private int siguienteId = 0;
        private volatile bool detenido = false;
        private readonly ServicioAPI servicioAPI = new ServicioAPI();
        private int idPartidaBD = 0;
        private bool partidaRegistradaAPI = false;
        private readonly ConcurrentDictionary<int, int> idsJugadoresBD = new ConcurrentDictionary<int, int>();
        private bool resultadoFinalRegistrado = false;
        private string ultimoJugadorTurnoRegistrado = null;
        private int cantidadCartasPenalizacionRegistradas = 0;
        private readonly SemaphoreSlim bloqueoRegistroAPI = new SemaphoreSlim(1, 1);

        public async Task IniciarAsync()
        {
            listener = new TcpListener(IPAddress.Any, Puerto);
            listener.Start();

            detenido = false;

            Console.WriteLine("SERVIDOR UNO INICIADO");
            Console.WriteLine("Puerto: " + Puerto);
            Console.WriteLine("Esperando jugadores...");

            try
            {
                while (!detenido)
                {
                    TcpClient cliente = await listener.AcceptTcpClientAsync();

                    if (!await lugares.WaitAsync(0))
                    {
                        await RechazarConexionAsync(cliente);
                        continue;
                    }

                    int id = Interlocked.Increment(ref siguienteId);
                    JugadorServidor jugador = new JugadorServidor(id, cliente);

                    clientes.TryAdd(id, jugador);

                    Console.WriteLine("Nueva conexión. ID asignado: " + id);

                    Console.WriteLine("Conexiones: " + clientes.Count + "/" + MaxJugadores);

                    _ = AtenderClienteAsync(jugador);
                }
            }
            catch (ObjectDisposedException)
            {
                //el servidor fue detenido.
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
                        await EnviarRespuestaAsync(
                            jugador,
                            "Error",
                            false,
                            "El mensaje JSON no es válido");

                        continue;
                    }

                    if (mensaje == null)
                        continue;

                    if (!jugador.Registrado)
                    {
                        await ProcesarRegistroAsync(jugador, mensaje);
                        continue;
                    }

                    Jugador jugadorJuego = partida.ObtenerJugador(jugador.Id);
                    string turnoAnterior = null;

                    if (partida.Juego != null)
                    {
                        Jugador actual = partida.Juego.obtenerJugadorActual();

                        if (actual != null) 
                        {
                            turnoAnterior = actual.Nombre;
                        }
                    }

                    Carta cartaJugada = null;

                    string tipoAccion = mensaje.Tipo == null ? "" : mensaje.Tipo.Trim().ToLowerInvariant();

                    if (tipoAccion == "jugarcarta" && jugadorJuego != null && partida.Juego != null)
                    {
                        foreach (Carta carta in jugadorJuego.Cartas)
                        {
                            if (carta.Id == mensaje.CartaId)
                            {
                                cartaJugada = carta;
                                break;
                            }
                        }
                    }

                    Mensajes respuesta = partida.ProcesarAccion(jugador.Id, mensaje);
                    respuesta.JugadorId = jugador.Id;

                    await jugador.EnviarAsync(respuesta);

                    if (respuesta.Aceptado == true)
                    {
                        await RegistrarAccionEnAPIAsync(
                            jugador,
                            mensaje,
                            tipoAccion,
                            cartaJugada,
                            turnoAnterior);

                        await NotificarEstadoPartidaAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error con jugador " + jugador.Id + ": " + ex.Message);
            }
            finally
            {
                bool estabaRegistrado = jugador.Registrado;

                partida.EliminarJugador(jugador);
                clientes.TryRemove(jugador.Id, out _);
                jugador.Desconectar();

                lugares.Release();

                Console.WriteLine("Conexiones restantes: " + clientes.Count + "/" + MaxJugadores);

                if (partida.Interrumpida)
                {
                    await NotificarInterrupcionAsync();
                }
                else if (estabaRegistrado && partida.CantidadJugadores > 0)
                {
                    await NotificarSalaAsync();
                }

                if (partida.CantidadJugadores == 0)
                {
                    Console.WriteLine("Esperando una nueva partida...");

                    idPartidaBD = 0;
                    partidaRegistradaAPI = false;
                    resultadoFinalRegistrado = false;
                    ultimoJugadorTurnoRegistrado = null;
                    cantidadCartasPenalizacionRegistradas = 0;

                    idsJugadoresBD.Clear();
                }
            }
        }

        private async Task ProcesarRegistroAsync(JugadorServidor jugador, Mensajes mensaje)
        {
            if (mensaje.Tipo == null || !mensaje.Tipo.Equals(
                    "Conectar",
                    StringComparison.OrdinalIgnoreCase))
            {
                await EnviarRespuestaAsync(
                    jugador,
                    "Error",
                    false,
                    "Debes enviar primero un mensaje de conexión");

                return;
            }

            if (string.IsNullOrWhiteSpace(mensaje.JugadorNombre))
            {
                await EnviarRespuestaAsync(
                    jugador,
                    "Error",
                    false,
                    "Debes proporcionar un nombre de jugador");

                return;
            }

            jugador.Nombre = mensaje.JugadorNombre.Trim();

            bool registrado = partida.RegistrarJugador(jugador);

            if (!registrado)
            {
                await EnviarRespuestaAsync(
                    jugador,
                    "Error",
                    false,
                    "No fue posible registrar al jugador. " + "La sala puede estar llena o la partida interrumpida.");

                return;
            }

            Console.WriteLine("Jugador " + jugador.Id + " registrado con nombre: " + jugador.Nombre);

            if (partida.Iniciada)
            {
                bool registrada = await RegistrarPartidaEnAPIAsync();

                if (!registrada)
                {
                    Console.WriteLine("La partida inició, pero no fue registrada en la API.");
                }
                else
                {
                    await RegistrarTurnoInicialAsync();
                }
            }

            await jugador.EnviarAsync(
                new Mensajes
                {
                    Tipo = "Conectado",
                    JugadorId = jugador.Id,
                    Aceptado = true,
                    Mensaje = "Te has registrado correctamente."
                });

            await NotificarSalaAsync();
        }

        private async Task<bool> RegistrarPartidaEnAPIAsync()
        {
            if (partidaRegistradaAPI)
                return true;

            var jugadoresPartida = partida.ObtenerJugadores();

            if (jugadoresPartida.Count != MaxJugadores)
            {
                Console.WriteLine("No se puede registrar la partida: faltan jugadores.");

                return false;
            }

            List<string> nombres = new List<string>();

            foreach (JugadorServidor participante in jugadoresPartida)
            {
                nombres.Add(participante.Nombre);
            }

            RespuestaInicioAPI resultado = await servicioAPI.IniciarPartidaAsync(nombres);

            if (resultado == null ||
                resultado.IdPartida <= 0 ||
                resultado.IdsJugadores == null ||
                resultado.IdsJugadores.Count != jugadoresPartida.Count)
            {
                Console.WriteLine("No se pudo registrar la partida en la API.");

                return false;
            }

            idPartidaBD = resultado.IdPartida;
            idsJugadoresBD.Clear();

            for (int i = 0; i < jugadoresPartida.Count; i++)
            {
                idsJugadoresBD[jugadoresPartida[i].Id] = resultado.IdsJugadores[i];
            }

            partidaRegistradaAPI = true;
            resultadoFinalRegistrado = false;
            cantidadCartasPenalizacionRegistradas = 0;

            Console.WriteLine("Partida registrada en MySQL. ID: " + idPartidaBD);
            return true;
        }

        private int ObtenerIdJugadorBD(int idJugadorTCP)
        {
            if (idsJugadoresBD.TryGetValue(
                idJugadorTCP,
                out int idJugadorBD))
            {
                return idJugadorBD;
            }

            Console.WriteLine("No se encontró el ID del jugador " + idJugadorTCP + " en la base de datos.");
            return 0;
        }

        private async Task RegistrarTurnoInicialAsync()
        {
            if (!partidaRegistradaAPI || partida.Juego == null)
                return;

            Jugador actual = partida.Juego.obtenerJugadorActual();

            if (actual == null)
                return;

            int idJugadorBD = ObtenerIdJugadorBD(actual.ID);

            if (idJugadorBD <= 0)
                return;

            bool registrado = await servicioAPI.RegistrarTurnoAsync(
                idPartidaBD,
                idJugadorBD,
                actual.Nombre);

            if (registrado)
            {
                ultimoJugadorTurnoRegistrado = actual.Nombre;

                Console.WriteLine("Turno inicial registrado: " + actual.Nombre);
            }
            else
            {
                Console.WriteLine("No se pudo registrar el turno inicial en la API.");
            }
        }

        private async Task RegistrarAccionEnAPIAsync(
            JugadorServidor jugador,
            Mensajes mensaje,
            string tipoAccion,
            Carta cartaJugada,
            string turnoAnterior)
        {
            if (!partidaRegistradaAPI || idPartidaBD <= 0)
                return;

            await bloqueoRegistroAPI.WaitAsync();

            try
            {
                int idJugadorBD = ObtenerIdJugadorBD(jugador.Id);

                if (idJugadorBD <= 0)
                    return;

                if (tipoAccion == "jugarcarta" && cartaJugada != null)
                {
                    bool jugadaRegistrada = await servicioAPI.RegistrarJugadaAsync(
                            idPartidaBD,
                            idJugadorBD,
                            jugador.Nombre,
                            cartaJugada.Color,
                            cartaJugada.Valor,
                            cartaJugada.Tipo);

                    if (jugadaRegistrada)
                    {
                        Console.WriteLine("Carta jugada registrada: " + cartaJugada.Valor);
                    }

                    if (cartaJugada.Tipo == "Salta" || cartaJugada.Tipo == "Reversa" ||
                        cartaJugada.Tipo == "+2" || cartaJugada.Tipo == "+4")
                    {
                        await servicioAPI.RegistrarAccionEspecialAsync(
                            idPartidaBD,
                            idJugadorBD,
                            jugador.Nombre,
                            cartaJugada.Tipo);
                    }

                    if ((cartaJugada.Tipo == "Comodin" || cartaJugada.Tipo == "+4") && !string.IsNullOrWhiteSpace(mensaje.ColorElegido))
                    {
                        await servicioAPI.RegistrarCambioColorAsync(
                            idPartidaBD,
                            idJugadorBD,
                            jugador.Nombre,
                            mensaje.ColorElegido);
                    }
                }

                await RegistrarCartasRobadasPorPenalizacionAsync();

                if ((tipoAccion == "robarcarta" || tipoAccion == "robar") &&
                    partida.Juego != null &&
                    partida.Juego.JugadorQueRobo != null &&
                    partida.Juego.CartaRobada != null &&
                    partida.Juego.JugadorQueRobo.ID == jugador.Id)
                {
                    Carta cartaRobada = partida.Juego.CartaRobada;

                    bool registrada = await servicioAPI.RegistrarCartaRobadaAsync(
                        idPartidaBD,
                        idJugadorBD,
                        jugador.Nombre,
                        cartaRobada.Color,
                        cartaRobada.Valor,
                        cartaRobada.Tipo);

                    if (registrada)
                    {
                        Console.WriteLine("Carta robada registrada: " + cartaRobada.Valor);
                    }
                }

                if (partida.Juego != null && !partida.Juego.partidaTerminada)
                {
                    Jugador actual = partida.Juego.obtenerJugadorActual();

                    if (actual != null && actual.Nombre != ultimoJugadorTurnoRegistrado)
                    {
                        int idActualBD = ObtenerIdJugadorBD(actual.ID);

                        if (idActualBD > 0)
                        {
                            bool registrado = await servicioAPI.RegistrarTurnoAsync(
                                idPartidaBD,
                                idActualBD,
                                actual.Nombre);

                            if (registrado)
                            {
                                ultimoJugadorTurnoRegistrado = actual.Nombre;
                            }
                        }
                    }
                }

                if (partida.Juego != null && partida.Juego.partidaTerminada && partida.Juego.Ganador != null && !resultadoFinalRegistrado)
                {
                    await RegistrarResultadoFinalAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error registrando la acción en la API: " + ex.Message);
            }
            finally
            {
                bloqueoRegistroAPI.Release();
            }
        }

        private async Task RegistrarCartasRobadasPorPenalizacionAsync()
        {
            if (partida.Juego == null)
                return;

            var cartasRobadas = partida.Juego.CartasRobadasPorPenalizacion;

            while (cantidadCartasPenalizacionRegistradas < cartasRobadas.Count)
            {
                var registro = cartasRobadas[cantidadCartasPenalizacionRegistradas];

                Jugador jugadorAfectado = registro.Item1;
                Carta cartaRobada = registro.Item2;

                int idJugadorBD = ObtenerIdJugadorBD(jugadorAfectado.ID);

                if (idJugadorBD <= 0)
                {
                    Console.WriteLine("No se pudo obtener el ID del jugador " + "que recibió la penalización.");
                    return;
                }

                bool registrado = await servicioAPI.RegistrarCartaRobadaAsync(
                        idPartidaBD,
                        idJugadorBD,
                        jugadorAfectado.Nombre,
                        cartaRobada.Color,
                        cartaRobada.Valor,
                        cartaRobada.Tipo);

                if (!registrado)
                {
                    Console.WriteLine("No se pudo registrar una carta de penalización en la API.");
                    return;
                }

                cantidadCartasPenalizacionRegistradas++;

                Console.WriteLine("Carta de penalización registrada: " + jugadorAfectado.Nombre + " recibió " + cartaRobada.Valor);
            }
        }

        private async Task RegistrarResultadoFinalAsync()
        {
            if (resultadoFinalRegistrado || !partidaRegistradaAPI || partida.Juego == null || partida.Juego.Ganador == null)
            {
                return;
            }

            List<ResultadoJugadorAPI> resultados = new List<ResultadoJugadorAPI>();

            foreach (Jugador jugador in partida.Juego.jugadores)
            {
                resultados.Add(
                    new ResultadoJugadorAPI
                    {
                        NombreJugador = jugador.Nombre,
                        CartasRestantes = jugador.Cartas.Count
                    });
            }

            bool registrado = await servicioAPI.FinalizarPartidaAsync(
                    idPartidaBD,
                    partida.Juego.Ganador.Nombre,
                    resultados);

            if (registrado)
            {
                resultadoFinalRegistrado = true;

                Console.WriteLine("Resultado final registrado en la API. Ganador: " + partida.Juego.Ganador.Nombre);
            }
            else
            {
                Console.WriteLine("No se pudo registrar el resultado final en la API.");
            }
        }

        private async Task EnviarRespuestaAsync(
            JugadorServidor jugador,
            string tipo,
            bool aceptado,
            string mensaje)
        {
            await jugador.EnviarAsync(
                new Mensajes
                {
                    Tipo = tipo,
                    JugadorId = jugador.Id,
                    Aceptado = aceptado,
                    Mensaje = mensaje
                });
        }

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
                        });
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
                    EstadoPartidaDto estado = partida.CrearEstadoPara(jugador.Id);

                    string tipo = partida.Iniciada ? "PartidaIniciada" : "EsperandoJugadores";

                    string mensaje = partida.Iniciada ? "Los tres jugadores están conectados. " + "¡Comienza la partida!" : "Esperando a los demás jugadores.";

                    await jugador.EnviarAsync(
                        new Mensajes
                        {
                            Tipo = tipo,
                            Aceptado = true,
                            Mensaje = mensaje,
                            EstadoPartida = estado
                        });
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
                            Mensaje = "Un jugador se desconectó. " + "La partida ha sido interrumpida."
                        });
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
                        Mensaje = "La sala ya tiene tres jugadores."
                    };

                    string json = JsonConvert.SerializeObject(respuesta);

                    await escritor.WriteLineAsync(json);
                }
            }
            catch (Exception)
            {
                //el cliente pudo desconectarse
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
    }
}