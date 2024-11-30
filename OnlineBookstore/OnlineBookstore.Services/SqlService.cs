using System;
using System.Data.SqlClient;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using StackExchange.Redis;
using System.Transactions;
using System.Data.Common;

namespace OnlineBookingstore
{
    public class SQLService
    {
        
        private readonly SqlConnection _sqlConnection;
        private readonly RedisCacheService _redisCacheService;

        // Konstruktøren til at modtage SqlConnection og RedisCacheService
        public SQLService(SqlConnection sqlConnection, RedisCacheService redisCacheService)
        {
            _sqlConnection = sqlConnection;
            _redisCacheService = redisCacheService; // Initialiserer RedisCacheService
        }

        /*
        private readonly string _sqlConnectionString;
        //private readonly RedisCacheService _redisCacheService;  //tilføjet

        public SQLService(string sqlConnectionString)
        //public SQLService(string sqlConnectionString, RedisCacheService redisCacheService) //ændret pga tilføjelser
        {
            _sqlConnectionString = sqlConnectionString;
        //    _redisCacheService = redisCacheService;  //tilføjet
        }
        */


        public async Task UpdateSQLDatabase(Guid orderId, int customerId, decimal priceTotal, List<int> bookIds, SqlTransaction sqlTransaction)
        {
            Console.WriteLine("test: bliver UpdateSQLDatabase kaldt");

            try
            {
                // Sørg for at forbindelsen er åben
                if (_sqlConnection.State != System.Data.ConnectionState.Open)
                {
                    await _sqlConnection.OpenAsync(); // Åbn forbindelsen asynkront
                }

                // Opdater ClosedOrders-tabellen med den nye ordre
                string insertOrderQuery = @"
            INSERT INTO ClosedOrders (OrderId, CustomerId, DateTime, PriceTotal) 
            VALUES (@OrderId, @CustomerId, @DateTime, @PriceTotal)";
                using (var command = new SqlCommand(insertOrderQuery, _sqlConnection, sqlTransaction)) // Brug den eksisterende transaktion
                {
                    command.Parameters.AddWithValue("@OrderId", orderId);
                    command.Parameters.AddWithValue("@CustomerId", customerId);
                    command.Parameters.AddWithValue("@DateTime", DateTime.Now);
                    command.Parameters.AddWithValue("@PriceTotal", priceTotal);
                    await command.ExecuteNonQueryAsync(); // Asynkron udførelse af kommando
                }

                // Opdater Inventory-tabellen, så lagerbeholdningen reduceres
                foreach (var bookId in bookIds)
                {
                    string updateInventoryQuery = @"
                    UPDATE Inventory SET StockQuantity = StockQuantity - 1 
                    WHERE BookId = @BookId AND StockQuantity > 0";  // ikke effektiv. Burde gruppere pr bookid. 
                    using (var command = new SqlCommand(updateInventoryQuery, _sqlConnection, sqlTransaction)) // Brug den eksisterende transaktion
                    {
                        command.Parameters.AddWithValue("@BookId", bookId);
                        await command.ExecuteNonQueryAsync(); // Asynkron udførelse af kommando
                    }
                }

                // her mangler at opdatere cache?


                // Hvis alt er gået godt, skal vi afslutte transaktionen
                await Task.CompletedTask; // Returner en afsluttet Task for at sikre korrekt async afslutning

            }
            catch (Exception ex)
            {
                // Hvis en fejl opstår, rollback transaktionen
                Console.WriteLine($"SQL transaktion fejlede: {ex.Message}");
                throw; // Genkaster undtagelsen for at sikre rollback i kaldende metode
            }
        }


