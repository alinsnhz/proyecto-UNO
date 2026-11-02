using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using ProyectoUno.Protocolo;

namespace ProyectoUno.Servidor
{
    public class ServidorSocket
    {
        private const int Puerto = 11000;
        private const int MaxJugadores = 3;

        private Socket listener;

        private ConcurrentDictionary<int, Socket> clientes = new ConcurrentDictionary<int, Socket>();

        private int siguienteId = 0;

        public async Task IniciarAsync()
        {
            listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            // Escuchar en todas las interfaces IPv4 disponibles.
            IPEndPoint punto = new IPEndPoint(IPAddress.Any, Puerto);

            listener.Bind(punto);
            listener.Listen(10);

            Console.WriteLine(" SERVIDOR UNO INICIADO");
            Console.WriteLine(" Puerto: " + Puerto);
            Console.WriteLine(" Esperando jugadores...");

            while (true)
            {
                Socket cliente = await listener.AcceptAsync();

                // Comprobar si todavía hay lugares disponibles.
                if (clientes.Count >= MaxJugadores)
                {
                    Console.WriteLine("Conexión rechazada: sala llena.");
                    try
                    {
                        byte[] mensaje = Encoding.UTF8.GetBytes("SERVIDOR_LLENO");
                        await cliente.SendAsync(new ArraySegment<byte>(mensaje), SocketFlags.None);
                    }
                    catch (SocketException)
                    {
                        // El cliente pudo desconectarse.
                    }
                    finally
                    {
                        cliente.Close();
                    }

                    continue;
                }

                int id = ++siguienteId;

                clientes.TryAdd(id, cliente);
                Console.WriteLine($"Jugador {id} conectado. " + $"Jugadores conectados: {clientes.Count}/{MaxJugadores}");

                // Atender a cada cliente sin bloquear las demás conexiones.
                _ = AtenderClienteAsync(id, cliente);
            }
        }

        private async Task AtenderClienteAsync(int id, Socket cliente)
        {
            using (NetworkStream flujo = new NetworkStream(cliente, false))
            using (StreamReader lector = new StreamReader(flujo, Encoding.UTF8, false, 1024, true))
            using (StreamWriter escritor = new StreamWriter(flujo, new UTF8Encoding(false), 1024, true))
            {
                escritor.AutoFlush = true;

                try
                {
                    while (true)
                    {
                        // Esperar un mensaje JSON completo.
                        string linea = await lector.ReadLineAsync();

                        // Si no hay más datos, el cliente se desconectó.
                        if (linea == null)
                            break;

                        if (string.IsNullOrWhiteSpace(linea))
                            continue;

                        Console.WriteLine($"Mensaje recibido del jugador {id}: {linea}");

                        Mensajes solicitud;

                        // Convertir el JSON en un objeto.
                        try
                        {
                            solicitud = JsonConvert.DeserializeObject<Mensajes>(linea);
                        }
                        catch (JsonException)
                        {
                            await EnviarRespuestaAsync(
                                escritor, null, false,
                                "JSON inválido.");
                            continue;
                        }

                        // Comprobar que el mensaje tenga un tipo.
                        if (solicitud == null ||
                            string.IsNullOrWhiteSpace(solicitud.Tipo))
                        {
                            await EnviarRespuestaAsync(
                                escritor,
                                solicitud?.IdSolicitud,
                                false,
                                "El mensaje no contiene un tipo válido.");
                            continue;
                        }

                        Console.WriteLine( $"Tipo de solicitud: {solicitud.Tipo}");

                jugador.Desconectar();

                lugares.Release();

                Console.WriteLine(
                    "Conexiones restantes: " +
                    clientes.Count + "/" + MaxJugadores
                );

                // Si la partida se interrumpió, avisar a los demás.
                if (partida.Interrumpida)
                {
                    await NotificarInterrupcionAsync();
                }
                else if (partida.CantidadJugadores > 0)
                {
                    await NotificarSalaAsync();
                }

                // Si la sala está vacía, puede comenzar de nuevo.
                if (partida.CantidadJugadores == 0)
                {
                    Console.WriteLine(
                        "Esperando una nueva partida..."
                    );
                }
            }
        }

        // Registra el nombre del jugador.
        private async Task ProcesarRegistroAsync(
            JugadorServidor jugador,
            Mensajes mensaje)
        {
            if (mensaje.Tipo == null ||
                !mensaje.Tipo.Equals(
                    "Conectar",
                    StringComparison.OrdinalIgnoreCase
                ))
            {
                await EnviarRespuestaAsync(
                    jugador,
                    "Error",
                    false,
                    "Debes enviar primero un mensaje de conexión."
                );

                return;
            }

                            case "Desconectar":
                                await EnviarRespuestaAsync(
                                    escritor,
                                    solicitud.IdSolicitud,
                                    true,
                                    "Desconexión recibida.");
                                return;

                            default:
                                await EnviarRespuestaAsync(
                                    escritor,
                                    solicitud.IdSolicitud,
                                    false,
                                    "Tipo de mensaje desconocido.");
                                break;
                        }
                    }
                }
                catch (IOException ex)
                {
                    Console.WriteLine($"Error de comunicación con el jugador {id}: " + ex.Message);
                }
                catch (SocketException ex)
                {
                    Console.WriteLine($"Error de socket con el jugador {id}: " + ex.Message);
                }
                catch (ObjectDisposedException)
                {
                    // La conexión ya fue cerrada.
                }
                finally
                {
                    clientes.TryRemove(id, out _);
                    cliente.Close();

                    Console.WriteLine($"Jugador {id} desconectado. " + $"Jugadores conectados: {clientes.Count}/3");
                }
            }
        }

        private async Task EnviarRespuestaAsync(StreamWriter escritor, string idSolicitud, bool aceptado, string mensaje)
        {
            Mensajes respuesta = new Mensajes
            {
                Tipo = "Respuesta",
                IdSolicitud = idSolicitud,
                Aceptado = aceptado,
                Mensaje = mensaje
            };

            string json = JsonConvert.SerializeObject(respuesta);

            // WriteLineAsync agrega el salto de línea que delimita el mensaje.
            await escritor.WriteLineAsync(json);
        }

    }
}
