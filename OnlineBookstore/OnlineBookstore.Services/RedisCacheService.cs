
using Microsoft.Data.SqlClient;
using StackExchange.Redis;
using System;
using System.Collections.Generic;

namespace OnlineBookingstore
{
    public class RedisCacheService
    {
        
        private readonly ConnectionMultiplexer _redis;
        private readonly SqlConnection _sqlConnection;
        //private readonly SQLService _sqlService; har jeg fjernet denne afhængig ved at flytte fkt fra sqlservice og herover?

        // Konstruktør som tager ConnectionMultiplexer og SqlConnection som afhængigheder
        //public RedisCacheService(ConnectionMultiplexer redis, SqlConnection sqlConnection, SQLService sqlService)
        public RedisCacheService(ConnectionMultiplexer redis, SqlConnection sqlConnection)
        {
            _redis = redis;
            _sqlConnection = sqlConnection;
 //           _sqlService = sqlService;
        }
        


        //        public void UpdateRedisCache(ConnectionMultiplexer redis, SqlConnection connection)
        //public async Task UpdateRedisCache(ConnectionMultiplexer redis, SqlConnection connection)
        public async Task UpdateRedisCache() // opdater cache med data direkte fra SQL tabellen
        {
            // Hent databasen fra den eksisterende Redis-forbindelse
            var db = _redis.GetDatabase();

            // Hent nyeste lagerstatus for alle bøger
            //var stockQuantities = GetNewStockQuantities(connection);
            //var stockQuantities = await GetNewStockQuantities(connection);
            //var stockQuantities = await GetNewStockQuantities();
            //var stockQuantities = await _sqlService.GetNewStockQuantities(); // ændret til 
            var stockQuantities = await GetNewStockQuantitiesFromSql(); // denne
            //var stockQuantities = await _sqlService.GetNewStockQuantities(connection);

            Console.WriteLine("Cache updated inventory. Current stock:");

            //foreach (var kvp in stockQuantities)
            foreach (var kvp in stockQuantities.OrderBy(kvp => kvp.Key)) // Sorter stockQuantities efter BookId
            {
                int bookId = kvp.Key;        // BookId
                int stockQuantity = kvp.Value; // StockQuantity

                // Opdater Redis cache
                db.StringSet($"bookId:{bookId}:stock", stockQuantity);
                //Console.WriteLine($"Updated Redis cache for book {bookId} with new stock quantity: {stockQuantity}");
                Console.WriteLine($"BookId {kvp.Key}: {kvp.Value} in stock.");
            }
        }


        //public async Task<List<int>> CheckStockAvailability(ConnectionMultiplexer redis, List<int> bookIds) // skal flyttes til rediscacheservice? omdøb så navn forklarer det er cache der tjekkes
        public async Task<List<int>> CheckStockAvailability(List<int> bookIds) // tjekker med redis cache om der er nok bøger på lager til at gennemføre ordren
        {
            //Console.WriteLine("test: bliver CheckStockAvailability kaldt?");

            // from the inputtet list bookIds count the number of times each unique bookId is written and store in this dict

            //var bookQuantities = bookIds
            var orderBookQuantities = bookIds
                .GroupBy(bookId => bookId)
                .ToDictionary(g => g.Key, g => g.Count()); // Tæller hvor mange gange hver bookId optræder i ordren (dette er for ordren, ikke i sql db eller redis cache (tror jeg)

            var insufficientStock = new List<int>(); // Liste over bøger med utilstrækkelig lagerbeholdning

            var db = _redis.GetDatabase();


            // Tjek lagerbeholdningen for hver bog
            //foreach (var bookId in bookQuantities.Keys)
            foreach (var bookId in orderBookQuantities.Keys)
            {
                //var stockQuantity = (int)db.StringGet($"book:{bookId}:stock");
                //var stockQuantity = (int)await db.StringGetAsync($"book:{bookId}:stock");
                var stockQuantity = (int)await db.StringGetAsync($"bookId:{bookId}:stock"); // the stockQuantity is stored in the key long key name "$"bookId:{bookId}:stock""

                //if (stockQuantity < bookQuantities[bookId]) // Hvis lagerbeholdningen er mindre end den nødvendige mængde
                if (stockQuantity < orderBookQuantities[bookId]) // Hvis lagerbeholdningen er mindre end den nødvendige mængde
                {
                    //Console.WriteLine($"test: stockquantity {stockQuantity}, bookquantity {bookQuantities[bookId]}");
                    Console.WriteLine($"test: stockquantity {stockQuantity}, bookquantity {orderBookQuantities[bookId]}");
                    insufficientStock.Add(bookId); // Tilføj bog til listen over utilstrækkeligt lager
                }
            }



            // Hvis der er utilstrækkeligt lager for nogle bøger, rulles transaktionen tilbage
            if (insufficientStock.Any())
            {
                Console.WriteLine("test: kommer vi ind i: if (insufficientStock.Any()) ");
                throw new InvalidOperationException($"Insufficient stock for bookIds: {string.Join(", ", insufficientStock)}. Order is cancelled. Exception is thrown");
            }
            return insufficientStock; // Returner listen over bøger med utilstrækkeligt lager

        }



