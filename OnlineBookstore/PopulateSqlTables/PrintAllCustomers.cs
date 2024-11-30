/*
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PopulateSqlTables
{
    //public static class PrintCustomers
    public class PrintCustomers
    {
        private readonly SqlConnection _sqlConnection;

        // Constructor injection af SqlConnection
        public PrintCustomers(SqlConnection sqlConnection)
        {
            _sqlConnection = sqlConnection;
        }


        //public static void PrintAllCustomers(string connectionString)
        //public static void PrintAllCustomers()
        public void PrintAllCustomers()
        {
            //using (SqlConnection connection = new SqlConnection(connectionString))
            //{
            try
            {
                //connection.Open();
                //_sqlConnection.Open();

                string query = "SELECT customerId, email FROM Customers";

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
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occured called PrintAllCustomers: {ex.Message}");
                throw;
            }
            //}
        }
    }
}
*/