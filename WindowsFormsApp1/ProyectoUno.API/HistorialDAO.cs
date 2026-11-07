using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using MySql.Data.MySqlClient;
using ProyectoUno.API;

namespace WindowsFormsApp1
{
    public class ResultadoInicioPartida
    {
        public int IdPartida { get; set; }

        public List<int> IdsJugadores { get; set; } = new List<int>();
    }

    internal class HistorialDAO
    {
        private readonly JugadorDAO jugadorDAO = new JugadorDAO();

        // Crea un registro de nueva partida y devuelve su identificador.
        public int GuardarPartida()
        {
            using (var con = conexionBD.NuevaConexion())
            {
                con.Open();

                using (var cmd = new MySqlCommand(
                    "INSERT INTO Partida (Fecha, IdJugadorGanador) VALUES (@f, NULL); SELECT LAST_INSERT_ID();",
                    con))
                {
                    cmd.Parameters.AddWithValue("@f", DateTime.Now);
                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
        }

        // Registra el ganador de una partida.
        public void GuardarGanador(int idPartida, string nombreGanador)
        {
            int idGanador = jugadorDAO.ObtenerOCrear(nombreGanador);

            using (var con = conexionBD.NuevaConexion())
            {
                con.Open();

                using (var cmd = new MySqlCommand(
                    "UPDATE Partida SET IdJugadorGanador=@g WHERE IdPartida=@p",
                    con))
                {
                    cmd.Parameters.AddWithValue("@g", idGanador);
                    cmd.Parameters.AddWithValue("@p", idPartida);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // Relaciona los nombres de los jugadores con la partida.
        public void GuardarParticipantes(
            int idPartida,
            List<string> nombresJugadores)
        {
            using (var con = conexionBD.NuevaConexion())
            {
                con.Open();

                foreach (var nombre in nombresJugadores)
                {
                    int idJugador = jugadorDAO.ObtenerOCrear(nombre);

                    using (var cmd = new MySqlCommand(
                        @"INSERT INTO ResultadoPartida
                        (IdPartida, IdJugador, CartasRestantes, Gano)
                        VALUES (@p, @j, 0, FALSE);",
                        con))
                    {
                        cmd.Parameters.AddWithValue("@p", idPartida);
                        cmd.Parameters.AddWithValue("@j", idJugador);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        // Guarda el resultado del jugador y cuántas cartas le quedaron.
        public void GuardarResultado(
            int idPartida,
            string nombreJugador,
            int cartasRestantes)
        {
            int idJugador = jugadorDAO.ObtenerOCrear(nombreJugador);

            using (var con = conexionBD.NuevaConexion())
            {
                con.Open();

                using (var cmd = new MySqlCommand(
                    @"UPDATE ResultadoPartida
                    SET CartasRestantes=@c
                    WHERE IdPartida=@p AND IdJugador=@j",
                    con))
                {
                    cmd.Parameters.AddWithValue("@c", cartasRestantes);
                    cmd.Parameters.AddWithValue("@p", idPartida);
                    cmd.Parameters.AddWithValue("@j", idJugador);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // Actualiza el conteo de partidas ganadas y perdidas a partir del ganador.
        public void RegistrarGanadasPerdidas(
            int idPartida,
            string nombreGanador)
        {
            int idGanador = jugadorDAO.ObtenerOCrear(nombreGanador);

            using (var con = conexionBD.NuevaConexion())
            {
                con.Open();

                using (var cmd = new MySqlCommand(
                    @"UPDATE ResultadoPartida
                    SET Gano=TRUE
                    WHERE IdPartida=@p AND IdJugador=@j",
                    con))
                {
                    cmd.Parameters.AddWithValue("@p", idPartida);
                    cmd.Parameters.AddWithValue("@j", idGanador);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // Registra una partida y sus tres participantes en una única transacción.
        public ResultadoInicioPartida GuardarPartidaConParticipantes(
            List<string> nombresJugadores)
        {
            if (nombresJugadores == null || nombresJugadores.Count != 3)
            {
                throw new ArgumentException(
                    "La partida debe tener exactamente 3 jugadores.");
            }

            if (nombresJugadores.Exists(string.IsNullOrWhiteSpace))
            {
                throw new ArgumentException(
                    "Todos los jugadores deben tener un nombre.");
            }

            if (nombresJugadores
                .Select(n => n.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count() != 3)
            {
                throw new ArgumentException(
                    "No puede haber jugadores con nombres repetidos.");
            }

            using (var con = conexionBD.NuevaConexion())
            {
                con.Open();

                using (var transaccion = con.BeginTransaction())
                {
                    try
                    {
                        var resultado = new ResultadoInicioPartida();

                        // Obtener o crear los jugadores manteniendo el orden recibido.
                        foreach (string nombre in nombresJugadores)
                        {
                            int idJugador = jugadorDAO.ObtenerOCrear(
                                nombre.Trim(),
                                con,
                                transaccion);

                            resultado.IdsJugadores.Add(idJugador);
                        }

                        // Crear la partida.
                        using (var cmd = new MySqlCommand(
                            @"INSERT INTO Partida
                            (Fecha, IdJugadorGanador)
                            VALUES (@f, NULL);
                            SELECT LAST_INSERT_ID();",
                            con,
                            transaccion))
                        {
                            cmd.Parameters.AddWithValue("@f", DateTime.Now);

                            resultado.IdPartida =
                                Convert.ToInt32(cmd.ExecuteScalar());
                        }

                        // Registrar los tres participantes.
                        foreach (int idJugador in resultado.IdsJugadores)
                        {
                            using (var cmd = new MySqlCommand(
                                @"INSERT INTO ResultadoPartida
                                (IdPartida, IdJugador, CartasRestantes, Gano)
                                VALUES (@p, @j, 0, FALSE);",
                                con,
                                transaccion))
                            {
                                cmd.Parameters.AddWithValue(
                                    "@p", resultado.IdPartida);

                                cmd.Parameters.AddWithValue(
                                    "@j", idJugador);

                                cmd.ExecuteNonQuery();
                            }
                        }

                        transaccion.Commit();

                        return resultado;
                    }
                    catch
                    {
                        transaccion.Rollback();
                        throw;
                    }
                }
            }
        }

        // Consulta y devuelve los registros del historial en un DataTable.
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

        // Verifica si la partida ya fue finalizada.
        public bool PartidaFinalizada(int idPartida)
        {
            using (var con = conexionBD.NuevaConexion())
            {
                con.Open();

                using (var cmd = new MySqlCommand(
                    @"SELECT IdJugadorGanador
                    FROM Partida
                    WHERE IdPartida = @p",
                    con))
                {
                    cmd.Parameters.AddWithValue("@p", idPartida);

                    object resultado = cmd.ExecuteScalar();

                    return resultado != null && resultado != DBNull.Value;
                }
            }
        }

        // Verifica si un jugador pertenece a una partida.
        public bool JugadorPertenecePartida(int idPartida, int idJugador)
        {
            using (var con = conexionBD.NuevaConexion())
            {
                con.Open();

                using (var cmd = new MySqlCommand(
                    @"SELECT COUNT(*)
                    FROM ResultadoPartida
                    WHERE IdPartida = @p AND IdJugador = @j",
                    con))
                {
                    cmd.Parameters.AddWithValue("@p", idPartida);
                    cmd.Parameters.AddWithValue("@j", idJugador);

                    return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                }
            }
        }

        // Finaliza la partida y actualiza los resultados en una única transacción.
        public bool FinalizarPartida(
            int idPartida,
            int idJugadorGanador,
            Dictionary<int, int> resultados)
        {
            if (idPartida <= 0 ||
                idJugadorGanador <= 0 ||
                resultados == null ||
                resultados.Count != 3 ||
                !resultados.ContainsKey(idJugadorGanador))
            {
                throw new ArgumentException(
                    "Los datos de finalización son inválidos.");
            }

            if (resultados.Any(r => r.Key <= 0 || r.Value < 0))
            {
                throw new ArgumentException(
                    "Los resultados de los jugadores son inválidos.");
            }

            if (resultados[idJugadorGanador] != 0)
            {
                throw new ArgumentException(
                    "El ganador debe tener cero cartas restantes.");
            }

            using (var con = conexionBD.NuevaConexion())
            {
                con.Open();

                using (var transaccion = con.BeginTransaction())
                {
                    try
                    {
                        // Comprobar si la partida ya fue finalizada.
                        using (var cmd = new MySqlCommand(
                            @"SELECT IdJugadorGanador
                            FROM Partida
                            WHERE IdPartida = @p
                            FOR UPDATE",
                            con,
                            transaccion))
                        {
                            cmd.Parameters.AddWithValue("@p", idPartida);

                            object resultado = cmd.ExecuteScalar();

                            // Si la partida no existe.
                            if (resultado == null)
                            {
                                transaccion.Rollback();
                                return false;
                            }

                            // Si ya tiene ganador, no se puede finalizar otra vez.
                            if (resultado != DBNull.Value)
                            {
                                transaccion.Rollback();
                                return false;
                            }
                        }

                        // Registrar al ganador.
                        using (var cmd = new MySqlCommand(
                            @"UPDATE Partida
                            SET IdJugadorGanador = @g
                            WHERE IdPartida = @p
                            AND IdJugadorGanador IS NULL",
                            con,
                            transaccion))
                        {
                            cmd.Parameters.AddWithValue("@g", idJugadorGanador);
                            cmd.Parameters.AddWithValue("@p", idPartida);

                            if (cmd.ExecuteNonQuery() != 1)
                            {
                                transaccion.Rollback();
                                return false;
                            }
                        }

                        // Actualizar los resultados de los tres jugadores.
                        foreach (var jugador in resultados)
                        {
                            using (var cmd = new MySqlCommand(
                                @"UPDATE ResultadoPartida
                                SET CartasRestantes = @c,
                                    Gano = @g
                                WHERE IdPartida = @p
                                AND IdJugador = @j",
                                con,
                                transaccion))
                            {
                                cmd.Parameters.AddWithValue("@c", jugador.Value);
                                cmd.Parameters.AddWithValue(
                                    "@g", jugador.Key == idJugadorGanador);

                                cmd.Parameters.AddWithValue("@p", idPartida);
                                cmd.Parameters.AddWithValue("@j", jugador.Key);

                                if (cmd.ExecuteNonQuery() != 1)
                                {
                                    throw new InvalidOperationException(
                                        "No se encontró el resultado de uno de los jugadores.");
                                }
                            }
                        }

                        transaccion.Commit();
                        return true;
                    }
                    catch
                    {
                        if (transaccion.Connection != null)
                        {
                            transaccion.Rollback();
                        }

                        throw;
                    }
                }
            }
        }
    }
}