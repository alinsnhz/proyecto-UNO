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

        public void GuardarGanador(int idPartida, string nombreGanador)
        {
            int idGanador = jugadorDAO.ObtenerOCrear(nombreGanador);

            using (var con = conexionBD.NuevaConexion())
            {
                con.Open();
                using (var cmd = new MySqlCommand(
                    "UPDATE Partida SET IdJugadorGanador=@g WHERE IdPartida=@p", con))
                {
                    cmd.Parameters.AddWithValue("@g", idGanador);
                    cmd.Parameters.AddWithValue("@p", idPartida);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void GuardarParticipantes(int idPartida, List<string> nombresJugadores)
        {
            using (var con = conexionBD.NuevaConexion())
            {
                con.Open();
                foreach (var nombre in nombresJugadores)
                {
                    int idJugador = jugadorDAO.ObtenerOCrear(nombre);
                    using (var cmd = new MySqlCommand(
                        "INSERT INTO ResultadoPartida (IdPartida, IdJugador, CartasRestantes, Gano) VALUES (@p, @j, 0, FALSE);", con))
                    {
                        cmd.Parameters.AddWithValue("@p", idPartida);
                        cmd.Parameters.AddWithValue("@j", idJugador);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        public void GuardarResultado(int idPartida, string nombreJugador, int cartasRestantes)
        {
            int idJugador = jugadorDAO.ObtenerOCrear(nombreJugador);

            using (var con = conexionBD.NuevaConexion())
            {
                con.Open();
                using (var cmd = new MySqlCommand(
                    "UPDATE ResultadoPartida SET CartasRestantes=@c WHERE IdPartida=@p AND IdJugador=@j", con))
                {
                    cmd.Parameters.AddWithValue("@c", cartasRestantes);
                    cmd.Parameters.AddWithValue("@p", idPartida);
                    cmd.Parameters.AddWithValue("@j", idJugador);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void RegistrarGanadasPerdidas(int idPartida, string nombreGanador)
        {
            int idGanador = jugadorDAO.ObtenerOCrear(nombreGanador);

            using (var con = conexionBD.NuevaConexion())
            {
                con.Open();

                using (var cmd = new MySqlCommand(
                    "UPDATE ResultadoPartida SET Gano=TRUE WHERE IdPartida=@p AND IdJugador=@j", con))
                {
                    cmd.Parameters.AddWithValue("@p", idPartida);
                    cmd.Parameters.AddWithValue("@j", idGanador);
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}