        /*
        public async Task UpdateSQLDatabase(Guid orderId, int customerId, decimal priceTotal, List<int> bookIds)
        {
            Console.WriteLine("test: bliver UpdateSQLDatabase kaldt");

            try
            {
                // Sørg for at forbindelsen er åben
                if (_sqlConnection.State != System.Data.ConnectionState.Open)
                {
                    await _sqlConnection.OpenAsync(); // Åbn forbindelsen asynkront
                }

                // Start transaktionen asynkront
                using (var sqlTransaction = await _sqlConnection.BeginTransactionAsync()) // Brug BeginTransactionAsync
                {
                    // Før vi begynder at opdatere, tjek om der er nok på lager ved at bruge den nyeste opdaterede Redis-cache
                    await _redisCacheService.CheckStockAvailability(bookIds); // Asynkron ventning på lagerkontrol

                    // Opdater ClosedOrders-tabellen med den nye ordre
                    string insertOrderQuery = @"
                        INSERT INTO ClosedOrders (OrderId, CustomerId, DateTime, PriceTotal) 
                        VALUES (@OrderId, @CustomerId, @DateTime, @PriceTotal)";
                    using (var command = new SqlCommand(insertOrderQuery, _sqlConnection, (SqlTransaction)sqlTransaction)) // Asynkron transaktion
                    {
                        command.Parameters.AddWithValue("@OrderId", orderId);
                        command.Parameters.AddWithValue("@CustomerId", customerId);
                        command.Parameters.AddWithValue("@DateTime", DateTime.Now);
                        command.Parameters.AddWithValue("@PriceTotal", priceTotal);
                        await command.ExecuteNonQueryAsync(); // Asynkron udførelse af kommando
                    }

                    // Opdater Inventory-tabellen, så lagerbeholdningen reduceres
                    foreach (var bookId in bookIds)
                    {
                        string updateInventoryQuery = @"
                                UPDATE Inventory SET StockQuantity = StockQuantity - 1 
                                WHERE BookId = @BookId AND StockQuantity > 0";
                        using (var command = new SqlCommand(updateInventoryQuery, _sqlConnection, (SqlTransaction)sqlTransaction)) // Asynkron transaktion
                        {
                            command.Parameters.AddWithValue("@BookId", bookId);
                            await command.ExecuteNonQueryAsync(); // Asynkron udførelse af kommando
                        }
                    }

                    // Commit transaktionen, hvis alt er gået godt
                    await sqlTransaction.CommitAsync(); // Asynkron commit
                }
            }
            catch (Exception ex)
            {
                // Hvis en fejl opstår, rollback transaktionen
                Console.WriteLine($"SQL transaktion fejlede: {ex.Message}");
                throw; // Genkaster undtagelsen
            }
            finally
            {
                // Sørg for at lukke forbindelsen efter transaktionen
                if (_sqlConnection.State != System.Data.ConnectionState.Closed)
                {
                    await _sqlConnection.CloseAsync(); // Asynkron lukning af forbindelse
                }
            }
        }
        */


