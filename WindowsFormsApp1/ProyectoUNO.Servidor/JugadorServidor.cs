using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using ProyectoUno.Protocolo;

namespace ProyectoUno.Servidor
{
    // esta clase representa a cada computadora conectada
    // guarda el ID asigando por el servidor, nombre del jugador y medio para enviar mensajes
    public class JugadorServidor
    {
        public int Id { get; private set; }
        public string Nombre { get; set; }
        public bool Registrado { get; set; }

        private StreamWriter escritor;

        // SemaphoreSlim evita que dos mensajes intenten escribirse al mismo tiempo
        // en la conexión de un jugador
        private SemaphoreSlim bloqueo = new SemaphoreSlim(1, 1);

        public JugadorServidor(int id, StreamWriter escritor)
        {
            Id = id;
            this.escritor = escritor;
            Registrado = false;
        }

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
    }
}