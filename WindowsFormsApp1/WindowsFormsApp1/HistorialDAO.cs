using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace WindowsFormsApp1
{
    internal class HistorialDAO
    {
        private readonly JugadorDAO jugadorDAO = new JugadorDAO();

        public int GuardarPartida()
        {
            using (var con = conexionBD.NuevaConexion())
            {
                con.Open();
                using (var cmd = new MySqlCommand(
                    "INSERT INTO Partida (Fecha, IdJugadorGanador) VALUES (@f, NULL); SELECT LAST_INSERT_ID();", con))
                {
                    cmd.Parameters.AddWithValue("@f", DateTime.Now);
                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
        }
    }
}