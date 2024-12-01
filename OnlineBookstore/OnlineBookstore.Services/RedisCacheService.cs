using Microsoft.Data.SqlClient;
using StackExchange.Redis;


namespace OnlineBookingstore
{
    public class RedisCacheService
    {
        private readonly ConnectionMultiplexer _redis;
        private readonly SqlConnection _sqlConnection;

        public RedisCacheService(ConnectionMultiplexer redis, SqlConnection sqlConnection)
        {
            _redis = redis;
            _sqlConnection = sqlConnection;
        }


        /// <summary>
        /// opdater cache inventory med data direkte fra SQL tabellen
        /// </summary>
        /// <returns></returns>
        public async Task UpdateRedisCacheUsingSqlInventoryTabel()
        {
            // Hent databasen fra den eksisterende Redis-forbindelse
            var db = _redis.GetDatabase();

            // Hent nyeste lagerstatus for alle bøger fra SQL inventory tabellen
            var stockQuantities = await GetNewStockQuantitiesFromSql();


            Console.WriteLine("Cache updated inventory. Current stock:");
            foreach (var kvp in stockQuantities.OrderBy(kvp => kvp.Key))
            {
                int bookId = kvp.Key;
                int stockQuantity = kvp.Value;

                // Opdater Redis cache
                db.StringSet($"bookId:{bookId}:stock", stockQuantity);
                Console.WriteLine($"BookId {kvp.Key}: {kvp.Value} in stock.");
            }
        }


        /// <summary>
        /// Tjek redis cache om der er nok bøger på lager til at gennemføre ordren
        /// </summary>
        /// <param name="bookIds"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task<List<int>> CheckStockAvailabilityInRedisCache(List<int> bookIds)
        {
            var orderBookQuantities = bookIds
                .GroupBy(bookId => bookId)
                .ToDictionary(g => g.Key, g => g.Count()); // Tæller hvor mange gange hver bookId optræder i købsordren

            var insufficientStock = new List<int>(); // Liste over bøger med utilstrækkelig lagerbeholdning

            var db = _redis.GetDatabase();


            // Tjek lagerbeholdningen for hver bog
            foreach (var bookId in orderBookQuantities.Keys)
            {
                var stockQuantity = (int)await db.StringGetAsync($"bookId:{bookId}:stock"); // stockQuantity er gemt under key navnet: "$"bookId:{bookId}:stock""

                
                if (stockQuantity < orderBookQuantities[bookId]) // Hvis lagerbeholdningen er mindre end hvad købsordren lyder på
                {
                    Console.WriteLine($"test: stockquantity {stockQuantity}, bookquantity {orderBookQuantities[bookId]}");
                    insufficientStock.Add(bookId);
                }
            }

            // Hvis der er utilstrækkeligt lager for nogle bøger, rulles transaktionen tilbage
            if (insufficientStock.Any())
            {
                throw new InvalidOperationException($"Insufficient stock for bookIds: {string.Join(", ", insufficientStock)}. Order is cancelled. Exception is thrown");
            }
            return insufficientStock;
        }

        
        public async Task<Dictionary<int, int>> GetNewStockQuantitiesFromSql() // Flyttet herover for at undgå cirkulær afhængighed ml SqlService og RedisCacheService
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

            // Sørger for, at SQL- forbindelsen er åben (kan sikkert gøres smartere)
            if (_sqlConnection.State != System.Data.ConnectionState.Open)
            {
                await _sqlConnection.OpenAsync();
            }

            using (var command = new SqlCommand(query, _sqlConnection))
            {
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        int bookId = reader.GetInt32(0);        // Første kolonne: BookId
                        int stockQuantity = reader.GetInt32(1); // Anden kolonne: StockQuantity

                        stockQuantities[bookId] = stockQuantity;
                    }
                }
            }
            return stockQuantities;
        }
    }
}