        /*
        //public void UpdateSQLDatabase(Guid orderId, int customerId, decimal priceTotal, List<int> bookIds)
        public Task UpdateSQLDatabase(Guid orderId, int customerId, decimal priceTotal, List<int> bookIds)
        {
            Console.WriteLine("test: bliver UpdateSQLDatabase kaldt");

            try
            {
                // Sørg for at forbindelsen er åben
                if (_sqlConnection.State != System.Data.ConnectionState.Open)
                {
                    _sqlConnection.Open(); // Åbn forbindelsen, hvis den ikke allerede er åben
                }

                // Start transaktionen synkront på den åbne forbindelse
                using (var sqlTransaction = _sqlConnection.BeginTransaction()) // Brug BeginTransaction synkront
                {
                    // Før vi begynder at opdatere, tjek om der er nok på lager ved at bruge den nyeste opdaterede Redis-cache
                    _redisCacheService.CheckStockAvailability(bookIds).Wait(); // Brug synkron ventning her

                    // Opdater ClosedOrders-tabellen med den nye ordre
                    string insertOrderQuery = @"
                                INSERT INTO ClosedOrders (OrderId, CustomerId, DateTime, PriceTotal) 
                                VALUES (@OrderId, @CustomerId, @DateTime, @PriceTotal)";
                    using (var command = new SqlCommand(insertOrderQuery, _sqlConnection, sqlTransaction)) // Brug den synkrone transaktion
                    {
                        command.Parameters.AddWithValue("@OrderId", orderId);
                        command.Parameters.AddWithValue("@CustomerId", customerId);
                        command.Parameters.AddWithValue("@DateTime", DateTime.Now);
                        command.Parameters.AddWithValue("@PriceTotal", priceTotal);
                        command.ExecuteNonQuery(); // Synkron udførelse af kommando
                    }

                    // Opdater Inventory-tabellen, så lagerbeholdningen reduceres
                    foreach (var bookId in bookIds)
                    {
                        string updateInventoryQuery = @"
                                        UPDATE Inventory SET StockQuantity = StockQuantity - 1 
                                        WHERE BookId = @BookId AND StockQuantity > 0";
                        using (var command = new SqlCommand(updateInventoryQuery, _sqlConnection, sqlTransaction)) // Brug den synkrone transaktion her også
                        {
                            command.Parameters.AddWithValue("@BookId", bookId);
                            command.ExecuteNonQuery(); // Synkron udførelse af kommando
                        }
                    }

                    // Commit transaktionen, hvis alt er gået godt
                    sqlTransaction.Commit();
                }
            }
            catch (Exception ex)
            {
                // Hvis en fejl opstår, rollback transaktionen
                Console.WriteLine($"SQL transaktion fejlede: {ex.Message}");
                // Rollback vil ske automatisk i "using"-blokken, hvis en exception opstår
                throw; // Genkaster undtagelsen for at indikere fejl
            }
            finally
            {
                // Sørg for at lukke forbindelsen efter transaktionen
                if (_sqlConnection.State != System.Data.ConnectionState.Closed)
                {
                    _sqlConnection.Close(); // Luk forbindelsen synkront
                }
            }

            // da ænret fra void til task skal have et output
            return Task.CompletedTask;
        }
        */


        /*
        public async Task UpdateSQLDatabase(Guid orderId, int customerId, decimal priceTotal, List<int> bookIds)
        {
            Console.WriteLine("test: bliver UpdateSQLDatabase kaldt");

            DbTransaction transaction = null; // Brug DbTransaction i stedet for SqlTransaction

            try
            {
                // Sørg for at forbindelsen er åben
                if (_sqlConnection.State != System.Data.ConnectionState.Open)
                {
                    await _sqlConnection.OpenAsync(); // Åbn forbindelsen, hvis den ikke allerede er åben
                }

                // Start transaktionen på den åbne forbindelse
                transaction = await _sqlConnection.BeginTransactionAsync(); // BeginTransactionAsync returnerer DbTransaction

                // Før vi begynder at opdatere, tjek om der er nok på lager ved at bruge den nyeste opdaterede Redis-cache
                await _redisCacheService.CheckStockAvailability(bookIds);

                // Opdater ClosedOrders-tabellen med den nye ordre
                string insertOrderQuery = @"
                            INSERT INTO ClosedOrders (OrderId, CustomerId, DateTime, PriceTotal) 
                            VALUES (@OrderId, @CustomerId, @DateTime, @PriceTotal)";
                using (var command = new SqlCommand(insertOrderQuery, _sqlConnection, (SqlTransaction)transaction)) // Cast transaction til SqlTransaction
                {
                    command.Parameters.AddWithValue("@OrderId", orderId);
                    command.Parameters.AddWithValue("@CustomerId", customerId);
                    command.Parameters.AddWithValue("@DateTime", DateTime.Now);
                    command.Parameters.AddWithValue("@PriceTotal", priceTotal);
                    await command.ExecuteNonQueryAsync();
                }

                // Opdater Inventory-tabellen, så lagerbeholdningen reduceres
                foreach (var bookId in bookIds)
                {
                    string updateInventoryQuery = @"
                                    UPDATE Inventory SET StockQuantity = StockQuantity - 1 
                                    WHERE BookId = @BookId AND StockQuantity > 0";
                    using (var command = new SqlCommand(updateInventoryQuery, _sqlConnection, (SqlTransaction)transaction)) // Cast transaction her også
                    {
                        command.Parameters.AddWithValue("@BookId", bookId);
                        await command.ExecuteNonQueryAsync();
                    }
                }

                // Commit transaktionen, hvis alt er gået godt
                await transaction.CommitAsync(); // Asynkron commit
            }
            catch (Exception ex)
            {
                // Hvis en fejl opstår, rollback transaktionen
                Console.WriteLine($"SQL transaktion fejlede: {ex.Message}");
                if (transaction != null)
                {
                    await transaction.RollbackAsync(); // Asynkron rollback
                }
                throw; // Genkaster undtagelsen for at indikere fejl
            }
            finally
            {
                // Sørg for at lukke forbindelsen efter transaktionen
                if (_sqlConnection.State != System.Data.ConnectionState.Closed)
                {
                    await _sqlConnection.CloseAsync(); // Asynkron lukning af forbindelse
                }
            }
        }
        */


