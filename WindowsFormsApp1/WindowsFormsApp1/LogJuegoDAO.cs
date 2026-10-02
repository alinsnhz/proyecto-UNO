using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsFormsApp1
{
    public class LogJuegoDAO
    {
        private void InsertarEvento(int idPartida, int idJugador, string mensaje)
        {
            using (var con = ConexionBD.NuevaConexion())
            {
                con.Open();
                using (var cmd = new MySqlCommand(
                    "INSERT INTO logjuego (IdPartida, IdJugador, Fecha, Mensaje) VALUES (@p, @j, @f, @m);", con))
                {
                    cmd.Parameters.AddWithValue("@p", idPartida);
                    cmd.Parameters.AddWithValue("@j", idJugador);
                    cmd.Parameters.AddWithValue("@f", DateTime.Now);
                    cmd.Parameters.AddWithValue("@m", mensaje);
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
