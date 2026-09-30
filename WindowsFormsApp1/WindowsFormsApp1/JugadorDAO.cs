using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

namespace WindowsFormsApp1
{
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
        public int Crear(string nombre)
        {
            using (var con = ConexionBD.NuevaConexion())
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

        public bool Actualizar(int id, string nuevoNombre)
        {
            using (var con = ConexionBD.NuevaConexion())
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

        public bool Eliminar(int id)
        {
            using (var con = ConexionBD.NuevaConexion())
            {
                con.Open();
                using (var cmd = new MySqlCommand("DELETE FROM Jugador WHERE IdJugador=@id", con))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        public JugadorRegistro ObtenerPorNombre(string nombre)
        {
            using (var con = ConexionBD.NuevaConexion())
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

        
    }
}
