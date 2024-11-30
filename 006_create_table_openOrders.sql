if not exists (select * from INFORMATION_SCHEMA.TABLES where TABLE_NAME = 'OpenOrders' and TABLE_SCHEMA = 'dbo')
begin
	create table OpenOrders (
	OrderId int primary key identity(1, 1),
	BookId int not null,
	Price decimal(10, 2) not null,
	PriceTotal decimal(10, 2) not null,
	DiscountCode varchar(50),
	GiftWrapped bit,
	foreign key (BookId) references Books(BookId)
	);
end