        /*
        public async Task UpdateSQLDatabase(Guid orderId, int customerId, decimal priceTotal, List<int> bookIds)
        {
            Console.WriteLine("test: bliver UpdateSQLDatabase kaldt");

            SqlTransaction transaction = null;

            try
            {
                // Sørg for at forbindelsen er åben
                if (_sqlConnection.State != System.Data.ConnectionState.Open)
                {
                    await _sqlConnection.OpenAsync(); // Åbn forbindelsen, hvis den ikke allerede er åben
                }

                // Start transaktionen på den åbne forbindelse
                transaction = await _sqlConnection.BeginTransactionAsync();
                

                // Start transaktionen på den åbne forbindelse (synkront)
                //transaction = _sqlConnection.BeginTransaction(); // Brug BeginTransaction i stedet for BeginTransactionAsync


                // Før vi begynder at opdatere, tjek om der er nok på lager ved at bruge den nyeste opdaterede Redis-cache
                await _redisCacheService.CheckStockAvailability(bookIds);

                // Opdater ClosedOrders-tabellen med den nye ordre
                string insertOrderQuery = @"
                                    INSERT INTO ClosedOrders (OrderId, CustomerId, DateTime, PriceTotal) 
                                    VALUES (@OrderId, @CustomerId, @DateTime, @PriceTotal)";
                using (var command = new SqlCommand(insertOrderQuery, _sqlConnection, transaction))
                {
                    command.Parameters.AddWithValue("@OrderId", orderId);
                    command.Parameters.AddWithValue("@CustomerId", customerId);
                    command.Parameters.AddWithValue("@DateTime", DateTime.Now);
                    command.Parameters.AddWithValue("@PriceTotal", priceTotal);
                    await command.ExecuteNonQueryAsync();
                }

                // Opdater Inventory-tabellen, så lagerbeholdningen reduceres
                foreach (var bookId in bookIds)
                {
                    string updateInventoryQuery = @"
                                            UPDATE Inventory SET StockQuantity = StockQuantity - 1 
                                            WHERE BookId = @BookId AND StockQuantity > 0";
                    using (var command = new SqlCommand(updateInventoryQuery, _sqlConnection, transaction))
                    {
                        command.Parameters.AddWithValue("@BookId", bookId);
                        await command.ExecuteNonQueryAsync();
                    }
                }

                // Commit transaktionen, hvis alt er gået godt
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                // Hvis en fejl opstår, rollback transaktionen
                Console.WriteLine($"SQL transaktion fejlede: {ex.Message}");
                if (transaction != null)
                {
                    await transaction.RollbackAsync(); // Sørg for at rulle transaktionen tilbage på fejl
                }
                throw; // Genkaster undtagelsen for at indikere fejl
            }
            finally
            {
                // Sørg for at lukke forbindelsen efter transaktionen
                if (_sqlConnection.State != System.Data.ConnectionState.Closed)
                {
                    await _sqlConnection.CloseAsync();
                }
            }
        }
        */
        /*
        //public async Task UpdateSQLDatabase(SqlConnection connection, SqlTransaction transaction, Guid orderId, int customerId, decimal totalPrice, List<int> bookIds)
        //public async Task UpdateSQLDatabase(SqlConnection connection, SqlTransaction transaction, Guid orderId, int customerId, decimal priceTotal, List<int> bookIds)
        //public async Task UpdateSQLDatabase(SqlConnection connection, SqlTransaction transaction, ConnectionMultiplexer redis, Guid orderId, int customerId, decimal priceTotal, List<int> bookIds)
        public async Task UpdateSQLDatabase(Guid orderId, int customerId, decimal priceTotal, List<int> bookIds)
        {
            Console.WriteLine("test: bliver UpdateSQLDatabase kaldt");

            try
            {
                // Før vi begynder at opdatere, tjek om der er nok på lager ud fra den nyeste opdaterede Redis cache
                //await CheckStockAvailability(redis, bookIds); // Kald lagercheck-funktionen
                await _redisCacheService.CheckStockAvailability(bookIds);
                //await _redisCacheService.CheckStockAvailability(redis, bookIds); // Kald lagercheck-funktionen



                // Opdater ClosedOrders tabellen med den nye ordre
                //string insertOrderQuery = @"
                //                      INSERT INTO ClosedOrders (OrderId, CustomerId, DateTime, TotalPrice) 
                //                    VALUES (@OrderId, @CustomerId, @DateTime, @TotalPrice)";
                string insertOrderQuery = @"
                                        INSERT INTO ClosedOrders (OrderId, CustomerId, DateTime, PriceTotal) 
                                        VALUES (@OrderId, @CustomerId, @DateTime, @priceTotal)";
                //using (var command = new SqlCommand(insertOrderQuery, connection, transaction))
                using (var command = new SqlCommand(insertOrderQuery, _sqlConnection))
                {
                    command.Parameters.AddWithValue("@OrderId", orderId);
                    command.Parameters.AddWithValue("@CustomerId", customerId);
                    command.Parameters.AddWithValue("@DateTime", DateTime.Now);
                    //command.Parameters.AddWithValue("@TotalPrice", totalPrice);
                    command.Parameters.AddWithValue("@PriceTotal", priceTotal);
                    await command.ExecuteNonQueryAsync();
                }

                // Opdater Inventory-tabellen, så lagerbeholdningen reduceres
                foreach (var bookId in bookIds)
                {
                    string updateInventoryQuery = @"
                                                UPDATE Inventory SET StockQuantity = StockQuantity - 1 
                                                WHERE BookId = @BookId AND StockQuantity > 0";
                    //using (var command = new SqlCommand(updateInventoryQuery, connection, transaction))
                    using (var command = new SqlCommand(updateInventoryQuery, _sqlConnection))
                    {
                        command.Parameters.AddWithValue("@BookId", bookId);
                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SQL transaction failed: {ex.Message}");
                throw; // Genkaster undtagelsen for at sikre rollback i kaldende metode
            }
        }
        */

