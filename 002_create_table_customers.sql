-- Create table Customers if do not exist
if not exists (select * from INFORMATION_SCHEMA.TABLES where TABLE_NAME = 'Customers' and TABLE_SCHEMA = 'dbo')
begin
	create table Customers (
	CustomerId int primary key identity(1, 1),
	email varchar(100) not null
	);
end
