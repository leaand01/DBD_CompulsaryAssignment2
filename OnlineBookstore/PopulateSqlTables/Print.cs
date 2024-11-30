using Microsoft.Data.SqlClient;


namespace PopulateSqlTables
{
    public class Print
    {
        private readonly SqlConnection _sqlConnection;

        public Print(SqlConnection sqlConnection)
        {
            _sqlConnection = sqlConnection;
        }

        public void AllCustomers()
        {
            try
            {
                string query = "SELECT customerId, email FROM Customers";

                using (var connection = new SqlConnection(_sqlConnection.ConnectionString))
                {
                    connection.Open();
                    using (SqlCommand cmd = new SqlCommand(query, connection))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            Console.WriteLine("Customers in the database:");
                            while (reader.Read())
                            {
                                Console.WriteLine($"CustomerId: {reader["CustomerId"]}, Email: {reader["email"]}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occured called PrintAllCustomers: {ex.Message}");
                throw;
            }
        }

        public void AllBooks()
        {
            try
            {
                string query = "SELECT BookId, Title, Author, Genre, Price FROM Books";

                using (var connection = new SqlConnection(_sqlConnection.ConnectionString))
                {
                    connection.Open();
                    using (SqlCommand cmd = new SqlCommand(query, connection))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            Console.WriteLine("Books in the database:");
                            while (reader.Read())
                            {
                                Console.WriteLine($"BookId: {reader["BookId"]}, Title: {reader["Title"]}, " +
                                                    $"Author: {reader["Author"]}, Genre: {reader["Genre"]}, " +
                                                    $"Price: {reader["Price"]}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occured calling PrintAllBooks: {ex.Message}");
                throw;
            }
        }
    }
}
