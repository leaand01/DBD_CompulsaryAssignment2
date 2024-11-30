using Microsoft.Data.SqlClient;
using StackExchange.Redis;


namespace OnlineBookingstore
{
    public class SQLService
    {
        private readonly SqlConnection _sqlConnection;
        
        public SQLService(SqlConnection sqlConnection)
        {
            _sqlConnection = sqlConnection;
        }

        
        public async Task UpdateSQLDatabase(Guid orderId, int customerId, decimal priceTotal, List<int> bookIds, SqlTransaction sqlTransaction)
        {
            try
            {
                // Sørg for at forbindelsen er åben
                if (_sqlConnection.State != System.Data.ConnectionState.Open)
                {
                    await _sqlConnection.OpenAsync();
                }

                // Opdater SQL ClosedOrders-tabellen med den nye ordre
                string insertOrderQuery = @"
                                        INSERT INTO ClosedOrders (OrderId, CustomerId, DateTime, PriceTotal) 
                                        VALUES (@OrderId, @CustomerId, @DateTime, @PriceTotal)";
                using (var command = new SqlCommand(insertOrderQuery, _sqlConnection, sqlTransaction)) // Brug den eksisterende transaktion der er givet som input i funktionen
                {
                    command.Parameters.AddWithValue("@OrderId", orderId);
                    command.Parameters.AddWithValue("@CustomerId", customerId);
                    command.Parameters.AddWithValue("@DateTime", DateTime.Now);
                    command.Parameters.AddWithValue("@PriceTotal", priceTotal);
                    await command.ExecuteNonQueryAsync();
                }

                // Opdater SQL inventory-tabellen, så lagerbeholdningen reduceres ift. købsordren
                foreach (var bookId in bookIds)
                {
                    string updateInventoryQuery = @"
                                            UPDATE Inventory SET StockQuantity = StockQuantity - 1 
                                            WHERE BookId = @BookId AND StockQuantity > 0";  // ikke effektiv. Burde gruppere pr bookid så slipper for potentielt loop over samme bookid.
                    
                    using (var command = new SqlCommand(updateInventoryQuery, _sqlConnection, sqlTransaction)) // Brug eksisterende transaktion
                    {
                        command.Parameters.AddWithValue("@BookId", bookId);
                        await command.ExecuteNonQueryAsync();
                    }
                }
                await Task.CompletedTask; // Da funktion er en Task skal den have et output

            }
            catch (Exception ex)
            {
                Console.WriteLine($"SQL transaktion fejlede: {ex.Message}"); // Hvis en fejl opstår, rollback transaktionen
                throw; // Genkaster undtagelsen for at sikre rollback i kaldende metode
            }
        }


        // Henter bog priser fra SQL tabellen Books og beregner prisen pr bog ganget med antal gange står i købsordren
        public async Task<(Dictionary<int, decimal> totalPrices, List<decimal> individualPrices)> GetBookPricesAsync(List<int> bookIds) 
        {
            var bookPrices = new Dictionary<int, decimal>(); // Dictionary til at gemme priserne
            var individualPrices = new List<decimal>(); // Liste til at gemme individuelle bogpriser
            var bookQuantities = bookIds
                .GroupBy(bookId => bookId)
                .ToDictionary(g => g.Key, g => g.Count()); // Tæller hvor mange gange hvert bookId optræder i ordren

            var query = "SELECT BookId, Price FROM Books WHERE BookId IN (" + string.Join(",", bookIds.Select((_, index) => "@BookId" + index)) + ")";


            // Sørg for SQL- forbindelsen er åben (kan sikket gøres smartere)
            if (_sqlConnection.State != System.Data.ConnectionState.Open)
            {
                await _sqlConnection.OpenAsync();
            }

            // Opret en SQL-kommando
            using (var command = new SqlCommand(query, _sqlConnection))
            {
                // Tilføj hver bookId som en parameter
                for (int i = 0; i < bookIds.Count; i++)
                {
                    command.Parameters.AddWithValue("@BookId" + i, bookIds[i]);
                }

                // Kør sql query
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
            }

            // Opret en dictionary over den samlede pris for hver bog baseret på hvor mange gange den er i købsordren
            var totalPrices = new Dictionary<int, decimal>();

            foreach (var bookId in bookQuantities.Keys)
            {
                decimal price = bookPrices[bookId];
                totalPrices[bookId] = price * bookQuantities[bookId];
            }

            // Returner både totalPrices og individualPrices
            return (totalPrices, individualPrices);
        }
    }
}
