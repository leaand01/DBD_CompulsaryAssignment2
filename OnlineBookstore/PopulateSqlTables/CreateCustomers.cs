using Microsoft.Data.SqlClient;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PopulateSqlTables
{
    public class CreateCustomers
    {
        private readonly SqlConnection _sqlConnection;

        // Constructor injection af SqlConnection
        public CreateCustomers(SqlConnection sqlConnection)
        {
            _sqlConnection = sqlConnection;
        }


        //public static void CreateCustomer(SqlConnection connection, string email)
        //public static void CreateCustomer(string email)
        public void CreateCustomer(string email)
        {
            string insertQuery = "INSERT INTO Customers (email) VALUES (@email)";

            using (var connection = new SqlConnection(_sqlConnection.ConnectionString))
            {
                connection.Open(); // Åbn forbindelsen
                using (SqlCommand cmd = new SqlCommand(insertQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@email", email);
                    cmd.ExecuteNonQuery();
                }

            }

                /*
                    //using (SqlCommand cmd = new SqlCommand(insertQuery, connection))
                    using (SqlCommand cmd = new SqlCommand(insertQuery, _sqlConnection))
            {
                cmd.Parameters.AddWithValue("@email", email);
                cmd.ExecuteNonQuery();
            }
                */
            }

        }
}
