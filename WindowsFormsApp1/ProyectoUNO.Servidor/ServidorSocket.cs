using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

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

                Console.WriteLine(
                    $"Jugador {id} conectado. " +
                    $"Jugadores conectados: {clientes.Count}/{MaxJugadores}");

                // Atender a cada cliente sin bloquear las demás conexiones.
                _ = AtenderClienteAsync(id, cliente);
            }
        }

        private async Task AtenderClienteAsync(int id, Socket cliente)
        {
            byte[] buffer = new byte[1024];

            try
            {
                while (true)
                {
                    int recibidos = await cliente.ReceiveAsync(
                        new ArraySegment<byte>(buffer),
                        SocketFlags.None
                    );

                    // Cero bytes significa que el cliente cerró la conexión.
                    if (recibidos == 0)
                    {
                        break;
                    }

                    string mensaje = Encoding.UTF8.GetString(buffer, 0, recibidos);

                    Console.WriteLine($"Jugador {id} envió: {mensaje}");

                    // ACK temporal para probar el cliente actual.
                    byte[] respuesta = Encoding.UTF8.GetBytes("<|ACK|>");
                    int enviados = 0;
                    while (enviados < respuesta.Length)
                    {
                        int cantidad = await cliente.SendAsync(new ArraySegment<byte>(respuesta, enviados, respuesta.Length - enviados), SocketFlags.None);
                        if (cantidad == 0)
                        {
                            throw new SocketException();
                        }
                        enviados += cantidad;
                    }
                }
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"Error de conexión con el jugador {id}: " + ex.Message);
            }
            catch (ObjectDisposedException)
            {
                // El socket ya estaba cerrado.
            }
            finally
            {
                clientes.TryRemove(id, out _);

                try
                {
                    cliente.Close();
                }
                catch (SocketException)
                {
                    // La conexión ya pudo haberse cerrado.
                }

                Console.WriteLine($"Jugador {id} desconectado. " + $"Jugadores conectados: {clientes.Count}/{MaxJugadores}");
            }
        }
    }
}
