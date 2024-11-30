using StackExchange.Redis;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net;

namespace OnlineBookstore.Services
{
    public class InventoryService
    {
        private readonly string sqlConnectionString;
        private readonly IDatabase redisDatabase;

        public InventoryService(string sqlConnectionString, ConnectionMultiplexer redisConnection)
        {
            this.sqlConnectionString = sqlConnectionString;
            this.redisDatabase = redisConnection.GetDatabase();
        }

        // Henter den nyeste lagerstatus for alle bøger fra SQL Server og gemmer det i Redis
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

            using (SqlConnection connection = new SqlConnection(sqlConnectionString))
            {
                try
                {
                    await connection.OpenAsync();

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
                                await redisDatabase.StringSetAsync(redisKey, $"{stockQuantity},{lastUpdated}");
                                //Console.WriteLine($"Updated Redis cache for BookId: {bookId} with StockQuantity: {stockQuantity} and LastUpdated: {lastUpdated}");
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
            // Opret forbindelse til Redis
            var redis = ConnectionMultiplexer.Connect("localhost");  // Brug host som en string (f.eks. "localhost")
            var server = redis.GetServer("localhost", 6379);  // Angiv host og port som separate argumenter

            // Dictionary for at holde den nyeste lagerstatus per bog
            var latestInventoryData = new Dictionary<int, (int stockQuantity, DateTime lastUpdated)>();

            // Hent alle keys der starter med "inventory:"
            await foreach (var key in server.KeysAsync(pattern: "inventory:*"))
            {
                string inventoryData = await redisDatabase.StringGetAsync(key);

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

            Console.WriteLine("Cached Inventory - Latest update:");

            // Print de nyeste lagerstatusser for hver bog
            foreach (var kvp in latestInventoryData)
            {
                int bookId = kvp.Key;
                var (stockQuantity, lastUpdated) = kvp.Value;

                Console.WriteLine($"Book ID: {bookId}, Stock Quantity: {stockQuantity}, Last Updated: {lastUpdated}");
            }
        }

        /*
        // Henter nyeste lagerstatus fra Redis Cache for alle bøger
        public async Task PrintAllInventoryAsync()
        {
            // Vi skal hente alle Redis keys der starter med "inventory:"
            //var server = redisDatabase.Multiplexer.GetServer(redisDatabase.HashSet("localhost", 6379).ToString()); // Here, replace the host and port appropriately.

            // Opret forbindelse til Redis via Multiplexer
            var redis = ConnectionMultiplexer.Connect("localhost");  // Brug host som en string (for eksempel "localhost")
            var server = redis.GetServer("localhost", 6379);  // Angiv host og port som separate argumenter



            await foreach (var key in server.KeysAsync(pattern: "inventory:*"))
            {
                string inventoryData = await redisDatabase.StringGetAsync(key);

                if (!string.IsNullOrEmpty(inventoryData))
                {
                    var data = inventoryData.Split(',');

                    int bookId = int.Parse(key.ToString().Split(':')[1]);
                    int stockQuantity = int.Parse(data[0]);
                    DateTime lastUpdated = DateTime.Parse(data[1]);

                    Console.WriteLine($"Book ID: {bookId}, Stock Quantity: {stockQuantity}, Last Updated: {lastUpdated}");
                }
                else
                {
                    Console.WriteLine($"No inventory data found for BookId: {key}");
                }
            }
        }
        */
    }
}




/*
using StackExchange.Redis;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;

namespace OnlineBookstore.Services
{
    public class InventoryService
    {
        private readonly string sqlConnectionString;
        private readonly IDatabase redisDatabase;

        public InventoryService(string sqlConnectionString, ConnectionMultiplexer redisConnection)
        {
            this.sqlConnectionString = sqlConnectionString;
            this.redisDatabase = redisConnection.GetDatabase();
        }

        // Update redis cache with the latest stock quantity per book
        public void UpdateInventoryCache()
        {
            string query = @"
                SELECT BookId, StockQuantity, LastUpdated
                FROM Inventory
                WHERE (BookId, LastUpdated) IN (
                    SELECT BookId, MAX(LastUpdated) AS LastUpdated
                    FROM Inventory
                    GROUP BY BookId
                )
                ORDER BY BookId;
            ";

            using (SqlConnection connection = new SqlConnection(sqlConnectionString))
            {
                try
                {
                    connection.Open();

                    using (SqlCommand cmd = new SqlCommand(query, connection))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                int bookId = reader.GetInt32(reader.GetOrdinal("BookId"));
                                int stockQuantity = reader.GetInt32(reader.GetOrdinal("StockQuantity"));
                                DateTime lastUpdated = reader.GetDateTime(reader.GetOrdinal("LastUpdated"));

                                // Opret et objekts key for Redis
                                string redisKey = $"inventory:{bookId}";

                                // Gem den nyeste lagerstatus i Redis Cache
                                redisDatabase.StringSet(redisKey, $"{stockQuantity},{lastUpdated}");
                                Console.WriteLine($"Updated Redis cache for BookId: {bookId} with StockQuantity: {stockQuantity} and LastUpdated: {lastUpdated}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred while updating the inventory cache: {ex.Message}");
                }
            }
        }

        // Henter nyeste lagerstatus fra Redis Cache- lav om så henter nyeste lager status for alle bøger og ikke kun baseret på input
        public string GetInventoryFromCache(int bookId)
        {
            string redisKey = $"inventory:{bookId}";
            string inventoryData = redisDatabase.StringGet(redisKey);

            return inventoryData;
        }
    }
}



*/