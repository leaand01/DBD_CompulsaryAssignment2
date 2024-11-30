using System;
using System.Reflection;
using Microsoft.Data.SqlClient;

namespace PopulateSqlTables
{
    public class CreateBooks
    {
        private readonly SqlConnection _sqlConnection;

        // Constructor injection af SqlConnection
        public CreateBooks(SqlConnection sqlConnection)
        {
            _sqlConnection = sqlConnection;
        }


        //public static void CreateBook(SqlConnection connection, string title, string author, string genre, decimal price)
        public void CreateBook(string title, string author, string genre, decimal price)
        {
            string insertQuery = "INSERT INTO Books (Title, Author, Genre, Price) VALUES (@Title, @Author, @Genre, @Price)";

            using (var connection = new SqlConnection(_sqlConnection.ConnectionString))
            {
                connection.Open(); // Åbn forbindelsen
                using (SqlCommand cmd = new SqlCommand(insertQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@Title", title);
                    cmd.Parameters.AddWithValue("@Author", author);
                    cmd.Parameters.AddWithValue("@Genre", genre);
                    cmd.Parameters.AddWithValue("@Price", price);
                    cmd.ExecuteNonQuery();
                }
            }

                    /*
                    //using (SqlCommand cmd = new SqlCommand(insertQuery, connection))
                    using (SqlCommand cmd = new SqlCommand(insertQuery, _sqlConnection))
                    {
                        cmd.Parameters.AddWithValue("@Title", title);
                        cmd.Parameters.AddWithValue("@Author", author);
                        cmd.Parameters.AddWithValue("@Genre", genre);
                        cmd.Parameters.AddWithValue("@Price", price);
                        cmd.ExecuteNonQuery();
                    }
                    */
                }
    }
}
