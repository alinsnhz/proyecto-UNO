using System;
using System.Data;
using MySql.Data.MySqlClient;

namespace WindowsFormsApp1
{
    public static class conexionBD
    {
        private const string cadenaConexion = "Server=127.0.0.1;Port=3306;Database=uno;Uid=root;Pwd=root;";

        public static MySqlConnection NuevaConexion()
        {
            return new MySqlConnection(cadenaConexion);
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
                    return "No se pudo conectar al servidor MySQL. ¿Está corriendo el servicio?";
                case 1042:
                    return "No se encuentra el servidor (host/puerto incorrectos).";
                case 1045:
                    return "Usuario o contraseña incorrectos.";
                case 1049:
                    return "La base de datos 'uno_db' no existe. Corre primero schema_uno_mysql.sql.";
                default:
                    return $"Error de MySQL ({ex.Number}): {ex.Message}";
            }
        }
    }
}