        /*
        private async Task<Dictionary<int, int>> GetNewStockQuantities(SqlConnection connection)
        {
            string query = @"
                    SELECT i.BookId, i.StockQuantity
                    FROM Inventory i
                    INNER JOIN (
                        SELECT BookId, MAX(LastUpdated) AS LastUpdated
                        FROM Inventory
                        GROUP BY BookId
                    ) latest ON i.BookId = latest.BookId AND i.LastUpdated = latest.LastUpdated;";

            var stockQuantities = new Dictionary<int, int>();

            using (var command = new SqlCommand(query, connection))
            {
                using (var reader = await command.ExecuteReaderAsync())  // Brug ExecuteReaderAsync
                {
                    while (await reader.ReadAsync())  // Brug ReadAsync
                    {
                        int bookId = reader.GetInt32(0);  // Første kolonne: BookId
                        int stockQuantity = reader.GetInt32(1);  // Anden kolonne: StockQuantity

                        stockQuantities[bookId] = stockQuantity;
                    }
                }
            }

            return stockQuantities;
        }
        */


        //public async Task ClearRedisCache(ConnectionMultiplexer redis)
        public async Task ClearRedisCache()
        {
            //var db = redis.GetDatabase();
            var db = _redis.GetDatabase();
            await db.ExecuteAsync("FLUSHALL");  // Rydder alle databaser i Redis
            Console.WriteLine("Redis cache cleared.");
        }


        //public async Task<Dictionary<int, int>> GetNewStockQuantities()
        public async Task<Dictionary<int, int>> GetNewStockQuantitiesFromSql() // forsøgt flyttet herover for at undgå cirkulær afhængighed ml SqlService og RedisCacheService
        {
            string query = @"
                    SELECT i.BookId, i.StockQuantity
                    FROM Inventory i
                    INNER JOIN (
                        SELECT BookId, MAX(LastUpdated) AS LastUpdated
                        FROM Inventory
                        GROUP BY BookId
                    ) latest ON i.BookId = latest.BookId AND i.LastUpdated = latest.LastUpdated;";

            var stockQuantities = new Dictionary<int, int>();

            // Sørg for, at forbindelsen er åben
            if (_sqlConnection.State != System.Data.ConnectionState.Open)
            {
                await _sqlConnection.OpenAsync(); // Åbn forbindelsen asynkront
            }

            //using (var command = new SqlCommand(query, connection))
            using (var command = new SqlCommand(query, _sqlConnection))
            {
                using (var reader = await command.ExecuteReaderAsync())  // Brug ExecuteReaderAsync
                {
                    while (await reader.ReadAsync())  // Brug ReadAsync
                    {
                        int bookId = reader.GetInt32(0);  // Første kolonne: BookId
                        int stockQuantity = reader.GetInt32(1);  // Anden kolonne: StockQuantity

                        stockQuantities[bookId] = stockQuantity;
                    }
                }
            }

            return stockQuantities;
        }
    }
}
