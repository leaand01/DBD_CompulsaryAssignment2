using Microsoft.Data.SqlClient;
using OnlineBookingstore;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
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
            string redisConnectionString = "localhost"; // Redis connection string

            // Angiv GUID-repræsentationen ved hjælp af en tilpasset BsonSerializer
            BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));

            // Opret forbindelser
            var sqlConnection = new SqlConnection(sqlConnectionString);
            //var mongoClientConnection = new MongoClient(mongoConnectionString);
            var redisConnection = ConnectionMultiplexer.Connect(redisConnectionString);


            // Registrér MongoClient separat som Singleton
            // forsøg. tilføjet disse to
            
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
            //serviceCollection.AddSingleton(mongoClientConnection.GetDatabase("OnlineBookstore"));
            serviceCollection.AddSingleton(redisConnection);

            // Opret og registrer dine serviceklasser, der bruger disse objekter
            serviceCollection.AddSingleton<SQLService>();
            serviceCollection.AddSingleton<RedisCacheService>();
            serviceCollection.AddSingleton<CreateClosedOrderDetails>();

            // Registrer Initialize-klasse i DI-containeren
            serviceCollection.AddTransient<Initialize>();

            // Registrer CreateCustomer-klasse i DI-containeren
            serviceCollection.AddTransient<CreateCustomers>();

            // Registrer CreateBooks-klasse i DI-containeren
            serviceCollection.AddTransient<CreateBooks>();

            // Registrer CreateInventory-klasse i DI-containeren
            serviceCollection.AddTransient<CreateInventory>();

            // Registrer Print-klasse i DI-containeren
            serviceCollection.AddTransient<Print>();

            // Returner ServiceProvider som kan håndtere afhængighederne
            return serviceCollection.BuildServiceProvider();
        }
    }

}
