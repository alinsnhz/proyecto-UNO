using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WindowsFormsApp1
{
    // Clase responsable de conectar cada cliente con el servidor TCP y mantener la comunicación durante toda la partida.
    public class ClienteSocket
    {
        private TcpClient _client;
        private NetworkStream _stream;
        private CancellationTokenSource _cts;
        private bool _isConnected;

        // Eventos para notificar a la interfaz de usuario (InicioForm) de forma desacoplada
        public event Action<string> OnMessageReceived;
        public event Action<string> OnDisconnected;

        // Propiedad de solo lectura para verificar si el cliente se encuentra conectado activamente
        public bool IsConnected => _isConnected;

        // Conecta con la IP del servidor y el puerto 11000 de forma asíncrona,
        // envía el nombre del jugador y arranca la escucha continua.
        public async Task<bool> ConectarAsync(string ipServidor, string nombreJugador)
        {
            try
            {
                _client = new TcpClient();

                // Conexión con la IP del servidor y el puerto 11000 especificado
                await _client.ConnectAsync(ipServidor, 11000);

                _stream = _client.GetStream();
                _isConnected = true;
                _cts = new CancellationTokenSource();

                // Enviar el nombre del jugador inmediatamente al conectarse mediante el protocolo acordado
                await EnviarMensajeAsync($"CONNECT|{nombreJugador}");

                // Iniciar la tarea de recepción continua de mensajes en segundo plano (sin bloquear la interfaz)
                _ = Task.Run(() => RecibirMensajesLoopAsync(_cts.Token));

                return true;
            }
            catch (Exception)
            {
                _isConnected = false;
                return false;
            }
        }

        // Envía mensajes o solicitudes al servidor utilizando el protocolo acordado.
        public async Task EnviarMensajeAsync(string mensaje)
        {
            if (!_isConnected || _stream == null) return;

            try
            {
                byte[] data = Encoding.UTF8.GetBytes(mensaje + "\n");
                await _stream.WriteAsync(data, 0, data.Length);
            }
            catch (Exception)
            {
                Desconectar();
                OnDisconnected?.Invoke("Se perdió la conexión con el servidor al intentar enviar datos.");
            }
        }

        // Bucle continuo que se ejecuta en segundo plano para recibir datos del servidor
        // de forma asíncrona, evitando congelar el hilo principal de Windows Forms.
        private async Task RecibirMensajesLoopAsync(CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[4096];
            try
            {
                while (_isConnected && !cancellationToken.IsCancellationRequested)
                {
                    int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);

                    if (bytesRead == 0) break; // El servidor cerró la conexión ordenadamente

                    string mensaje = Encoding.UTF8.GetString(buffer, 0, bytesRead).TrimEnd('\n', '\r');

                    // Notificar a la interfaz el mensaje recibido (incluye confirmaciones o identificadores)
                    OnMessageReceived?.Invoke(mensaje);
                }
            }
            catch (Exception)
            {
                // Excepción generada por cancelación de token o interrupción de red
            }
            finally
            {
                if (_isConnected)
                {
                    Desconectar();
                    // Notificar desconexión imprevista a la interfaz
                    OnDisconnected?.Invoke("Se ha perdido la conexión con el servidor.");
                }
            }
        }

        // Método para desconectarse correctamente del servidor y liberar los recursos de red.
        public void Desconectar()
        {
            if (!_isConnected) return;

            _isConnected = false;
            try
            {
                _cts?.Cancel();
                _stream?.Close();
                _client?.Close();
            }
            catch (Exception)
            {
                // Ignorar excepciones menores al liberar sockets cerrados
            }
        }
    }
}