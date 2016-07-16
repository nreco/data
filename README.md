# NReco.Data
Lightweight data access components for generating SQL commands by db-independent queries.

* abstract query structure
* implements DbCommandBuilder for generating SELECT, INSERT, UPDATE and DELETE commands 
* best for schema-less DB access, dynamic DB queries and user-defined filters 
* fills the gap between minimalistic .NET Core (corefx) System.Data and rich EF Core 
* parser for compact string query representation (relex)
* can be used with any existing ADO.NET data provider
* supports both full .NET Framework 4.x and .NET Core (netstandard1.5)

## How to use 	
**SqlClient**:
```
var dbFactory = new DbFactory(System.Data.SqlClient.SqlClientFactory.Instance) {
	LastInsertIdSelectText = "SELECT @@IDENTITY"
};
var dbCmdBuilder = new DbCommandBuilder(dbFactory);
var selectCmd = dbCmdBuilder.GetSelectCommand( new Query("Employees", (QField)"BirthDate" > new QConst(new DateTime(1960,1,1)) ) );
var insertCmd = dbCmdBuilder.GetInsertCommand( "Employees", new { Name = "John Smith", BirthDate = new DateTime(1980,1,1) } );
var deleteCmd = dbCmdBuilder.GetDeleteCommand( new Query("Employees", (QField)"Name" == (QConst)"John Smith" ) );
```
**Sqlite** - only difference is:
```
var dbFactory = new DbFactory(Microsoft.Data.Sqlite.SqliteFactory.Instance) {
	LastInsertIdSelectText = "SELECT last_insert_rowid()"
};
```
**Relex** - compact query expressions:
```
var relexParser = new NReco.Data.Relex.RelexParser();
Query q = relexParser.Parse("Employees(BirthDate>"1960-01-01":datetime)[Name,BirthDate]");
```
Nuget package: [NReco.Data](https://www.nuget.org/packages/NReco.Data/)