        /* forsøg at flyt til rediscacheservice for at undgår cirkulære afhængigheder
        //public async Task<Dictionary<int, int>> GetNewStockQuantities(SqlConnection connection)
        public async Task<Dictionary<int, int>> GetNewStockQuantities()
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
        */



        //public async Task<(Dictionary<int, decimal> totalPrices, List<decimal> individualPrices)> GetBookPricesAsync(SqlConnection sqlConnection, List<int> bookIds)
        public async Task<(Dictionary<int, decimal> totalPrices, List<decimal> individualPrices)> GetBookPricesAsync(List<int> bookIds) // Henter bog priser fra SQL og beregner prisen pr bog ganget med antal gange står i ordren
        {
            var bookPrices = new Dictionary<int, decimal>(); // Dictionary til at gemme priserne
            var individualPrices = new List<decimal>(); // Liste til at gemme individuelle bogpriser
            var bookQuantities = bookIds
                .GroupBy(bookId => bookId)
                .ToDictionary(g => g.Key, g => g.Count()); // Tæller hvor mange gange hver bookId optræder i ordren

            // Opbyg SQL-forespørgslen til at hente priserne for de angivne bookIds
            var query = "SELECT BookId, Price FROM Books WHERE BookId IN (" + string.Join(",", bookIds.Select((_, index) => "@BookId" + index)) + ")";


            //tilføjet
            // Sørg for at forbindelsen er åben, før kommandoen køres
            if (_sqlConnection.State != System.Data.ConnectionState.Open)
            {
                await _sqlConnection.OpenAsync(); // Åbn forbindelsen hvis den ikke allerede er åben
            }



            // Opret en SQL-kommando
            //using (var command = new SqlCommand(query, sqlConnection))
            using (var command = new SqlCommand(query, _sqlConnection))
            {
                // Tilføj hver bookId som en parameter
                for (int i = 0; i < bookIds.Count; i++)
                {
                    command.Parameters.AddWithValue("@BookId" + i, bookIds[i]);
                }

                // Kør forespørgslen
                
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        int bookId = reader.GetInt32(0); // BookId er den første kolonne
                        decimal price = reader.GetDecimal(1); // Price er den anden kolonne

                        // Gem prisen for denne bookId i dictionary
                        if (!bookPrices.ContainsKey(bookId))
                        {
                            bookPrices[bookId] = price;
                        }

                        // Tilføj prisen til individualPrices, for hver bog vi har i ordren (inklusive flere kopier af samme bog)
                        int quantity = bookQuantities[bookId];
                        for (int i = 0; i < quantity; i++)
                        {
                            individualPrices.Add(price);
                        }
                    }
                }


