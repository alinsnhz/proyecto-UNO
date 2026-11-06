using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace WindowsFormsApp1
{
    // Mantiene la conexión TCP con el servidor UNO.
    public class ClienteSocket
    {
        private TcpClient _client;
        private NetworkStream _stream;
        private CancellationTokenSource _cts;
        private bool _isConnected;
        private readonly SemaphoreSlim _bloqueoEnvio = new SemaphoreSlim(1, 1);

        public event Action<string> OnMessageReceived;
        public event Action<string> OnDisconnected;

        public bool IsConnected => _isConnected;

        public async Task<bool> ConectarAsync(string ipServidor, string nombreJugador)
        {
            try
            {
                _client = new TcpClient();
                await _client.ConnectAsync(ipServidor, 11000);
                _stream = _client.GetStream();
                _cts = new CancellationTokenSource();
                _isConnected = true;

                // El servidor espera un objeto Mensajes serializado como JSON.
                await EnviarObjetoAsync(new
                {
                    tipo = "Conectar",
                    jugadorNombre = nombreJugador
                });

                _ = Task.Run(() => RecibirMensajesLoopAsync(_cts.Token));
                return true;
            }
            catch
            {
                Desconectar();
                return false;
            }
        }

        // Envía una acción usando los nombres de propiedades del protocolo del servidor.
        public Task EnviarAccionAsync(string tipo, string cartaId = null,
            string colorElegido = null, int? jugadorObjetivoId = null)
        {
            return EnviarObjetoAsync(new
            {
                tipo = tipo,
                cartaId = cartaId,
                colorElegido = colorElegido,
                jugadorObjetivoId = jugadorObjetivoId
            });
        }

        public async Task EnviarObjetoAsync(object mensaje)
        {
            if (!_isConnected || _stream == null)
                return;

            string json = JsonConvert.SerializeObject(mensaje);
            byte[] datos = Encoding.UTF8.GetBytes(json + "\n");

            await _bloqueoEnvio.WaitAsync();
            try
            {
                if (_isConnected && _stream != null)
                    await _stream.WriteAsync(datos, 0, datos.Length);
            }
            catch
            {
                Desconectar();
                OnDisconnected?.Invoke("Se perdió la conexión con el servidor al enviar datos.");
            }
            finally
            {
                _bloqueoEnvio.Release();
            }
        }

        // Se conserva este método para llamadas antiguas que envíen JSON directamente.
        public async Task EnviarMensajeAsync(string mensaje)
        {
            if (string.IsNullOrWhiteSpace(mensaje)) return;
            byte[] datos = Encoding.UTF8.GetBytes(mensaje.TrimEnd('\r', '\n') + "\n");
            if (!_isConnected || _stream == null) return;

            await _bloqueoEnvio.WaitAsync();
            try
            {
                if (_isConnected && _stream != null)
                    await _stream.WriteAsync(datos, 0, datos.Length);
            }
            catch
            {
                Desconectar();
                OnDisconnected?.Invoke("Se perdió la conexión con el servidor al enviar datos.");
            }
            finally
            {
                _bloqueoEnvio.Release();
            }
        }

        private async Task RecibirMensajesLoopAsync(CancellationToken cancellationToken)
        {
            try
            {
                using (StreamReader lector = new StreamReader(_stream, Encoding.UTF8, false, 1024, true))
                {
                    while (_isConnected && !cancellationToken.IsCancellationRequested)
                    {
                        string linea = await lector.ReadLineAsync();
                        if (linea == null) break;
                        if (!string.IsNullOrWhiteSpace(linea))
                            OnMessageReceived?.Invoke(linea);
                    }
                }
            }
            catch
            {
                // La conexión pudo cerrarse o cancelarse mientras se recibían datos.
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

        public void Desconectar()
        {
            if (!_isConnected && _client == null) return;
            _isConnected = false;
            try { _cts?.Cancel(); } catch { }
            try { _stream?.Close(); } catch { }
            try { _client?.Close(); } catch { }
            _stream = null;
            _client = null;
        }
    }
}
