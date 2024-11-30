using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;

namespace PopulateSqlTables
{
    public class Print
    {
        private readonly SqlConnection _sqlConnection;

        // Constructor injection af SqlConnection
        public Print(SqlConnection sqlConnection)
        {
            _sqlConnection = sqlConnection;
        }

        public void AllCustomers()
        {
            //using (SqlConnection connection = new SqlConnection(connectionString))
            //{
            try
            {
                //connection.Open();
                //_sqlConnection.Open();

                string query = "SELECT customerId, email FROM Customers";


                using (var connection = new SqlConnection(_sqlConnection.ConnectionString))
                {
                    connection.Open(); // Åbn forbindelsen
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


                        /*
                        using (SqlCommand command = new SqlCommand(query, _sqlConnection))
                        {
                            using (SqlDataReader reader = command.ExecuteReader())
                            {
                                Console.WriteLine("Customers in the database:");

                                while (reader.Read())
                                {
                                    Console.WriteLine($"CustomerId: {reader["CustomerId"]}, Email: {reader["email"]}");
                                }
                            }
                        }
                        */
                    }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occured called PrintAllCustomers: {ex.Message}");
                throw;
            }
            //}
        }

        public void AllBooks()
        {
            //using (SqlConnection connection = new SqlConnection(connectionString))
            //{
            try
            {
                // Åbn SQL-forbindelsen
                //connection.Open();

                string query = "SELECT BookId, Title, Author, Genre, Price FROM Books";


                using (var connection = new SqlConnection(_sqlConnection.ConnectionString))
                {
                    connection.Open(); // Åbn forbindelsen
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



                /////////////////////////////////////////
                /*
                using (SqlCommand command = new SqlCommand(query, _sqlConnection))
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
                */
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occured calling PrintAllBooks: {ex.Message}");
                throw;
            }
            //}
        }



    }
}
