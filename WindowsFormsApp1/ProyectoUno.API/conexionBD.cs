using System;
using System.Data;
using MySql.Data.MySqlClient;

namespace ProyectoUno.API
{
    public static class conexionBD
    {
        private static string ObtenerCadenaConexion()
        {
            string? cadena = Environment.GetEnvironmentVariable("UNO_DB_CONNECTION");

            if (string.IsNullOrWhiteSpace(cadena))
            {
                throw new InvalidOperationException(
                    "No se configuró la variable de entorno UNO_DB_CONNECTION.");
            }

            return cadena;
        }

        public static MySqlConnection NuevaConexion()
        {
            return new MySqlConnection(ObtenerCadenaConexion());
        }

        public static bool ProbarConexion(out string mensajeError)
        {
            mensajeError = "";
            try
            {
                using (var con = NuevaConexion())
                {
                    con.Open();
                    return true;
                }
            }
            catch (MySqlException ex)
            {
                mensajeError = ObtenerMensajeError(ex);
                return false;
            }
            catch (Exception ex)
            {
                mensajeError = "Error inesperado: " + ex.Message;
                return false;
            }
        }

        private static string ObtenerMensajeError(MySqlException ex)
        {
            switch (ex.Number)
            {
                case 0:
                    return "No se pudo conectar al servidor MySQL";
                case 1042:
                    return "No se encuentra el servidor (host/puerto incorrectos).";
                case 1045:
                    return "Usuario o contraseña incorrectos.";
                case 1049:
                    return "La base de datos 'uno' no existe. Corre primero schema_uno_mysql.sql.";
                default:
                    return $"Error de MySQL ({ex.Number}): {ex.Message}";
            }
        }

        public static int EjecutarNonQuery(string sql, params MySqlParameter[] parametros)
        {
            try
            {
                using (var con = NuevaConexion())
                {
                    con.Open();
                    using (var cmd = new MySqlCommand(sql, con))
                    {
                        if (parametros != null)
                        {
                            cmd.Parameters.AddRange(parametros);
                        }
                        return cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (MySqlException ex)
            {
                Console.WriteLine(ObtenerMensajeError(ex));
                return -1;
            }
        }

        public static object? EjecutarEscalar(string sql, params MySqlParameter[] parametros)
        {
            try
            {
                using (var con = NuevaConexion())
                {
                    con.Open();
                    using (var cmd = new MySqlCommand(sql, con))
                    {
                        if (parametros != null)
                        {
                            cmd.Parameters.AddRange(parametros);
                        }
                        return cmd.ExecuteScalar();
                    }
                }
            }
            catch (MySqlException ex)
            {
                Console.WriteLine(ObtenerMensajeError(ex));
                return null;
            }
        }

        public static DataTable EjecutarConsulta(string sql, params MySqlParameter[] parametros)
        {
            var tabla = new DataTable();
            try
            {
                using (var con = NuevaConexion())
                {
                    con.Open();
                    using (var cmd = new MySqlCommand(sql, con))
                    {
                        if (parametros != null)
                        {
                            cmd.Parameters.AddRange(parametros);
                        }

                        using (var adaptador = new MySqlDataAdapter(cmd))
                        {
                            adaptador.Fill(tabla);
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                Console.WriteLine(ObtenerMensajeError(ex));
            }
            return tabla;
        }
    }
}