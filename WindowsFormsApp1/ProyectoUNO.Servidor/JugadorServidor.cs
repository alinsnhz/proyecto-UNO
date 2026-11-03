
using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using ProyectoUno.Protocolo;

namespace ProyectoUno.Servidor
{
    // Representa a una computadora conectada al servidor.
    // esta clase representa a cada computadora conectada
    // guarda el ID asigando por el servidor, nombre del jugador y medio para enviar mensajes
    public class JugadorServidor
    {
        // identificador generado por el servidor
        public int Id { get; private set; }
        // nombre que registra el cliente
        public string Nombre { get; set; }
        public bool Registrado { get; set; }

        private readonly TcpClient cliente;
        private readonly StreamReader lector;
        private readonly StreamWriter escritor;

        // SemaphoreSlim evita que dos mensajes intenten escribirse al mismo tiempo
        // en la conexión de un jugador
        private readonly SemaphoreSlim bloqueo = new SemaphoreSlim(1, 1);

        public JugadorServidor(int id, TcpClient cliente)
        {
            Id = id;
            this.cliente = cliente;

            NetworkStream flujo = cliente.GetStream();

            lector = new StreamReader(flujo, new UTF8Encoding(false), false, 1024, true);

            escritor = new StreamWriter(flujo, new UTF8Encoding(false), 1024, true);

            escritor.AutoFlush = true;

            Registrado = false;
        }

        // Recibe un mensaje JSON completo.
        // Cada mensaje termina con un salto de línea.
        public async Task<string> RecibirAsync()
        {
            return await lector.ReadLineAsync();
        }

        // Envía un mensaje JSON al cliente.
        public async Task EnviarAsync(Mensajes mensaje)
        {
            string json = JsonConvert.SerializeObject(mensaje);

            await bloqueo.WaitAsync();

            try
            {
                await escritor.WriteLineAsync(json);
            }
            finally
            {
                bloqueo.Release();
            }
        }

        // Comprueba si la conexión continúa activa.
        public bool EstaConectado
        {
            get
            {
                return cliente != null && cliente.Connected;
            }
        }

        // Cierra la conexión y libera los recursos.
        public void Desconectar()
        {
            try
            {
                lector?.Dispose();
                escritor?.Dispose();
                cliente?.Close();
            }
            catch (Exception)
            {
                // La conexión pudo haberse cerrado anteriormente
            }
        }
    }
}
