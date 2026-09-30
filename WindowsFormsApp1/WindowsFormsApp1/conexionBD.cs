using System;
using System.Data;
using MySql.Data.MySqlClient;

namespace WindowsFormsApp1
{
    public static class conexionBD
    {
        private const string cadenaConexion = "Server=127.0.0.1;Port=3306;Database=uno_db;Uid=root;Pwd=root;";

        public static MySqlConnection NuevaConexion()
        {
            return new MySqlConnection(cadenaConexion);
        }
    }
}
