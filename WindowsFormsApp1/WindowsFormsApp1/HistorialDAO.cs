using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace WindowsFormsApp1
{
    /*
     * Guarda y consulta partidas, participantes, resultados y estadísticas. 
     * Existe en la solución de interfaz y en la API; revisa cuál versión se usa en cada proyecto
    */
    internal class HistorialDAO
    {
        private readonly JugadorDAO jugadorDAO = new JugadorDAO();

        //crea un registro de nueva partida y devuelve su identificador
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

        //registra el ganador de una partida
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

        //relaciona los nombres de los jugadores con la partida
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

        //guarda el resultado del jugador y cuántas cartas le quedaron
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

        //actualiza el conteo de partidas ganadas y perdidas a partir del ganador
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

        //consulta y devuelve los registros del historial en un DataTable
        public DataTable ConsultarHistorial()
        {
            using (var con = conexionBD.NuevaConexion())
            {
                con.Open();
                string sql = @"
                    SELECT p.IdPartida AS Partida, p.Fecha, j.Nombre AS Ganador
                    FROM Partida p
                    LEFT JOIN Jugador j ON p.IdJugadorGanador = j.IdJugador
                    ORDER BY p.Fecha DESC;";

                using (var cmd = new MySqlCommand(sql, con))
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