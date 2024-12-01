# Database design for onlineBookstore

**SQL tables**

Customers with columns: CustomerId, email

Books with columns: BookId, genre, Author, Title, Price (a books price)

Inventory with columns: InventoryId, BookId, stockQuantity, LastUpdated

ClosedOrders with columns: OrderId, CustomerId, datetime, PriceTotal

**Data to be stored in mongoDB**

ClosedOrderDetails with key-value pairs: OrderId (the same id as in SQL table ClosedOrders), BookId (list), Price (list), DiscountCode (optional), Giftwrapped (optional)

BookDetails with key-value pairs: BookId, Biography (optional), Other in series (optional), Reviews (optional), Blurb (optional)

BestSellers with key-value pairs: Id, datetime, BookId (list), genre (list), author (list), title (list)

**Data to be stored in redis cache**

OpenOrders (basket) with features: OrderId (identical to orderId in ClosedOrderDetails and ClosedOrders if transactions is successfull), BookId (list), Price (list), PriceTotal, DiscountCode (optional), Giftwrapped (optional)

Inventory

BookDetails of BestSellers

## Explanation of DB design
Customer information and basic book information rarely or never change making them ideal for the relational setup in SQL. Further an simple inventory overview is also stored in SQL as this information is static and one can quickly get a historical overview of the inventory for potential analysis. For each BookId updating the given books stockQantity and adding a timestamp for the time of the update allows us to always know when and how many of a given BookId were purchased (note however, in the simple implementation, the rows are just updated with a new timestamp and inventory level if an order is successful, i.e. the order is closed).
Furher, having this data stored in SQL we do not run into the 16MB limitation in mongoDB or RAM limitations of redis, so here we do not need to worry about the amount of available books and number of customers.

ClosedOrderDetails are stored in mongoDB as this setup is more flexible. Here the details of the order is more clears specified than in the SQL table ClosedOrders (which just summaries the total cost of the order), where one can see which books were purchased and what their price were. I chose to place this in mongoDB to add the flexibility og additional key-value pair such as e.g. DiscountCode or GiftWrapped. Most likely the onlineBookstore will have sales on the books from time to time or customers want their purchase wrapped for an additional cost as they are meant as presents. To allow for this flesiblity we store these sort of (optional) details in mongoDB, instead of in SQL as in this case we would have to include the optional features such as DiscountCode leading to redundanct code if the majority of books are sold without a discount.

Furthermore, details about a book is also stored in mongoDB, as associated information such as review, other in series, biography etc can vary. It is ideal for the flexible setup in mongoDB.

In cache we want to store relevant information that is conviently accessible fast in order to give the customers a better online experience. Therefore we want the inventory to be caches so the user can quickly see if the books of interes are in stock. OpenOrders (the basket) is kept in cache, only when the order is send and successful the OpenOrder will be deleted and instead a ClosedOrderDetails and ClosedORder in mongoDB and SQL will be created, respectively. Furthermore, the latest bestsellers are also stored in cache potentially increasing their sales. So this is of interest for the onlineBookstore owner, but also a very nice feature for the users/customers.

In general one needs to ensure redis cache is kept up to date and not overfilled. E.g. the least frequently used data searched upon in cache can be removed to free up space and improve speed and performance. If for some reason too much data is relevant to be stored in cache, on should consider only stored the most relevant information and retrieve it in batches.


# My minimal implementation

I have created the following SQL tables: Customers, Books, Inventory, ClosedOrders and inserted minimal data into Customers and Books and an initial inventory.

In mongoDB i only create ClosedOrderDetails.

In redis cache I only store lastest inventory. Thus when a purchas is made, if successful, the cache inventory is also updated.

in program.cs in project OnlineBookstore the function call createClosedOrderService.HandleOrder corresponds to having an open order and then pressing buy. This function will do the following in a transaction - ensuring it will either succeed and everything is updated correctly, or the order is cancelled:

- first it check with the cache inventory if the order is possible to make, if yes then proceed
- then ClosedOrderDetails is created and stored in the mongoDB
- then the SQl tables ClosedOrder and Inventory are updated based on the information of the order
- lastly cache is updated so it corresponds to the SQL inventory, ensuring the customer always sees the correct stock quantities.


When running OnlineBookstore.program.cs in VS all relevant information is printed in the console for a better overview.


## Setup of minimal implementation

- First you must create the SQL tables. Open SQL and rund the file "000_all schema migrations.sql". In the script is given an explanation of how to run it.
- Next you need to setup a redis server. You can download the installation here https://github.com/microsoftarchive/redis/releases. select a folder an unpack the files. Then open Windows Powershell and cd to the folder where you unpacked the files. Run the command .\redis-server.exe to start the redis server and do not close this while running the application. If you want to monitor the redis cache you can open another Powershell and cd to the same directory as before and run the command .\redis-cli.exe followed by the command monitor.
- Lastly open Visual Studio. In OnlineBookstore.program.cs in line 31 you need to specify your own connectionstring to mongoDB. In OnlineBookstore.Config.ConfigureService.cs in line 22-24 you also need to specify your own connection string to sql, mongo and redis respectively.
- set OnlineBookstore as the startup project in Visual Studio Solution Explorer.
  
You are now able to run the application. In the bottom of OnlineBookStore.Program.cs is given an example of an order. All relevant information is printed in the console when the program is running. Note The default inventory level is set very low. In the case of an order exceeding the stockQuantity of just a single book, the order will be cancelled and an exception will be thrown.

  
