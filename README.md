# NReco.Data
Lightweight data access components for generating SQL commands by dynamic queries, mapping results to strongly typed POCO models or dictionaries, CRUD-operations. 

* abstract Query structure
* DbCommandBuilder for generating SELECT, INSERT, UPDATE and DELETE commands
* DbBatchCommandBuilder for generating several SQL statements into one IDbCommand instance (batch inserts, updates)
* DbDataAdapter for CRUD-operations, can map query results to objects, insert/update by objects
* best for schema-less DB access, dynamic DB queries, user-defined filters, reporting applications 
* fills the gap between minimalistic .NET Core (corefx) System.Data and EF Core 
* parser for compact string query representation (relex)
* can be used with any existing ADO.NET data provider (MsSql, PostgreSql, Sqlite, MySql etc)
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
**DbDataAdapter** - provides simple interface for CRUD-operations:
```
var dbConnection = dbFactory.CreateConnection();
var dbAdapter = new DbDataAdapter(dbConnection, dbCmdBuilder);
// map select results to POCO models
var employeeModelsList = dbAdapter.Select<Employee>( new Query("Employees") ).ToList(); 
// read select result to dictionary
var employeeDictionary = dbAdapter.Select( 
		new Query("Employees", (QField)"EmployeeID"==(QConst)newEmployee.EmployeeID ).Select("FirstName","LastName") 
	).ToDictionary();
```

**Relex** - compact query expressions:
```
var relexParser = new NReco.Data.Relex.RelexParser();
Query q = relexParser.Parse("Employees(BirthDate>"1960-01-01":datetime)[Name,BirthDate]");
```

More examples:
* [Command Builder](https://github.com/nreco/data/tree/master/examples/SqliteDemo.CommandBuilder)
* [Data Adapter](https://github.com/nreco/data/tree/master/examples/SqliteDemo.DataAdapter)

Nuget package: [NReco.Data](https://www.nuget.org/packages/NReco.Data/)