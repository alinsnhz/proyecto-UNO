using System;
using System.Threading.Tasks;

namespace ProyectoUno.Servidor
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            ServidorSocket servidor = new ServidorSocket();

            try
            {
                await servidor.IniciarAsync();
            }
            catch (Exception ex)
        {
                Console.WriteLine(
                    "Error al iniciar el servidor: " + ex.Message
                );
            }
        }
    }
}
