using System;
using System.Data;
using MySql.Data.MySqlClient;

namespace WindowsFormsApp1
{
    public class LogJuegoDAO
    {
        private void InsertarEvento(int idPartida, int idJugador, string mensaje)
        {
            using (var con = conexionBD.NuevaConexion())
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

        public void RegistrarCartaJugada(int idPartida, int idJugador, string nombreJugador, string carta)
        {
            InsertarEvento(idPartida, idJugador, $"{nombreJugador} jugó: {carta}");
        }

        public void RegistrarCartaRobada(int idPartida, int idJugador, string nombreJugador, string carta)
        {
            InsertarEvento(idPartida, idJugador, $"{nombreJugador} robó una carta: {carta}");
        }

        public void RegistrarCambioColor(int idPartida, int idJugador, string nombreJugador, string nuevoColor)
        {
            InsertarEvento(idPartida, idJugador, $"{nombreJugador} cambió el color a: {nuevoColor}");
        }

        public void RegistrarTurno(int idPartida, int idJugador, string nombreJugador)
        {
            InsertarEvento(idPartida, idJugador, $"Comienza el turno de {nombreJugador}");
        }

        public void RegistrarAccionEspecial(int idPartida, int idJugador, string nombreJugador, string tipoAccion)
        {
            string mensaje;
            switch (tipoAccion)
            {
                case "Salto":
                    mensaje = $"{nombreJugador} jugó Salto: el siguiente jugador pierde su turno";
                    break;
                case "Reversa":
                    mensaje = $"{nombreJugador} jugó Reversa: cambia el sentido del juego";
                    break;
                case "+2":
                    mensaje = $"{nombreJugador} jugó +2: el siguiente jugador roba 2 cartas";
                    break;
                case "+4":
                    mensaje = $"{nombreJugador} jugó Comodín +4: el siguiente jugador roba 4 cartas";
                    break;
                default:
                    mensaje = $"{nombreJugador} jugó una carta especial: {tipoAccion}";
                    break;
            }
            InsertarEvento(idPartida, idJugador, mensaje);
        }

        public DataTable ObtenerLogDePartida(int idPartida)
        {
            using (var con = conexionBD.NuevaConexion())
            {
                con.Open();
                string sql = @"
                    SELECT l.Fecha, j.Nombre AS Jugador, l.Mensaje
                    FROM logjuego l
                    LEFT JOIN jugador j ON l.IdJugador = j.IdJugador
                    WHERE l.IdPartida = @p
                    ORDER BY l.Fecha ASC;";

                using (var cmd = new MySqlCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("@p", idPartida);
                    using (var adaptador = new MySqlDataAdapter(cmd))
                    {
                        var tabla = new DataTable();
                        adaptador.Fill(tabla);
                        return tabla;
                    }
                }
            }
        }
    }
}
