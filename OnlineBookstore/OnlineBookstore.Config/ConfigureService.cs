using Microsoft.Data.SqlClient;
using OnlineBookingstore;
using StackExchange.Redis;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson;
using MongoDB.Driver;
using PopulateSqlTables;
using OnlineBookingstore.MongoDB;


namespace OnlineBookstore.Config
{
    public static class ConfigureServices
    {
        public static ServiceProvider Configure()
        {
            var serviceCollection = new ServiceCollection();

            // Definer forbindelsesstrenge
            string sqlConnectionString = "Server=Kontoret;Database=OnlineBookstore;Trusted_Connection=True;TrustServerCertificate=True;";
            string mongoConnectionString = "mongodb+srv://leaand01:y9VCOHTjgL4CrDYl@cluster0.wjrfw.mongodb.net/";
            string redisConnectionString = "localhost";

            // Angiv GUID-repræsentationen ved hjælp af en tilpasset BsonSerializer
            BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));

            // Opret forbindelser
            var sqlConnection = new SqlConnection(sqlConnectionString);
            var redisConnection = ConnectionMultiplexer.Connect(redisConnectionString);

            serviceCollection.AddSingleton<MongoClient>(sp => 
            {
                return new MongoClient(mongoConnectionString);
            });

            // Registrér MongoDB-databasen separat
            serviceCollection.AddSingleton<IMongoDatabase>(sp =>
            {
                var mongoClient = sp.GetRequiredService<MongoClient>();
                return mongoClient.GetDatabase("OnlineBookstore");
            });
            

            // Opret serviceobjekter og registrer dem i DI-containeren (DI = Dependency Injection)
            serviceCollection.AddSingleton(sqlConnection);
            serviceCollection.AddSingleton(redisConnection);

            // Opret og registrer serviceklasserne
            serviceCollection.AddSingleton<SQLService>();
            serviceCollection.AddSingleton<RedisCacheService>();
            serviceCollection.AddSingleton<CreateClosedOrderDetails>();

            // Registrer følgende klasser i DI-containeren
            serviceCollection.AddTransient<Initialize>();
            serviceCollection.AddTransient<CreateCustomers>();
            serviceCollection.AddTransient<CreateBooks>();
            serviceCollection.AddTransient<CreateInventory>();
            serviceCollection.AddTransient<Print>();

            // Returner ServiceProvider som håndterer afhængighederne
            return serviceCollection.BuildServiceProvider();
        }
    }
}
