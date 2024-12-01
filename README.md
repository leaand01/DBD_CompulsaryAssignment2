# Database design for onlineBookstore

**SQL tables**

Customers with columns: CustomerId, email

Books with columns: BookId, genre, Author, Title, Price (a books price)

Inventory with columns: InventoryId, BookId, stockQuantity, LastUpdated

ClosedOrders with columns: OrderId, CustomerId, datetime, PriceTotal

**Data to be stored in mongoDB**

ClosedOrderDetails with key-value pairs: OrderId, BookId (list), Price (list), DiscountCode (optional), Giftwrapped (optional)

BookDetails with key-value pairs: BookId, Biography (optional), Other in series (optional), Reviews (optional), Blurb (optional)

BestSellers with key-value pairs: Id, datetime, BookId (list), genre (list), author (list), title (list)

**Data to be stored in redis cache**

OpenOrders (basket) with features: OrderId, BookId (list), Price (list), PriceTotal, DiscountCode (optional), Giftwrapped (optional)

Inventory

BestSellers

BookDetails of BestSellers

Note, that features such as BookId, OrderID will be the same across all tables for cross-reference and easy lookup.


## Explanation of DB design
Customer information and basic book information rarely or never change making them ideal for the relational setup in SQL. Further an simple inventory overview is also stored in SQL as this information is static and one can quickly get a historical overview of the inventory for potential analysis. For each BookId updating the given books stockQantity and adding a timestamp for the time of the update allows us to always know when and how many of a given BookId were purchased (note however, in the simple implementation, the rows are just updated/overwritten with a new timestamp and inventory level if an order is successful, i.e. the order is closed).
Furher, having the data of these tables stored in SQL we do not run into the 16MB limitation in mongoDB or RAM limitations of redis, so here we do not need to worry about the amount of available books and number of customers.

ClosedOrderDetails are stored in mongoDB as this setup is more flexible. Here the details of the order is more clearly specified than in the SQL table ClosedOrders (which just summaries the total cost of the order). In ClosedOrderDetails one can see which books and how many were purchased and what their prices were. I chose to place this in mongoDB to add the flexibility of additional key-value pair such as e.g. DiscountCode or GiftWrapped. Most likely the onlineBookstore will have sales from time to time or customers want their purchase gift wrapped. To allow for this flexiblity of these optional details we choose mongoDB instead of an SQL table, as in the latter case optional features DiscountCode and GiftWrapped would have to be specified for all orders. This would just redundant information stored in SQL, as it may contain many null values (e.g. null values when the books are not on sale/giftwrapped).

BookDetails about a book is also stored in mongoDB, as associated information such as reviews, other in series, biography etc can vary. It is ideal for the flexible setup in mongoDB and the text inputs.

In cache we want to store relevant information that is conviently accessible fast in order to give the customers a better online experience. Therefore we want the inventory to be cached so customers can quickly see if the books of interes are in stock. OpenOrders (the basket) is also kept in cache as there is no need for it to be stored in a db until the order is successfully purchased, further this would also only slow down the process due to DB calls. Only when an order is send/purchased successfully the OpenOrder will be deleted from cache and a ClosedOrderDetails and ClosedORder in mongoDB and SQL will be created and stored, respectively. Furthermore, the latest bestsellers are also stored in cache potentially increasing their sales. This is of interest for the onlineBookstore owner, but also a very nice feature for the customers.
Assume we in cache have the top 100 bestseller within the latest 3month. In case the BookDetails of these bestsellers, together with all other information stored in cache, are taking up too much RAM space we could instead load the BookDetails in batches based on e.g. the top 30 best sellers or the best sellers most often searched for. In general, when cached information is updated is should replace existing data and not just be added to cache. This is to prevent using up all RAM which would negatively affect performance.


# My minimal implementation

I have created the following SQL tables: Customers, Books, Inventory, ClosedOrders and inserted minimal data into Customers and Books and an initial inventory. Insertion of data is done in Visual Studio in project OnlineBookstore.program.cs

In mongoDB i only create ClosedOrderDetails.

In redis cache I only store the lastest inventory. Thus when a purchas is made, if successful, the cache inventory is also updated based on the sql inventory table.

In Visual Studio project OnlineBookstore.program.cs the function call createClosedOrderService.HandleOrder corresponds to having an open order and then pressing buy. This function will do the following in a transaction:

- first it check with the cache inventory if the order is possible to make, if yes then proceed. The cache inventory is checked as this is faster than retrieving the information from SQL.
- then ClosedOrderDetails is created and stored in mongoDB
- then the SQl tables ClosedOrder and Inventory are updated based on the information of the order
- lastly cache is updated so it corresponds to the SQL inventory, ensuring the customer always sees the correct stock quantities.

Above steps are done in a transaction to ensuring it will either succeed and everything is updated correctly, or the order is cancelled. This is to ensure consistency according to the ACID-principles.

When running OnlineBookstore.program.cs in Visual Studio all relevant information is printed in the console for a better overview.


## Setup of minimal implementation

You must have a SQL server, MongoDB server and Redis server up and running.

- First you must create the SQL tables. Open SQL and run the file "000_all schema migrations.sql". In the script is given an explanation of how to run it.
- Next you need to setup a redis server. You can download the installation here https://github.com/microsoftarchive/redis/releases. Select a folder and unpack the files. Then open Windows Powershell and cd to the folder where you unpacked the files. Run the command .\redis-server.exe to start the redis server and do not close this while running the application. If you want to monitor the redis cache you can open another Powershell and cd to the same directory as before and run the command: .\redis-cli.exe followed by the command: monitor.
- Lastly open Visual Studio. In OnlineBookstore.program.cs in line 31 you need to specify your own connectionstring to your (local) mongoDB server. In OnlineBookstore.Config.ConfigureService.cs in line 22-24 you also need to specify your own connection strings to your (local) sql, mongo, and redis server, respectively.
- set OnlineBookstore as the startup project in Visual Studio Solution Explorer.
  
You are now able to run the application. At the bottom of OnlineBookStore.Program.cs is given an example of an order. All relevant information is printed in the console when the program is running. Note The default inventory level is set very low. In the case of an order exceeding the stockQuantity of just a single book, the order will be cancelled and an exception will be thrown.

  
