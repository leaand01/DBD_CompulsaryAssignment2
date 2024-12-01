using MongoDB.Driver;
using OnlineBookingstore;
using OnlineBookingstore.MongoDB;
using PopulateSqlTables;
using Microsoft.Extensions.DependencyInjection;
using OnlineBookstore.Config;


class Program
{
    static async Task Main(string[] args)
    {
        // Opret og konfigurer alle forbindelser og serviceklasser én gang
        var serviceProvider = ConfigureServices.Configure();
        
        // Hent de oprettede services
        var sqlService= serviceProvider.GetService<SQLService>(); // Dependency Inversion/Injection (DI): Hent SqlService fra containeren
        var redisCacheService = serviceProvider.GetService<RedisCacheService>();
        var createClosedOrderService = serviceProvider.GetService<CreateClosedOrderDetails>();

        var initialize = serviceProvider.GetService<Initialize>();
        var createCustomer = serviceProvider.GetService<CreateCustomers>();
        var createBook = serviceProvider.GetService<CreateBooks>();
        var createInventory = serviceProvider.GetService<CreateInventory>();
        var print = serviceProvider.GetService<Print>();


        // Opret forbindelse til MongoDB
        // pga af cirkulære refercen mellem SqlServices og RedisCacheService.cs flyttede jeg funktion fra SqlService til RedisCacheService, men kunne ikke få det til at virke hvis 
        // benyttede DI for CreateClosedOrderDetails, hvor definerede afhængigheder af connections og databaser i konstruktøren. Derfor denne løsning.
        string mongoConnectionString = "mongodb+srv://leaand01:y9VCOHTjgL4CrDYl@cluster0.wjrfw.mongodb.net/";
        var mongoClient = new MongoClient(mongoConnectionString); // objekt med forbineldse til mongoDB
        var mongoDB = mongoClient.GetDatabase("OnlineBookstore"); // Adgang til specifik DB - OnlineBookstore


        try
        {
            if (initialize.IsTableEmpty("Customers"))
            {
                createCustomer.CreateCustomer("customer1@email.com");
                createCustomer.CreateCustomer("customer2@email.com");
                createCustomer.CreateCustomer("customer3@email.com");
                Console.WriteLine("Customers have been created.");
            }

            if (initialize.IsTableEmpty("Books"))
            {
                createBook.CreateBook("title1", "author1", "fiction", 100);
                createBook.CreateBook("title2", "author1", "Thriller", 150);
                createBook.CreateBook("title3", "author2", "fiction", 180);
                Console.WriteLine("Books have been created.");
            }

            if (initialize.IsTableEmpty("Inventory"))
            {
                createInventory.InitialiseInventory();
                Console.WriteLine("Inventory has been initialized.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred while initializing: {ex.Message}");
            throw;
        }

        print.AllCustomers();
        print.AllBooks();

        
        // Opdater Redis med nyeste lagerstatus direkte fra SQL
        await redisCacheService.UpdateRedisCacheUsingSqlInventoryTabel();

        // Eksempel på købsordrer forklaret: kunde med customerId=1 køber 1stk af bookId=1, 1stk af bookId=2 og 2stk af bookId=3
        int customerId = 1;
        var bookIds = new List<int> { 1, 2, 3, 3 };
        await createClosedOrderService.HandleOrder(mongoDB, bookIds, customerId);
        
        // Der kan tilføjes flere købsordrer her (eller ændres i ovenstående eksempel). Pr default har sat start inventory til 10 stk af hver bog, hvor der er 3 forskellige bøger.
        // Hvis en købsordrer lyder på mere end inventory bliver ordren annuleret og en exception bliver thrown.
    }
}
