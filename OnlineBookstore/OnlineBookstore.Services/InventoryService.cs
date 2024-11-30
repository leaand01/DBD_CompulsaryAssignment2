using StackExchange.Redis;
using Microsoft.Data.SqlClient;


namespace OnlineBookstore.Services
{
    public class InventoryService
    {
        private readonly string _sqlConnectionString;
        private readonly IDatabase _redisDatabase;

        public InventoryService(string sqlConnectionString, ConnectionMultiplexer redisConnection)
        {
            _sqlConnectionString = sqlConnectionString;
            _redisDatabase = redisConnection.GetDatabase();
        }


        public async Task UpdateInventoryCacheAsync()
        {
            string query = @"
                            SELECT i.BookId, i.StockQuantity, i.LastUpdated
                            FROM Inventory i
                            INNER JOIN (
                                SELECT BookId, MAX(LastUpdated) AS LastUpdated
                                FROM Inventory
                                GROUP BY BookId
                            ) latest
                            ON i.BookId = latest.BookId AND i.LastUpdated = latest.LastUpdated
                            ORDER BY i.BookId;
                        ";

            using (SqlConnection connection = new SqlConnection(_sqlConnectionString))
            {
                try
                {
                    // følgende 3 linjer kan sikker gøres smartere og mere effektivt
                    await connection.OpenAsync(); // opret forbindelse til SQL

                    using (SqlCommand cmd = new SqlCommand(query, connection))
                    {
                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                int bookId = reader.GetInt32(reader.GetOrdinal("BookId"));
                                int stockQuantity = reader.GetInt32(reader.GetOrdinal("StockQuantity"));
                                DateTime lastUpdated = reader.GetDateTime(reader.GetOrdinal("LastUpdated"));

                                // Opret et objekts key for Redis
                                string redisKey = $"inventory:{bookId}";

                                // Gem den nyeste lagerstatus i Redis Cache
                                await _redisDatabase.StringSetAsync(redisKey, $"{stockQuantity},{lastUpdated}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred while updating the inventory cache: {ex.Message}");
                    throw;
                }
            }
        }


        public async Task PrintAllInventoryAsync()
        {
            // Opret forbindelse til Redis cache
            var redis = ConnectionMultiplexer.Connect("localhost");
            var server = redis.GetServer("localhost", 6379);

            // Dictionary til at gemme nyeste lagerstatus per bog
            var latestInventoryData = new Dictionary<int, (int stockQuantity, DateTime lastUpdated)>();

            // loop over alle keys der starter med "inventory:"
            await foreach (var key in server.KeysAsync(pattern: "inventory:*"))
            {
                string inventoryData = await _redisDatabase.StringGetAsync(key);

                if (!string.IsNullOrEmpty(inventoryData))
                {
                    var data = inventoryData.Split(',');

                    int bookId = int.Parse(key.ToString().Split(':')[1]);
                    int stockQuantity = int.Parse(data[0]);
                    DateTime lastUpdated = DateTime.Parse(data[1]);

                    // Tjek om vi allerede har lagret lagerdata for denne bog
                    if (latestInventoryData.ContainsKey(bookId))
                    {
                        // Hvis vi har den, skal vi kun opdatere, hvis den nye opdatering er nyere
                        if (lastUpdated > latestInventoryData[bookId].lastUpdated)
                        {
                            latestInventoryData[bookId] = (stockQuantity, lastUpdated);
                        }
                    }
                    else
                    {
                        // Hvis vi ikke har lagerdata for denne bog, gem den
                        latestInventoryData[bookId] = (stockQuantity, lastUpdated);
                    }
                }
                else
                {
                    Console.WriteLine($"No inventory data found for BookId: {key}");
                }
            }

            // Print nyeste lagerstatusser for hver bog
            Console.WriteLine("Cached Inventory - Latest update:");
            foreach (var kvp in latestInventoryData)
            {
                int bookId = kvp.Key;
                var (stockQuantity, lastUpdated) = kvp.Value;

                Console.WriteLine($"Book ID: {bookId}, Stock Quantity: {stockQuantity}, Last Updated: {lastUpdated}");
            }
        }
    }
}
