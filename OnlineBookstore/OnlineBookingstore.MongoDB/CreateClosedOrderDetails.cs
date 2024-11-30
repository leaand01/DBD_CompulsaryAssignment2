using MongoDB.Driver;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Data.SqlClient;
using StackExchange.Redis;
using Microsoft.Data.SqlClient;
using OnlineBookstore.Services;
using System.Diagnostics;


namespace OnlineBookingstore.MongoDB
{
    public class CreateClosedOrderDetails
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

        /*
        private readonly SqlConnection _sqlConnection;
        //private readonly IMongoDatabase _mongoConnection;
        private readonly IMongoClient _mongoClient;
        private readonly IMongoDatabase _mongoDb;
        private readonly ConnectionMultiplexer _redisConnection;
        private readonly SQLService _sqlService;
        private readonly RedisCacheService _redisService;

        // Constructor injection af både SqlConnection og MongoClient (MongoDatabase)
        //public CreateClosedOrderDetails(SqlConnection sqlConnection, MongoClient mongoConnection, ConnectionMultiplexer redisConnection, SQLService sqlService, RedisCacheService redisService)
        //public CreateClosedOrderDetails(SqlConnection sqlConnection, IMongoDatabase database, ConnectionMultiplexer redisConnection, SQLService sqlService, RedisCacheService redisService)
        //public CreateClosedOrderDetails(SqlConnection sqlConnection, IMongoClient mongoClient, ConnectionMultiplexer redisConnection, SQLService sqlService, RedisCacheService redisService)
        public CreateClosedOrderDetails(SqlConnection sqlConnection, IMongoClient mongoClient, IMongoDatabase mongoDb, ConnectionMultiplexer redisConnection, SQLService sqlService, RedisCacheService redisService)
        {
            _sqlConnection = sqlConnection;
            //_mongoConnection = mongoConnection.GetDatabase("OnlineBookstore"); // Hent den relevante database fra MongoClient. Ændrer navn til _mongoDb
            //_mongoClient = mongoClient;
            // Initialiser IMongoClient og hent databasen
            //_mongoClient = mongoClient.GetDatabase("OnlineBookstore"); // Ændre til at hente database fra mongoClient
            _mongoClient = mongoClient; // Ændre til at hente database fra mongoClient
            _mongoDb = mongoDb;


            _redisConnection = redisConnection; // Initialiser Redis-forbindelsen
            _sqlService = sqlService; // Initialiser SQLService
            _redisService = redisService; // Initialiser RedisCacheService
        }
        */



