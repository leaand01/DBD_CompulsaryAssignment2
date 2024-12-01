-- Shema Migrations 001 - 005 in OnlineBookstore
-- Important before able to run this script: go to Query in menu and select SQLCMD Mode in order to be able to execute lines starting with :r
-- (when selected lines starting with :r are highlighted)

-- create OnlineBookstore db if do not exist
:r "C:\easv\Databases for Developers\Opgaver\DBD_CompulsaryAssigmnemt2\001_create_local_db_OnlineBookstore.sql"
go

use OnlineBookstore;
go

-- create table customers
:r "C:\easv\Databases for Developers\Opgaver\DBD_CompulsaryAssigmnemt2\002_create_table_customers.sql"
go

:r "C:\easv\Databases for Developers\Opgaver\DBD_CompulsaryAssigmnemt2\003_create_table_books.sql"
go

:r "C:\easv\Databases for Developers\Opgaver\DBD_CompulsaryAssigmnemt2\004_create table_inventory.sql"
go

:r "C:\easv\Databases for Developers\Opgaver\DBD_CompulsaryAssigmnemt2\005_create_table_closedOrders.sql"
go
