using System;
using Microsoft.Data.SqlClient;
using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using StackExchange.Redis;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using OnlineBookstore.Services;
using OnlineBookingstore;
using OnlineBookingstore.MongoDB;
using PopulateSqlTables;
using System.Transactions;
using MongoDB.Bson.Serialization.Serializers;
using Microsoft.Extensions.DependencyInjection;
using OnlineBookstore.Config;


class Program
{
    static async Task Main(string[] args)
    {
        // Opret og konfigurer alle forbindelser og serviceklasser én gang
        var serviceProvider = ConfigureServices.Configure(); // Kald den nye metode her
        

        // Opret og konfigurer alle forbindelser og serviceklasser én gang
        //var serviceProvider = ConfigureServices();

        // Hent de oprettede services
        var sqlService= serviceProvider.GetService<SQLService>(); // DI: Hent SqlConnection fra containeren
        var redisCacheService = serviceProvider.GetService<RedisCacheService>();
        var createClosedOrderService = serviceProvider.GetService<CreateClosedOrderDetails>();

        // Hent Initialize-objektet via DI
        var initialize = serviceProvider.GetService<Initialize>(); // DI: Hent Initialize fra containeren

        var createCustomer = serviceProvider.GetService<CreateCustomers>();
        var createBook = serviceProvider.GetService<CreateBooks>();
        var createInventory = serviceProvider.GetService<CreateInventory>();
        var print = serviceProvider.GetService<Print>();


        string mongoConnectionString = "mongodb+srv://leaand01:y9VCOHTjgL4CrDYl@cluster0.wjrfw.mongodb.net/";
        // Opret forbindelse til MongoDB
        var mongoClient = new MongoClient(mongoConnectionString); // objekt med forbineldse til mongoDB
        var mongoDB = mongoClient.GetDatabase("OnlineBookstore"); // Adgang til specifik DB - OnlineBookstore


        /* Eksempel
        // Kald den nødvendige metode, f.eks. HandleOrderAsync
        createClosedOrderService.HandleOrderAsync(bookIds, customerId).Wait();
        */

        /* forsøg start
        // Opret forbindelserne
        string sqlConnectionString = "Server=Kontoret;Database=OnlineBookstore;Trusted_Connection=True;TrustServerCertificate=True;"; // forbindelses streng
        string mongoConnectionString = "mongodb+srv://leaand01:y9VCOHTjgL4CrDYl@cluster0.wjrfw.mongodb.net/";
        string redisConnectionString = "localhost"; // Redis connection string

        // Opret forbindelse til MongoDB
        var mongoClient = new MongoClient(mongoConnectionString); // objekt med forbineldse til mongoDB
        var mongoDB = mongoClient.GetDatabase("OnlineBookstore"); // Adgang til specifik DB - OnlineBookstore

        // Angiv GUID-repræsentationen ved hjælp af en tilpasset BsonSerializer
        BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));

        // Opret Redis-forbindelse
        var redis = await ConnectionMultiplexer.ConnectAsync(redisConnectionString); //objekt der håndterer forbindelse til redis-server.


        // Opret SQLService objekt
        //var sqlService = new SQLService(sqlConnectionString, redisCacheService);  // SQLService til at opdatere ClosedOrders og Inventory
        // Opret RedisCacheService objekt
        var redisCacheService = new RedisCacheService(); // service klasse der bruger forbindelsen til redis og implementerer logik for at opdatere/ændre i cache
        //var redisCacheService = new RedisCacheService(sqlService);

        // Ryd Redis cache ved opstart
        await redisCacheService.ClearRedisCache(redis); // Sørg for at bruge 'await' her, da ClearRedisCache er asynkron
        //Console.WriteLine("Redis cache cleared.");


        // Opret serviceklasser for SQL, MongoDB og Redis
        var sqlConnection = new SqlConnection(sqlConnectionString); // objekt med forbindelse til sql
        await sqlConnection.OpenAsync();

        //var inventoryService = new InventoryService(sqlConnectionString, redis); // Opret InventoryService (håndterer opdatering af lagerbeholdning i SQL og Redis)


        */ //forsøg slut
        try
        {

            
            //if (Initialize.IsTableEmpty(sqlConnection, "Customers"))
            if (initialize.IsTableEmpty("Customers"))
            {
                createCustomer.CreateCustomer("customer1@email.com");
                createCustomer.CreateCustomer("customer2@email.com");
                createCustomer.CreateCustomer("customer3@email.com");
                //CreateCustomers.CreateCustomer(sqlConnection, "customer1@email.com");
                //CreateCustomers.CreateCustomer(sqlConnection, "customer2@email.com");
                //CreateCustomers.CreateCustomer(sqlConnection, "customer3@email.com");
                Console.WriteLine("Customers have been created.");
            }

            //if (Initialize.IsTableEmpty(sqlConnection, "Books"))
            if (initialize.IsTableEmpty("Books"))
            {
                createBook.CreateBook("title1", "author1", "fiction", 100);
                createBook.CreateBook("title2", "author1", "Thriller", 150);
                createBook.CreateBook("title3", "author2", "fiction", 180);
                //CreateBooks.CreateBook(sqlConnection, "title1", "author1", "fiction", 100);
                //CreateBooks.CreateBook(sqlConnection, "title2", "author1", "Thriller", 150);
                //CreateBooks.CreateBook(sqlConnection, "title3", "author2", "fiction", 180);
                Console.WriteLine("Books have been created.");
            }

            //if (Initialize.IsTableEmpty(sqlConnection, "Inventory"))
            if (initialize.IsTableEmpty("Inventory"))
            {
                createInventory.InitialiseInventory();
                //CreateInventory.InitialiseInventory(sqlConnection);
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

        //PrintCustomers.PrintAllCustomers(sqlConnectionString);
        //PrintBooks.PrintAllBooks(sqlConnectionString);

        // Opdater Redis med nyeste lagerstatus
        //var redisCacheService = new RedisCacheService();
        //await redisCacheService.UpdateRedisCache(redis, sqlConnection);  // burde ikke være nødvendig når benytter DI
        await redisCacheService.UpdateRedisCache();  // opdater cache med data direkte fra SQL



        // NÅET HERTIL: ift cache er der ikke nok bøger på lager det løgn. tjek sql inventory tabel og se hvordan bliver opdateret. lav et nyt kald af handleorderasyn men hvor der kun købes en bog for at holde det simpelt!!



        // her vil jeg gerne tilføje en ClosedOrderDetails hvor specificerer hvilke bøger der er købt (skal nok også specificerer brugernavn således at ordren kan knyttes til customerId i sql Customers tabellen. men når gør dette skal ACID principperne overholdes så jeg heller ikke risikere at sælge flere bøger end har på lager. dvs skal opdaterer ClosedOrders SQL tabellen, Inventory SQL tabellen samt opdatere redis cache
        //var sqlService = new SQLService(sqlConnectionString); // burde ikke være nødvendig når benytter DI //en serviceklasse der bruger sqlconnection til at interagerer med SQL-databasen, fx hente/indsætte/ændre data  // SQLService til at opdatere ClosedOrders og Inventory
        //var sqlService = new SQLService(sqlConnectionString, redisCacheService);  // SQLService til at opdatere ClosedOrders og Inventory
        //var createClosedOrderService = new CreateClosedOrderDetails(); // burde ikke være nødvendig når benytter DI // MongoDB service til at oprette ClosedOrderDetails



        
        int customerId = 1;
        var bookIds = new List<int> {2,3,3};
        await createClosedOrderService.HandleOrderAsync(mongoDB, bookIds, customerId);
        



        //await createClosedOrderService.HandleOrderAsync(mongoDB, sqlConnection, redis, bookIds, customerId, sqlService, redisCacheService);
        //await createClosedOrderService.HandleOrderAsync(mongoDB, sqlConnection, redis, bookIds, customerId, sqlService, redisCacheService);
        //await createClosedOrderService.HandleOrderAsync(bookIds, customerId);



        /*
        int customerId2 = 1;
        var bookIds2 = new List<int> { 1,1,1,2,3 };
        await createClosedOrderService.HandleOrderAsync(mongoDB, bookIds2, customerId2);
        */

        /*
        int customerId = 1;
        var bookIds = new List<int> {1, 2};
        await createClosedOrderService.HandleOrderAsync(mongoDB, sqlConnection, redis, bookIds, customerId, sqlService, redisCacheService);

        
        int customerId2 = 1;
        var bookIds2 = new List<int> {1,1};
        await createClosedOrderService.HandleOrderAsync(mongoDB, sqlConnection, redis, bookIds2, customerId2, sqlService, redisCacheService);
        
        
        int customerId3 = 1;
        var bookIds3 = new List<int> {1,1,1,1,1,1,1,1}; // expect this one to fails as not enouth in stock
        await createClosedOrderService.HandleOrderAsync(mongoDB, sqlConnection, redis, bookIds3, customerId3, sqlService, redisCacheService);
        */

    }


    /*
    // flyt evt til seperat script
    // Metode til at konfigurere alle services
    public static ServiceProvider ConfigureServices()
    {
        var serviceCollection = new ServiceCollection();

        // Definer forbindelsesstrenge
        string sqlConnectionString = "Server=Kontoret;Database=OnlineBookstore;Trusted_Connection=True;TrustServerCertificate=True;";
        string mongoConnectionString = "mongodb+srv://leaand01:y9VCOHTjgL4CrDYl@cluster0.wjrfw.mongodb.net/";
        string redisConnectionString = "localhost"; // Redis connection string

        // Angiv GUID-repræsentationen ved hjælp af en tilpasset BsonSerializer
        // Registrer GUID-serialisering (så MongoDB håndterer GUID korrekt)
        BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));


        // Opret forbindelser
        var sqlConnection = new SqlConnection(sqlConnectionString);
        var mongoClientConnection = new MongoClient(mongoConnectionString);
        var redisConnection = ConnectionMultiplexer.Connect(redisConnectionString);

        // Opret serviceobjekter og registrer dem i DI-containeren (DI = Dependency Injection)
        serviceCollection.AddSingleton(sqlConnection);
        serviceCollection.AddSingleton(mongoClientConnection.GetDatabase("OnlineBookstore"));
        serviceCollection.AddSingleton(redisConnection);

        // Opret og registrer din serviceklasse, der bruger disse objekter
        serviceCollection.AddSingleton<SQLService>();
        serviceCollection.AddSingleton<RedisCacheService>();
        serviceCollection.AddSingleton<CreateClosedOrderDetails>();
        //serviceCollection.AddSingleton<CreateClosedOrderService>();

        // Registrer Initialize-klasse i DI-containeren
        serviceCollection.AddTransient<Initialize>(); // DI: Initialize vil blive injiceret automatisk

        // Registrer CreateCustomer-klasse i DI-containeren
        serviceCollection.AddTransient<CreateCustomers>();

        // Registrer CreateBooks-klasse i DI-containeren
        serviceCollection.AddTransient<CreateBooks>();

        // Registrer CreateInventory-klasse i DI-containeren
        serviceCollection.AddTransient<CreateInventory>();

        // Registrer Print-klasse i DI-containeren
        serviceCollection.AddTransient<Print>(); // gjort printallcustomer og printbookstable overflødig så kan fjernes hvis virker
        


        // Returner ServiceProvider som kan håndtere afhængighederne
        return serviceCollection.BuildServiceProvider();
    }
    */
}
