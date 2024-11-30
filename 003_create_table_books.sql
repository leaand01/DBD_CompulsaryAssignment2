if not exists (select * from INFORMATION_SCHEMA.TABLES where TABLE_NAME = 'Books' and TABLE_SCHEMA = 'dbo')
begin
	create table Books (
	BookId int primary key identity(1, 1),
	Title varchar(255) not null,
	Author varchar(255) not null,
	Genre varchar(255) not null,
	Price decimal(10, 2) not null
	);
end
