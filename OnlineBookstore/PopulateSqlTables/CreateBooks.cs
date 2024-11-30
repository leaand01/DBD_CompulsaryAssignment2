using Microsoft.Data.SqlClient;


namespace PopulateSqlTables
{
    public class CreateBooks
    {
        private readonly SqlConnection _sqlConnection;

        public CreateBooks(SqlConnection sqlConnection)
        {
            _sqlConnection = sqlConnection;
        }


        public void CreateBook(string title, string author, string genre, decimal price)
        {
            string insertQuery = "INSERT INTO Books (Title, Author, Genre, Price) VALUES (@Title, @Author, @Genre, @Price)";

            using (var connection = new SqlConnection(_sqlConnection.ConnectionString))
            {
                connection.Open();
                using (SqlCommand cmd = new SqlCommand(insertQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@Title", title);
                    cmd.Parameters.AddWithValue("@Author", author);
                    cmd.Parameters.AddWithValue("@Genre", genre);
                    cmd.Parameters.AddWithValue("@Price", price);
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
