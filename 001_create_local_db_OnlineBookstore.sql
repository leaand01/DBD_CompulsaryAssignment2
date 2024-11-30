-- Create local OnlineBookstore DB if do not exist
if not exists (select * from sys.databases where name = 'OnlineBookstore')
begin
	create database OnlineBookstore;
end
