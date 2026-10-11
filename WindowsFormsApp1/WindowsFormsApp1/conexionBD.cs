using System;
using System.Data;
using MySql.Data.MySqlClient;

namespace WindowsFormsApp1
{
    /*
     * Centraliza la conexión y las operaciones comunes con MySQL. Hay una copia en el proyecto de interfaz 
     * y otra en el proyecto API; documenta cada una según su ubicación
    */
    public static class conexionBD
    {
        // cambiar contraseña de sql
        private const string cadenaConexion = "Server=127.0.0.1;Port=3306;Database=uno;Uid=root;Pwd=alin;";

        //crea y devuelve un objeto de conexión a MySQL; el llamador es responsable de abrirlo/cerrarlo según el uso
        public static MySqlConnection NuevaConexion()
        {
            return new MySqlConnection(cadenaConexion);
        }

        //intenta comprobar la conexión y devuelve el resultado junto con un mensaje de error si falla
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

        //convierte una excepción de MySQL en un mensaje entendible para mostrar o registrar
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

        //ejecuta una instrucción que no devuelve filas, como INSERT, UPDATE o DELETE, y devuelve el número de filas afectadas
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
                System.Windows.Forms.MessageBox.Show(ObtenerMensajeError(ex), "Error de base de datos");
                return -1;
            }
        }

        //ejecuta una consulta y devuelve un único valor, como COUNT o un identificador generado
        public static object EjecutarEscalar(string sql, params MySqlParameter[] parametros)
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
                System.Windows.Forms.MessageBox.Show(ObtenerMensajeError(ex), "Error de base de datos");
                return null;
            }
        }

        //ejecuta una consulta de lectura y devuelve los resultados en un DataTable
        //Nota: los parámetros SQL ayudan a evitar concatenar directamente valores del usuario dentro de la consulta
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
                System.Windows.Forms.MessageBox.Show(ObtenerMensajeError(ex), "Error de base de datos");
            }
            return tabla;
        }
    }
}