using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

namespace WindowsFormsApp1
{
    /*
     * Contiene operaciones de persistencia para los jugadores registrados en la base de datos
    */
    public class JugadorRegistro
    {
        public int IdJugador { get; set; }
        public string Nombre { get; set; }
        public override string ToString() => $"[{IdJugador}] {Nombre}";
    }

    public class EstadisticasJugador
    {
        public string Nombre { get; set; }
        public int PartidasJugadas { get; set; }
        public int PartidasGanadas { get; set; }
        public double PromedioCartasRestantes { get; set; }
    }

    public class JugadorDAO
    {
        //inserta un jugador y devuelve su identificador según la implementación
        public int Crear(string nombre)
        {
            using (var con = conexionBD.NuevaConexion())
            {
                con.Open();
                using (var cmd = new MySqlCommand(
                    "INSERT INTO Jugador (Nombre) VALUES (@n); SELECT LAST_INSERT_ID();", con))
                {
                    cmd.Parameters.AddWithValue("@n", nombre);
                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
        }

        //cambia el nombre del jugador indicado y devuelve si la operación tuvo éxito
        public bool Actualizar(int id, string nuevoNombre)
        {
            using (var con = conexionBD.NuevaConexion())
            {
                con.Open();
                using (var cmd = new MySqlCommand("UPDATE Jugador SET Nombre=@n WHERE IdJugador=@id", con))
                {
                    cmd.Parameters.AddWithValue("@n", nuevoNombre);
                    cmd.Parameters.AddWithValue("@id", id);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        //elimina el registro del jugador indicado y devuelve el resultado
        public bool Eliminar(int id)
        {
            using (var con = conexionBD.NuevaConexion())
            {
                con.Open();
                using (var cmd = new MySqlCommand("DELETE FROM Jugador WHERE IdJugador=@id", con))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        //busca un jugador por su nombre y devuelve su registro si existe
        public JugadorRegistro ObtenerPorNombre(string nombre)
        {
            using (var con = conexionBD.NuevaConexion())
            {
                con.Open();
                using (var cmd = new MySqlCommand("SELECT IdJugador, Nombre FROM Jugador WHERE Nombre=@n", con))
                {
                    cmd.Parameters.AddWithValue("@n", nombre);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                            return new JugadorRegistro { IdJugador = reader.GetInt32(0), Nombre = reader.GetString(1) };
                        return null;
                    }
                }
            }
        }

        //busca el jugador por nombre; si no existe, lo crea y devuelve su identificador
        public int ObtenerOCrear(string nombre)
        {
            var existente = ObtenerPorNombre(nombre);
            return existente != null ? existente.IdJugador : Crear(nombre);
        }

        //consulta y devuelve la lista de jugadores registrados
        public List<JugadorRegistro> ObtenerTodos()
        {
            var lista = new List<JugadorRegistro>();
            using (var con = conexionBD.NuevaConexion())
            {
                con.Open();
                using (var cmd = new MySqlCommand("SELECT IdJugador, Nombre FROM Jugador ORDER BY Nombre", con))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                        lista.Add(new JugadorRegistro { IdJugador = reader.GetInt32(0), Nombre = reader.GetString(1) });
                }
            }
            return lista;
        }

        //busca un jugador por su identificador
        public JugadorRegistro ObtenerPorId(int id)
        {
            using (var con = conexionBD.NuevaConexion())
            {
                con.Open();
                using (var cmd = new MySqlCommand("SELECT IdJugador, Nombre FROM Jugador WHERE IdJugador=@id", con))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                            return new JugadorRegistro { IdJugador = reader.GetInt32(0), Nombre = reader.GetString(1) };
                        return null;
                    }
                }
            }
        }

        //consulta las estadísticas asociadas al jugador indicado
        public EstadisticasJugador ObtenerEstadisticas(int idJugador)
        {
            var stats = new EstadisticasJugador();

            var jugador = ObtenerPorId(idJugador);
            stats.Nombre = jugador != null ? jugador.Nombre : "(desconocido)";

            using (var con = conexionBD.NuevaConexion())
            {
                con.Open();

                using (var cmd = new MySqlCommand(
                    "SELECT COUNT(*) FROM ResultadoPartida WHERE IdJugador=@id", con))
                {
                    cmd.Parameters.AddWithValue("@id", idJugador);
                    stats.PartidasJugadas = Convert.ToInt32(cmd.ExecuteScalar());
                }

                using (var cmd = new MySqlCommand(
                    "SELECT COUNT(*) FROM Partida WHERE IdJugadorGanador=@id", con))
                {
                    cmd.Parameters.AddWithValue("@id", idJugador);
                    stats.PartidasGanadas = Convert.ToInt32(cmd.ExecuteScalar());
                }

                using (var cmd = new MySqlCommand(
                    "SELECT AVG(CartasRestantes) FROM ResultadoPartida WHERE IdJugador=@id", con))
                {
                    cmd.Parameters.AddWithValue("@id", idJugador);
                    var resultado = cmd.ExecuteScalar();
                    stats.PromedioCartasRestantes = (resultado == DBNull.Value || resultado == null)
                        ? 0
                        : Convert.ToDouble(resultado);
                }
            }

            return stats;
        }
    }
}
