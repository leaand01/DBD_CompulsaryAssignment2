using Microsoft.Data.SqlClient;


namespace PopulateSqlTables
{
    public class Initialize
    {
        private readonly SqlConnection _sqlConnection;

        public Initialize(SqlConnection sqlConnection)
        {
            _sqlConnection = sqlConnection;
        }


        public bool IsTableEmpty(string tableName)
        {
            string query = $"SELECT COUNT(*) FROM {tableName}";
            using (var connection = new SqlConnection(_sqlConnection.ConnectionString))
            {
                connection.Open();
                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    int count = (int)cmd.ExecuteScalar();  // Henter antal rækker i tabellen
                    return count == 0;  // Returner true hvis tabellen er tom, ellers false
                }
            }
        }
    }
}
