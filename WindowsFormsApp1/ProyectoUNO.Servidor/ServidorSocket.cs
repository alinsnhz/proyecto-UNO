using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using ProyectoUno.Protocolo;

namespace ProyectoUno.Servidor
{
    public class ServidorSocket
    {
        private const int Puerto = 11000;
        private const int MaxJugadores = 3;

        private TcpListener listener;

        private readonly PartidaServidor partida =
            new PartidaServidor();

        private readonly ConcurrentDictionary<int, JugadorServidor>
            clientes =
            new ConcurrentDictionary<int, JugadorServidor>();

        private readonly SemaphoreSlim lugares =
            new SemaphoreSlim(MaxJugadores, MaxJugadores);

        private int siguienteId = 0;

        private volatile bool detenido = false;

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

                    _ = AtenderClienteAsync(jugador);
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

                    // El primer mensaje debe registrar al jugador
                    if (!jugador.Registrado)
                    {
                        await ProcesarRegistroAsync(jugador, mensaje);

                        continue;
                    }

                    // El servidor utiliza el ID de la conexión
                    Mensajes respuesta = partida.ProcesarAccion(jugador.Id, mensaje);

                    respuesta.JugadorId = jugador.Id;

                    await jugador.EnviarAsync(respuesta);

                    // Si la acción fue aceptada, actualizar a todos
                    if (respuesta.Aceptado == true)
                    {
                        await NotificarEstadoPartidaAsync();
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
                    Console.WriteLine(
                        "Esperando una nueva partida..."
                    );
                }
            }
        }

        private async Task ProcesarRegistroAsync(
            JugadorServidor jugador,
            Mensajes mensaje)
        {
            if (mensaje.Tipo == null ||
                !mensaje.Tipo.Equals(
                    "Conectar",
                    StringComparison.OrdinalIgnoreCase))
            {
                await EnviarRespuestaAsync(
                    jugador,
                    "Error",
                    false,
                    "Debes enviar primero un mensaje de conexión."
                );

                return;
            }

            if (string.IsNullOrWhiteSpace(mensaje.JugadorNombre))
            {
                await EnviarRespuestaAsync(
                    jugador,
                    "Error",
                    false,
                    "Debes proporcionar un nombre de jugador."
                );

                return;
            }

            jugador.Nombre = mensaje.JugadorNombre.Trim();

            bool registrado =
                partida.RegistrarJugador(jugador);

            if (!registrado)
            {
                await EnviarRespuestaAsync(
                    jugador,
                    "Error",
                    false,
                    "No fue posible registrar al jugador. " +
                    "La sala puede estar llena o la partida interrumpida."
                );

                return;
            }

            Console.WriteLine(
                "Jugador " + jugador.Id +
                " registrado con nombre: " + jugador.Nombre
            );

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
                                "Un jugador se desconectó. " +
                                "La partida ha sido interrumpida."
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
    }
}
