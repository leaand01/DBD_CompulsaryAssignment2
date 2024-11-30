using Microsoft.Data.SqlClient;


namespace PopulateSqlTables
{
    public class CreateCustomers
    {
        private readonly SqlConnection _sqlConnection;

        public CreateCustomers(SqlConnection sqlConnection)
        {
            _sqlConnection = sqlConnection;
        }


        public void CreateCustomer(string email)
        {
            string insertQuery = "INSERT INTO Customers (email) VALUES (@email)";

            using (var connection = new SqlConnection(_sqlConnection.ConnectionString))
            {
                connection.Open();
                using (SqlCommand cmd = new SqlCommand(insertQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@email", email);
                    cmd.ExecuteNonQuery();
                }

            }
        }
    }
}
