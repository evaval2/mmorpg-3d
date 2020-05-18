using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Npgsql;

namespace MMORPG_Server.Config
{
    class Configuration
    {
        public static string DB_SERVER = "localhost";
        public static int DB_PORT = 5432;
        public static string DB_NAME = "mmorpg";
        public static string DB_USER = "postgres";
        public static string DB_PASS = "root";
        public static string UDP_SERVER_ADDR = "localhost";
        public static int UDP_SERVER_PORT = 24690;
        public static string TCP_SERVER_ADDR = "127.0.0.1";
        public static int TCP_SERVER_PORT = 24685;

        public static string GetDBConnectionString()
        {
            NpgsqlConnectionStringBuilder stringBuilder = new NpgsqlConnectionStringBuilder();
            stringBuilder.Host = DB_SERVER;
            stringBuilder.Port = DB_PORT;
            stringBuilder.Database = DB_NAME;
            stringBuilder.Username = DB_USER;
            stringBuilder.Password = DB_PASS;
            return stringBuilder.ConnectionString;
        }
    }
}
