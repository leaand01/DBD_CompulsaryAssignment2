using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PopulateSqlTables
{
    public class Initialize
    {
        private readonly SqlConnection _sqlConnection;

        // Constructor injection af SqlConnection
        public Initialize(SqlConnection sqlConnection)
        {
            _sqlConnection = sqlConnection;
        }


        //public static bool IsTableEmpty(SqlConnection connection, string tableName)
        //public static bool IsTableEmpty(string tableName)
        public bool IsTableEmpty(string tableName)
        {
            string query = $"SELECT COUNT(*) FROM {tableName}";
            //using (SqlCommand cmd = new SqlCommand(query, connection))
            //using (SqlCommand cmd = new SqlCommand(query, _sqlConnection))
            using (var connection = new SqlConnection(_sqlConnection.ConnectionString))
            {
                connection.Open(); // Åbn forbindelsen
                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    int count = (int)cmd.ExecuteScalar();  // Henter antal rækker i tabellen
                    return count == 0;  // Returner true hvis tabellen er tom, ellers false
                }


//                    int count = (int)cmd.ExecuteScalar();  // Henter antal rækker i tabellen
  //              return count == 0;  // Returner true hvis tabellen er tom, ellers false
            }
        }
    }
}
