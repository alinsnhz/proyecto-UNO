using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public class ClienteSocket
{
    private TcpClient _client;
    private NetworkStream _stream;
    private CancellationTokenSource _cts;
    private bool _isConnected;

    // Eventos para notificar a la interfaz (InicioForms) de forma desacoplada
    public event Action<string> OnMessageReceived;
    public event Action<string> OnDisconnected;

    public bool IsConnected => _isConnected;

    // Conecta con la IP del servidor en el puerto 11000 y envía el nombre del jugador.
    
    public async Task<bool> ConectarAsync(string ipServidor, string nombreJugador)
    {
        try
        {
            _client = new TcpClient();

            // Intento de conexión asíncrona al puerto 11000
            await _client.ConnectAsync(ipServidor, 11000);

            _stream = _client.GetStream();
            _isConnected = true;
            _cts = new CancellationTokenSource();

            // Enviar el nombre del jugador inmediatamente al conectarse (según protocolo)
            await EnviarMensajeAsync($"CONNECT|{nombreJugador}");

            // Iniciar la tarea de escucha continua en segundo plano sin bloquear el hilo principal
            _ = Task.Run(() => RecibirMensajesLoopAsync(_cts.Token));

            return true;
        }
        catch (Exception)
        {
            _isConnected = false;
            return false;
        }
    }

    // Envia un mensaje o solicitud al servidor utilizando el protocolo acordado.
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

    // Bucle continuo que escucha mensajes del servidor sin bloquear la interfaz.
    private async Task RecibirMensajesLoopAsync(CancellationToken cancellationToken)
    {
        byte[] buffer = new byte[4096];
        try
        {
            while (_isConnected && !cancellationToken.IsCancellationRequested)
            {
                int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);

                if (bytesRead == 0)
                {
                    // El servidor cerro la conexión ordenadamente
                    break;
                }

                string mensaje = Encoding.UTF8.GetString(buffer, 0, bytesRead).TrimEnd('\n', '\r');

                // Disparar evento de mensaje recibido
                OnMessageReceived?.Invoke(mensaje);
            }
        }
        catch (Exception)
        {
            // Excepcion por cancelacion o corte de la red
        }
        finally
        {
            if (_isConnected)
            {
                Desconectar();
                OnDisconnected?.Invoke("Se ha perdido la conexión con el servidor.");
            }
        }
    }

    // Metodo para desconectarse correctamente y liberar recursos
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
            // Ignorar errores menores al cerrar sockets
        }
    }
}