using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using ProyectoUno.Protocolo;

namespace ProyectoUno.Servidor
{
    // representa a cada computadora conectada. Guarda el ID asignado por el servidor,
    // el nombre del jugador y el medio para enviarle mensajes.
    public class JugadorServidor
    {
        public int Id { get; private set; }
        public string Nombre { get; set; }
        public bool Registrado { get; set; }

        private StreamWriter escritor;
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