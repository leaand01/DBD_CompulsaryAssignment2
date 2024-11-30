using Microsoft.Data.SqlClient;


namespace PopulateSqlTables
{
    public class CreateInventory
    {
        private readonly SqlConnection _sqlConnection;

        public CreateInventory(SqlConnection sqlConnection)
        {
            _sqlConnection = sqlConnection;
        }


        public void InitialiseInventory()
        {
            string query = @"
                            INSERT INTO Inventory (BookId, StockQuantity, LastUpdated)
                            SELECT BookId, 10, @CurrentTime
                            FROM Books
                            WHERE BookId NOT IN (SELECT BookId FROM Inventory)";


            using (var connection = new SqlConnection(_sqlConnection.ConnectionString))
            {
                connection.Open();
                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@CurrentTime", DateTime.UtcNow);
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