        public async Task CreateClosedOrderDetail(IMongoDatabase mongoDB, List<int> bookIds, List<decimal> prices, int customerId)
        //public async Task CreateClosedOrderDetail(List<int> bookIds, List<decimal> prices, int customerId)
        {
            // Start en session til MongoDB transaktioner
            using (var session = await mongoDB.Client.StartSessionAsync())
            //using (var session = await _mongoConnection.Client.StartSessionAsync())
            //using (var session = await _mongoClient.Client.StartSessionAsync())
            //using (var session = await _mongoClient.StartSessionAsync())
            {
                session.StartTransaction(); // Start transaktionen

                try
                {
                    // Opret et nyt ClosedOrderDetail objekt med de angivne data
                    var orderDetail = new ClosedOrderDetail
                    {
                        OrderId = Guid.NewGuid(),  // Generer en ny GUID for OrderId
                        BookIds = bookIds,
                        Prices = prices
                    };

                    // Hent samlingen "ClosedOrderDetails"
                    var collection = mongoDB.GetCollection<ClosedOrderDetail>("ClosedOrderDetails");
                    //var collection = _mongoConnection.GetCollection<ClosedOrderDetail>("ClosedOrderDetails");
                    //var collection = _mongoDb.GetCollection<ClosedOrderDetail>("ClosedOrderDetails"); // Brug _database til at få samlingen

                    // Indsæt ordren i MongoDB
                    await collection.InsertOneAsync(session, orderDetail);

                    /*
                    // Beregn totalprisen
                    //decimal totalPrice = 0;
                    decimal priceTotal = 0;
                    foreach (var price in prices)
                    {
                        //totalPrice += price;
                        priceTotal += price;
                    }
                    */

                    // Herefter skal vi også opdatere den relationelle database og lageret
                    // SQL- og Inventory-opdateringer vil blive håndteret i det næste trin

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


        //public async Task HandleOrderAsync(IMongoDatabase mongoDB, SqlConnection sqlConnection, ConnectionMultiplexer redis,
        //                                  List<int> bookIds, int customerId,
        //                                SQLService sqlService, RedisCacheService redisService)
        //public async Task HandleOrderAsync(List<int> bookIds, int customerId, SQLService sqlService, RedisCacheService redisService)
        //public async Task HandleOrderAsync(List<int> bookIds, int customerId)
        public async Task HandleOrderAsync(IMongoDatabase mongoDB, List<int> bookIds, int customerId)
        {

            //Console.WriteLine("test: bliver HandleOrderAsync kaldt");

            try
            {
                // start med at tjekke inventory i redis cache, hvis der er for lidt bøger ift ordren så cancel/throw exception
                await _redisService.CheckStockAvailability(bookIds); // tjekker med redis cache om der er nok bøger på lager til at gennemføre ordren



                // Hent priserne for de købte bøger fra SQL-databasen
                // Hent priserne for de købte bøger fra SQL-databasen
                //var (totalPrices, individualPrices) = await _sqlService.GetBookPricesAsync(_sqlConnection, bookIds);
                var (totalPrices, individualPrices) = await _sqlService.GetBookPricesAsync(bookIds); // Henter bog priser fra SQL og beregner prisen pr bog ganget med antal gange står i ordren
                // totalPrices er en Dictionary<int, decimal> med samlede priser per bog (bookId -> totalPrice)
                // individualPrices er en List<decimal> med prisen på hver bog (prisen gentaget hvis bogen er købt flere gange)

                Console.WriteLine($"HandleOrderAsync lige kaldt GetBookPricesAsync - priserne det koster pr bog hvor tager højde for antal bøger købt: totalprice {totalPrices}, individualprice: {individualPrices}");

                //var prices = await sqlService.GetBookPricesAsync(sqlConnection, bookIds);
                //var (totalPrices, individualPrices) = await sqlService.GetBookPricesAsync(sqlConnection, bookIds);
                //var (totalPrices, individualPrices) = await sqlService.GetBookPricesAsync(_sqlConnection, bookIds);
                // hvor
                // totalPrices er en Dictionary<int, decimal> med samlede priser per bog (bookId -> totalPrice)
                // individualPrices er en List<decimal> med prisen på hver bog (prisen gentaget hvis bogen er købt flere gange)



                // Opret ordren i MongoDB
                // Opret ordren i MongoDB
                //await CreateClosedOrderDetail(bookIds, individualPrices, customerId);
                await CreateClosedOrderDetail(mongoDB, bookIds, individualPrices, customerId);


                // I stedet bør du hente det fra DI-containeren:
                //var createOrderService = serviceProvider.GetService<CreateClosedOrderDetails>();
                //var createOrderService = new CreateClosedOrderDetails();
                //await createOrderService.CreateClosedOrderDetail(mongoDB, bookIds, prices, customerId);
                //await createOrderService.CreateClosedOrderDetail(mongoDB, bookIds, individualPrices, customerId);
                //await createOrderService.CreateClosedOrderDetail(_mongoConnection, bookIds, individualPrices, customerId);

                // Beregn totalprisen
                //decimal totalPrice = prices.Sum();
                //decimal priceTotal = prices.Sum();
                // Beregn totalprisen ved at summere værdierne i dictionary'en
                decimal priceTotal = totalPrices.Values.Sum();


                // Brug SQL-servicen til at opdatere databasen
                //using (var sqlTransaction = sqlConnection.BeginTransaction())
                using (var sqlTransaction = _sqlConnection.BeginTransaction())
                {
                    Guid orderId = Guid.NewGuid(); // Generer et nyt GUID for ordren
                                                   //await sqlService.UpdateSQLDatabase(sqlConnection, sqlTransaction, orderId, customerId, priceTotal, bookIds);
                                                   //await sqlService.UpdateSQLDatabase(sqlConnection, sqlTransaction, redis, orderId, customerId, priceTotal, bookIds);

                    //await _sqlService.UpdateSQLDatabase(_sqlConnection, sqlTransaction, _redisConnection, orderId, customerId, priceTotal, bookIds);
                    //await _sqlService.UpdateSQLDatabase(orderId, customerId, priceTotal, bookIds);
                    // Videregiv transaktionen til UpdateSQLDatabase
                    await _sqlService.UpdateSQLDatabase(orderId, customerId, priceTotal, bookIds, sqlTransaction);

                    sqlTransaction.Commit(); // Bekræft SQL-transaktionen

                    //await sqlService.UpdateSQLDatabase(_sqlConnection, sqlTransaction, _redisConnection, orderId, customerId, priceTotal, bookIds);
                    //sqlTransaction.Commit(); // Bekræft SQL-transaktionen
                }

                // Brug Redis-servicen til at opdatere cachen
                //await redisService.UpdateRedisCache(redis, sqlConnection); // går ikke videre til næste linje før denne er færdig. Tilføjer await fordi UpdateRedisCache er en async funktion

                // Brug Redis-servicen til at opdatere cachen
                //await _redisService.UpdateRedisCache(_redisConnection, _sqlConnection);
                await _redisService.UpdateRedisCache();
                //await redisService.UpdateRedisCache(_redisConnection, _sqlConnection); // går ikke videre til næste linje før denne er færdig. Tilføjer await fordi UpdateRedisCache er en async funktion

                Console.WriteLine("Order handled successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while handling the order: {ex.Message}");
                throw; // Genkaster undtagelsen for at sikre rollback i kaldende metode
            }
        }

        /*
        public async Task HandleOrderAsync(IMongoDatabase mongoDB, SqlConnection sqlConnection, ConnectionMultiplexer redis,
                                            List<int> bookIds, List<decimal> prices, int customerId,
                                            SQLService sqlService, RedisCacheService redisService)
        {
            try
            {
                // Opret ordren i MongoDB
                var createOrderService = new CreateClosedOrderDetails();
                await createOrderService.CreateClosedOrderDetail(mongoDB, bookIds, prices, customerId);

                // Beregn totalprisen
                decimal totalPrice = prices.Sum();

                // Brug SQL-servicen til at opdatere databasen
                using (var sqlTransaction = sqlConnection.BeginTransaction())
                {
                    Guid orderId = Guid.NewGuid(); // Generer et nyt GUID for ordren
                    await sqlService.UpdateSQLDatabase(sqlConnection, sqlTransaction, orderId, customerId, totalPrice, bookIds);
                    sqlTransaction.Commit(); // Bekræft SQL-transaktionen
                }

                // Brug Redis-servicen til at opdatere cachen
                await redisService.UpdateRedisCache(redis, sqlConnection); // går ikke videre til næste linje før denne er færdig. Tilføjer await fordi UpdateRedisCache er en async funktion

                Console.WriteLine("Order handled successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while handling the order: {ex.Message}");
            }
        }
        */
    }
}
