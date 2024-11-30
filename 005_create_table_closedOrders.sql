if not exists (select * from INFORMATION_SCHEMA.TABLES where TABLE_NAME = 'ClosedOrders' and TABLE_SCHEMA = 'dbo')
begin
	create table ClosedOrders (
	--OrderId int primary key identity(1, 1),
	OrderId uniqueidentifier primary key,  -- �ndret til denne i stedet for int s�ledes kan bruge automatisk generede Guid i VS.
	CustomerId int,
	Datetime datetime not null,
	PriceTotal decimal (10 ,2) not null,
	foreign key (CustomerId) references Customers(CustomerId)
	);
end
