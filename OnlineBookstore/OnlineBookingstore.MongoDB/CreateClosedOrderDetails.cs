using MongoDB.Driver;
using Microsoft.Data.SqlClient;

namespace OnlineBookingstore.MongoDB
{
    public class CreateClosedOrderDetails // benyt DI vha constructor injection
    {
        private readonly SqlConnection _sqlConnection;
        private readonly SQLService _sqlService;
        private readonly RedisCacheService _redisService;

        public CreateClosedOrderDetails(SqlConnection sqlConnection, SQLService sqlService, RedisCacheService redisService)
        {
            _sqlConnection = sqlConnection;
            _sqlService = sqlService;
            _redisService = redisService;
        }


        // Bemærk har benyttet DI vha constructor injection for at undgå funktioner skulle have databaser, connections mv som input. Men kunne ikke få det til at virke her,
        // hvorfor der gives mongoDB som input
        public async Task CreateClosedOrderDetail(IMongoDatabase mongoDB, List<int> bookIds, List<decimal> prices, int customerId)
        {
            // Start en session til MongoDB transaktioner
            using (var session = await mongoDB.Client.StartSessionAsync())
            {
                session.StartTransaction(); // Start transaktionen - ift ACID principperne. Enten udføres alt uden problemer, ellers annuler alle handlinger i transaction.

                try
                {
                    // ClosedOrderDetail objekt der bliver gemt i mongoDB hvis transaction gennemføres
                    var orderDetail = new ClosedOrderDetail
                    {
                        OrderId = Guid.NewGuid(),  // Generer en ny GUID for OrderId (unikt OrderId)
                        BookIds = bookIds,
                        Prices = prices
                    };

                    // Indsæt ordren i MongoDB ClosedOrderDetails
                    var collection = mongoDB.GetCollection<ClosedOrderDetail>("ClosedOrderDetails");
                    await collection.InsertOneAsync(session, orderDetail);

                    // Bekræft transaktionen i MongoDB
                    await session.CommitTransactionAsync();
                    Console.WriteLine($"Order {orderDetail.OrderId} has been added to MongoDB.");
                }
                catch (Exception ex)
                {
                    // Hvis der er en fejl, rulles transaktionen tilbage
                    await session.AbortTransactionAsync();
                    Console.WriteLine($"Transaction failed: {ex.Message}");
                    throw;
                }
            }
        }


        public async Task HandleOrderAsync(IMongoDatabase mongoDB, List<int> bookIds, int customerId)
        {
            try
            {
                // Først tjek redis cache inventory ift købsordren: Hvis der ifg cache inventory er færre af en slags bøger end hvad ordren lyder på, annuleres ordren og der smides en exception
                await _redisService.CheckStockAvailability(bookIds); // Benyt redis cache da er hurtigere end at tjekken inventory tabellen i SQL

                // Hent priserne for bøgerne ordren lyder på fra SQL-databasen
                var (totalPrices, individualPrices) = await _sqlService.GetBookPricesAsync(bookIds); 
                // totalPrices er en Dictionary<int, decimal> med samlede priser per bog. Hvis fx ordren lyder på flere eksemplarer af den samme bog lægges dens pris sammen det tilsvarende antal gange
                // individualPrices er en List<decimal> med prisen på hver bog (prisen gentages i listen hvis ordren lyder på flere eksemplarer af bogen)

                // Opret ordren i MongoDB ClosedOrderDetails
                await CreateClosedOrderDetail(mongoDB, bookIds, individualPrices, customerId);

                // Beregn beregn den total pris for ordren. Denne bliver gemt i Sql tabellen ClosedOrders. Flere detaljer for ordren findes i ClosedOrderDetails i mongoDB
                decimal priceTotal = totalPrices.Values.Sum();


                // opret en transaction for at sikre alt-eller-intet lykkedes.
                using (var sqlTransaction = _sqlConnection.BeginTransaction())
                {
                    Guid orderId = Guid.NewGuid(); // Generer et nyt GUID for ordren

                    // Opdater SQL tabellerne ClosedOrders og Inventory baseret på købsordren    
                    await _sqlService.UpdateSQLDatabase(orderId, customerId, priceTotal, bookIds, sqlTransaction);
                    sqlTransaction.Commit();
                }

                // Opdater redis cache
                await _redisService.UpdateRedisCache(); // await sikrer vi ikke går videre til næste linje før denne er færdig (en async funktion)
                Console.WriteLine("Order handled successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while handling the order: {ex.Message}");
                throw; // Genkaster undtagelsen for at sikre rollback i kaldende metode
            }
        }
    }
}
