/*
using MongoDB.Driver;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OnlineBookingstore
{
    public class MongoDBService
    {
        private readonly IMongoDatabase _mongoDB;

        public MongoDBService(IMongoDatabase mongoDB)
        {
            _mongoDB = mongoDB;
        }

        public async Task CreateClosedOrderDetailAsync(Guid orderId, List<int> bookIds, List<decimal> prices)
        {
            var collection = _mongoDB.GetCollection<ClosedOrderDetail>("ClosedOrderDetails");

            var orderDetail = new ClosedOrderDetail
            {
                OrderId = orderId,
                BookIds = bookIds,
                Prices = prices
            };

            await collection.InsertOneAsync(orderDetail);
            Console.WriteLine($"Order {orderDetail.OrderId} has been added to MongoDB.");
        }
    }
}
*/