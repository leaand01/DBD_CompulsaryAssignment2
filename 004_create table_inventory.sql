if not exists (select * from INFORMATION_SCHEMA.TABLES where TABLE_NAME = 'Inventory' and TABLE_SCHEMA = 'dbo')
begin
	create table Inventory (
	InventoryId int primary key identity(1, 1),
	BookId int not null,
	StockQuantity int not null,
	LastUpdated datetime not null
	foreign key (BookId) references Books(BookId) on delete cascade,
	);
end
