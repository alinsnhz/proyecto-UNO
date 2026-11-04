using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using ProyectoUno.Protocolo;

namespace ProyectoUno.Servidor
{
    // Clase que conecta todo: recibe las conexiones, asigna IDs, registra nombres y gestiona la sala.
    public class ServidorSocket
    {
        private const int Puerto = 11000;
        private const int MaxJugadores = 3;

        private TcpListener listener;

        private readonly PartidaServidor partida = new PartidaServidor();

        // Guarda todas las conexiones aceptadas.
        private readonly ConcurrentDictionary<int, JugadorServidor> clientes =
            new ConcurrentDictionary<int, JugadorServidor>();

        // Controla cuántas conexiones ocupan un lugar.
        private readonly SemaphoreSlim lugares = new SemaphoreSlim(MaxJugadores, MaxJugadores);

        private int siguienteId = 0;

        private volatile bool detenido = false;

        public async Task IniciarAsync()
        {
            listener = new TcpListener(IPAddress.Any, Puerto);
            listener.Start();

            Console.WriteLine("SERVIDOR UNO INICIADO");
            Console.WriteLine("Puerto: " + Puerto);
            Console.WriteLine("Esperando jugadores...");

            try
            {
                while (!detenido)
                {
                    TcpClient cliente = await listener.AcceptTcpClientAsync();

                    // Reservar un lugar antes de aceptar al jugador.
                    if (!await lugares.WaitAsync(0))
                    {
                        Console.WriteLine("Conexión rechazada: sala llena.");
                        await RechazarConexionAsync(cliente);
                        continue;
                    }

                    int id = Interlocked.Increment(ref siguienteId);

                    JugadorServidor jugador = new JugadorServidor(id, cliente);

                    clientes.TryAdd(id, jugador);

                    Console.WriteLine("Nueva conexión. ID asignado: " + id);
                    Console.WriteLine("Conexiones: " + clientes.Count + "/" + MaxJugadores);

                    // Atender a cada cliente de manera independiente.
                    _ = AtenderClienteAsync(jugador);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error en el listener del servidor: " + ex.Message);
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
                        await EnviarRespuestaPersonalizadaAsync(jugador, "Error", false, "El mensaje JSON no es válido");
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

                    // Procesar acciones de juego si ya está iniciado
                    Mensajes respuesta = partida.ProcesarAccion(jugador.Id, mensaje);
                    if (respuesta != null)
                    {
                        await jugador.EnviarAsync(respuesta);
                    }

                    // Si la partida se actualizó, notificar a la sala
                    if (partida.Iniciada)
                    {
                        await NotificarSalaAsync();
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
                }
            }
        }

        // Registra el nombre del jugador.
        private async Task ProcesarRegistroAsync(JugadorServidor jugador, Mensajes mensaje)
        {
            if (mensaje.Tipo == null || !mensaje.Tipo.Equals("Conectar", StringComparison.OrdinalIgnoreCase))
            {
                await EnviarRespuestaPersonalizadaAsync(jugador, "Error", false, "Debes enviar primero un mensaje de conexión.");
                return;
            }

            if (string.IsNullOrWhiteSpace(mensaje.JugadorNombre))
            {
                await EnviarRespuestaPersonalizadaAsync(jugador, "Error", false, "Debes proporcionar un nombre de jugador.");
                return;
            }

            jugador.Nombre = mensaje.JugadorNombre.Trim();

            bool registrado = partida.RegistrarJugador(jugador);

            if (!registrado)
            {
                await EnviarRespuestaPersonalizadaAsync(jugador, "Error", false, "No fue posible registrar al jugador.");
                return;
            }

            Console.WriteLine("Jugador " + jugador.Id + " registrado con nombre: " + jugador.Nombre);

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

        // Envía una respuesta personalizada a un jugador mediante su objeto JugadorServidor.
        private async Task EnviarRespuestaPersonalizadaAsync(
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

        // Envía el estado de la sala a los clientes.
        private async Task NotificarSalaAsync()
        {
            var listaJugadores = partida.ObtenerJugadores();

            foreach (JugadorServidor jugador in listaJugadores)
            {
                try
                {
                    EstadoPartidaDto estadoDto = partida.CrearEstadoPara(jugador.Id);

                    string tipoMensaje = partida.Iniciada ? "PartidaIniciada" : "EsperandoJugadores";
                    string textoMensaje = partida.Iniciada
                        ? "Los tres jugadores están conectados. ¡Comienza la partida!"
                        : "Esperando a los demás jugadores.";

                    await jugador.EnviarAsync(
                        new Mensajes
                        {
                            Tipo = tipoMensaje,
                            Aceptado = true,
                            Mensaje = textoMensaje,
                            EstadoPartida = estadoDto
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
            var listaJugadores = partida.ObtenerJugadores();

            foreach (JugadorServidor jugador in listaJugadores)
            {
                try
                {
                    await jugador.EnviarAsync(
                        new Mensajes
                        {
                            Tipo = "PartidaInterrumpida",
                            Aceptado = false,
                            Mensaje = "Un jugador se desconectó. La partida ha sido interrumpida"
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
                NetworkStream flujo = cliente.GetStream();

                using (var escritor = new StreamWriter(flujo, new UTF8Encoding(false), 1024, true))
                {
                    escritor.AutoFlush = true;

                    Mensajes respuesta = new Mensajes
                    {
                        Tipo = "ServidorLleno",
                        Aceptado = false,
                        Mensaje = "La sala ya tiene tres jugadores"
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