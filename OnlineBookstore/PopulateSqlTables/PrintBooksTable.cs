/*
using System;
using Microsoft.Data.SqlClient;

namespace PopulateSqlTables
{
    public static class PrintBooks
    {
        public static void PrintAllBooks(string connectionString)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
           {
                try
                {
                    // Åbn SQL-forbindelsen
                    connection.Open();

                    string query = "SELECT BookId, Title, Author, Genre, Price FROM Books";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
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
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occured calling PrintAllBooks: {ex.Message}");
                    throw;
                }
            }
        }
    }
}
*/