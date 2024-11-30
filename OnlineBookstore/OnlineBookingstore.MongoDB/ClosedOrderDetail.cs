using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnlineBookingstore.MongoDB
{
    public class ClosedOrderDetail
    {
        [BsonElement("OrderId")]  // Bruger OrderId som navnet på feltet
        public Guid OrderId { get; set; }  // Brug en GUID som et unikt ID

        [BsonElement("BookIds")]
        public List<int> BookIds { get; set; }  // Liste af BookIds

        [BsonElement("Prices")]
        public List<decimal> Prices { get; set; }  // Liste af priser per bog
    }
}