                /*
                using (var connection = new SqlConnection(_sqlConnection.ConnectionString))
                {
                    connection.Open(); // Åbn forbindelsen
                    using (SqlCommand cmd = new SqlCommand(query, connection))
                    {
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                int bookId = reader.GetInt32(0); // BookId er den første kolonne
                                decimal price = reader.GetDecimal(1); // Price er den anden kolonne

                                // Gem prisen for denne bookId i dictionary
                                if (!bookPrices.ContainsKey(bookId))
                                {
                                    bookPrices[bookId] = price;
                                }

                                // Tilføj prisen til individualPrices, for hver bog vi har i ordren (inklusive flere kopier af samme bog)
                                int quantity = bookQuantities[bookId];
                                for (int i = 0; i < quantity; i++)
                                {
                                    individualPrices.Add(price);
                                }
                            }
                        }

                    }
                }
                */
            }

            // Opret en dictionary over den samlede pris for hver bog baseret på hvor mange gange den er blevet købt
            var totalPrices = new Dictionary<int, decimal>();

            foreach (var bookId in bookQuantities.Keys)
            {
                decimal price = bookPrices[bookId];
                totalPrices[bookId] = price * bookQuantities[bookId];
            }

            // Returner både totalPrices og individualPrices
            return (totalPrices, individualPrices);
        }


        /*
        public async Task<List<int>> CheckStockAvailability(ConnectionMultiplexer redis, List<int> bookIds) // skal flyttes til rediscacheservice?
        {
            Console.WriteLine("test: bliver CheckStockAvailability kaldt?");

            // from the inputtet list bookIds count the number of times each unique bookId is written and store in this dict
            
            //var bookQuantities = bookIds
            var orderBookQuantities = bookIds
                .GroupBy(bookId => bookId)
                .ToDictionary(g => g.Key, g => g.Count()); // Tæller hvor mange gange hver bookId optræder i ordren (dette er for ordren, ikke i sql db eller redis cache (tror jeg)

            var insufficientStock = new List<int>(); // Liste over bøger med utilstrækkelig lagerbeholdning

            var db = redis.GetDatabase();


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
        */
    }
}
