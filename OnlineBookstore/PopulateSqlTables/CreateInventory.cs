using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PopulateSqlTables
{
    public class CreateInventory
    {
        private readonly SqlConnection _sqlConnection;

        // Constructor injection af SqlConnection
        public CreateInventory(SqlConnection sqlConnection)
        {
            _sqlConnection = sqlConnection;
        }

        ///public static void InitialiseInventory(SqlConnection connection)
        public void InitialiseInventory()
        {
            /*
            string query = @"
                            INSERT INTO Inventory (BookId, StockQuantity, LastUpdated)
                            SELECT BookId, 100, @CurrentTime
                            FROM Books
                            WHERE BookId NOT IN (SELECT BookId FROM Inventory)";
            */
            string query = @"
                            INSERT INTO Inventory (BookId, StockQuantity, LastUpdated)
                            SELECT BookId, 10, @CurrentTime
                            FROM Books
                            WHERE BookId NOT IN (SELECT BookId FROM Inventory)";


            using (var connection = new SqlConnection(_sqlConnection.ConnectionString))
            {
                connection.Open(); // Åbn forbindelsen
                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@CurrentTime", DateTime.UtcNow);
                    cmd.ExecuteNonQuery();
                }
            }


                    /*
                    //using (SqlCommand cmd = new SqlCommand(query, connection))
                    using (SqlCommand cmd = new SqlCommand(query, _sqlConnection))
                    {
                        cmd.Parameters.AddWithValue("@CurrentTime", DateTime.UtcNow);
                        cmd.ExecuteNonQuery();
                    }
                    */
                }
    }
}
