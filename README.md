# NReco.Data
Lightweight high-performance data access components for generating SQL commands, mapping results to strongly typed POCO models or dictionaries, schema-less CRUD-operations with RecordSet. 

NuGet | Windows x64 | Ubuntu 14.04
--- | --- | ---
[![NuGet Release](https://img.shields.io/nuget/v/NReco.Data.svg)](https://www.nuget.org/packages/NReco.Data/) | [![AppVeyor](https://img.shields.io/appveyor/ci/nreco/data/master.svg)](https://ci.appveyor.com/project/nreco/data) | [![Travis CI](https://img.shields.io/travis/nreco/data/master.svg)](https://travis-ci.org/nreco/data) 

* very fast: NReco.Data shows almost the same performance as Dapper but offers more features
* abstract DB-independent [Query structure](https://github.com/nreco/data/wiki/Query): no need to compose raw SQL in the code + query can be constructed dynamically (in the run-time)
* automated CRUD commands generation
* generate several SQL statements into one IDbCommand (batch inserts, updates, selects for multiple recordsets: *DbBatchCommandBuilder*)
* supports mapping to annotated POCO models (EF Core entity models), allows customized mapping of query result 
* API for schema-less data access (dictionaries, RecordSet, DataTable)
* can handle results returned by stored procedure, including multiple record sets
* application-level data views (for complex SQL queries) that accessed like simple read-only tables (DbDataView)
* parser for compact string query representation: [relex](https://github.com/nreco/data/wiki/Relex) expressions
* can be used with any existing ADO.NET data provider (SQL Server, PostgreSql, Sqlite, MySql etc)
* supports .NET Framework 4.5+, .NET Core 2.x / 3.x (netstandard2.0)

## Quick reference
Class | Dependencies | Purpose
--- | --- | ---
`DbFactory` | | incapsulates DB-specific functions and conventions 
`DbCommandBuilder` | *IDbFactory* | composes *IDbCommand* and SQL text for SELECT/UPDATE/DELETE/INSERT, handles app-level dataviews
`DbDataAdapter` | *IDbCommandBuilder*, *IDbConnection* | CRUD operations for model, dictionary, *DataTable* or *[RecordSet](https://github.com/nreco/data/wiki/RecordSet)*: Insert/Update/Delete/Select. Async versions are supported for all methods.
`Query` | | Represents abstract query to database; used as parameter in *DbCommandBuilder*, *DbDataAdapter*
`RelexParser` | | Parsers query string expression ([Relex](https://github.com/nreco/data/wiki/Relex)) into *Query* structure
`RecordSet` | | [RecordSet model](https://github.com/nreco/data/wiki/RecordSet) represents in-memory data records, this is lightweight and efficient replacement for classic *DataTable*/*DataRow*
`DataReaderResult` | *IDataReader* | reads data from any data reader implementation and efficiently maps it to models, dictionaries, *DataTable* or *RecordSet*

NReco.Data documentation:
* [Getting started and HowTos](https://github.com/nreco/data/wiki)
* [Full API Reference](http://www.nrecosite.com/doc/NReco.Data/)
* something is still not clear? Feel free to [ask a question on StackOverflow](http://stackoverflow.com/questions/ask?tags=nreco,c%23) 

## How to use
Generic implementation of `DbFactory` can be used with any ADO.NET connector. 

**DbFactory initialization for SqlClient**:
```
var dbFactory = new DbFactory(System.Data.SqlClient.SqlClientFactory.Instance) {
	LastInsertIdSelectText = "SELECT @@IDENTITY" };
```
**DbFactory initialization for Mysql**:
```
var dbFactory = new DbFactory(MySql.Data.MySqlClient.MySqlClientFactory.Instance) {
	LastInsertIdSelectText = "SELECT LAST_INSERT_ID()" };
```
**DbFactory initialization for Postgresql**:
```
var dbFactory = new DbFactory(Npgsql.NpgsqlFactory.Instance) {
	LastInsertIdSelectText = "SELECT lastval()" };
```
**DbFactory initialization for Sqlite**:
```
var dbFactory = new DbFactory(Microsoft.Data.Sqlite.SqliteFactory.Instance) {
	LastInsertIdSelectText = "SELECT last_insert_rowid()" };
```

**DbCommandBuilder** generates SQL commands by [Query](https://github.com/nreco/data/wiki/Query):
```
var dbCmdBuilder = new DbCommandBuilder(dbFactory);
var selectCmd = dbCmdBuilder.GetSelectCommand( 
	new Query("Employees", (QField)"BirthDate" > new QConst(new DateTime(1960,1,1)) ) );
var insertCmd = dbCmdBuilder.GetInsertCommand(
	"Employees", new { Name = "John Smith", BirthDate = new DateTime(1980,1,1) } );
var deleteCmd = dbCmdBuilder.GetDeleteCommand(
	new Query("Employees", (QField)"Name" == (QConst)"John Smith" ) );
```

**DbDataAdapter** - provides simple API for CRUD-operations:
```
var dbConnection = dbFactory.CreateConnection();
dbConnection.ConnectionString = "<db_connection_string>";
var dbAdapter = new DbDataAdapter(dbConnection, dbCmdBuilder);
// map select results to POCO models
var employeeModelsList = dbAdapter.Select( new Query("Employees") ).ToList<Employee>();
// read select result to dictionary
var employeeDictionary = dbAdapter.Select( 
    new Query("Employees", (QField)"EmployeeID"==(QConst)newEmployee.EmployeeID ).Select("FirstName","LastName") 
  ).ToDictionary();
// update by dictionary
dbAdapter.Update( 
	new Query("Employees", (QField)"EmployeeID"==(QConst)1001 ),
	new Dictionary<string,object>() {
		{"FirstName", "Bruce" },
		{"LastName", "Wayne" }
	});
// insert by model
dbAdapter.Insert( "Employees", new { FirstName = "John", LastName = "Smith" } );  
```
**[RecordSet](https://github.com/nreco/data/wiki/RecordSet)** - efficient replacement for DataTable/DataRow with very similar API:
```
var rs = dbAdapter.Select(new Query("Employees")).ToRecordSet();
rs.SetPrimaryKey("EmployeeID");
foreach (var row in rs) {
	Console.WriteLine("ID={0}", row["EmployeeID"]);
	if ("Canada".Equals(row["Country"]))
		row.Delete();
}
dbAdapter.Update(rs);
var rsReader = new RecordSetReader(rs); // DbDataReader for in-memory rows
```
**[Relex](https://github.com/nreco/data/wiki/Relex)** - compact relational query expressions:
```
var relex = @"Employees(BirthDate>""1960-01-01"":datetime)[Name,BirthDate]"
var relexParser = new NReco.Data.Relex.RelexParser();
Query q = relexParser.Parse(relex);
```

## More examples
* [Command Builder](https://github.com/nreco/data/tree/master/examples/SqliteDemo.CommandBuilder/Program.cs): illustrates SQL commands generation, command batching (inserts)
* [Data Adapter](https://github.com/nreco/data/tree/master/examples/SqliteDemo.DataAdapter/Program.cs): CRUD operations with dictionaries, POCO, RecordSet
* [DataSet GenericDataAdapter](https://github.com/nreco/data/blob/master/examples/DataSetGenericDataAdapter/Program.cs): how to implement generic DataSet DataAdapter (Fill/Update) for any ADO.NET provider 
* [SQL logging](https://github.com/nreco/data/tree/master/examples/SqliteDemo.SqlLogging): how to extend `DbFactory` and add wrapper for `DbCommand` that logs SQL commands produced by `DbDataAdapter`
* [DB WebApi](https://github.com/nreco/data/tree/master/examples/SqliteDemo.WebApi): configures NReco.Data services in MVC Core app, simple REST API for database tables
* [MVC Core CRUD](https://github.com/nreco/data/tree/master/examples/SqliteDemo.MVCApplication): full-functional CRUD (list, add/edit forms) that uses NReco.Data as data layer in combination with EF Core
* [DB Metadata](https://github.com/nreco/data/tree/master/examples/MySqlDemo.DbMetadata): extract database metadata (list of tables, columns) with information_schema queries
* [GraphQL API for SQL database](https://github.com/nreco/data/tree/master/examples/SqliteDemo.GraphQLApi): provides simple GraphQL API by existing database schema (simple queries only, no mutations yet)

## Who is using this?
NReco.Data is in production use at [SeekTable.com](https://www.seektable.com/) and [PivotData microservice](https://www.nrecosite.com/pivotdata_service.aspx).

## License
Copyright 2016-2020 Vitaliy Fedorchenko and contributors

Distributed under the MIT license
