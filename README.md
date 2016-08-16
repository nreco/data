# NReco.Data
Lightweight data access components for generating SQL commands, mapping results to strongly typed POCO models or dictionaries, schema-less CRUD-operations. 

* abstract DB-independent Query structure (no need to compose raw SQL)
* DbCommandBuilder for generating SELECT, INSERT, UPDATE and DELETE commands
* DbBatchCommandBuilder for generating several SQL statements into one IDbCommand instance (batch inserts, updates)
* RecordSet structure for many in-memory data records (lightweight and efficient replacement for DataTable/DataRow)
* DbDataAdapter for CRUD-operations, can map query results to POCO models, dictionaries and RecordSet
* application-level data views (complex SQL queries) that accessed like simple read-only tables (DbDataView)
* best for schema-less DB access, dynamic DB queries, user-defined filters, reporting applications 
* fills the gap between minimalistic .NET Core (corefx) System.Data and EF Core 
* parser/builder for compact string query representation (relex)
* can be used with any existing ADO.NET data provider (MsSql, PostgreSql, Sqlite, MySql etc)
* supports both full .NET Framework 4.x and .NET Core (netstandard1.5)

Nuget package: [NReco.Data](https://www.nuget.org/packages/NReco.Data/)

## How to use 	
**DbCommandBuilder for SqlClient**:
```
var dbFactory = new DbFactory(System.Data.SqlClient.SqlClientFactory.Instance) {
	LastInsertIdSelectText = "SELECT @@IDENTITY"
};
var dbCmdBuilder = new DbCommandBuilder(dbFactory);
var selectCmd = dbCmdBuilder.GetSelectCommand( 
	new Query("Employees", (QField)"BirthDate" > new QConst(new DateTime(1960,1,1)) ) );
var insertCmd = dbCmdBuilder.GetInsertCommand(
	"Employees", new { Name = "John Smith", BirthDate = new DateTime(1980,1,1) } );
var deleteCmd = dbCmdBuilder.GetDeleteCommand(
	new Query("Employees", (QField)"Name" == (QConst)"John Smith" ) );
```
**Sqlite** - only difference is:
```
var dbFactory = new DbFactory(Microsoft.Data.Sqlite.SqliteFactory.Instance) {
	LastInsertIdSelectText = "SELECT last_insert_rowid()"
};
```
**DbDataAdapter** - provides simple API for CRUD-operations:
```
var dbConnection = dbFactory.CreateConnection();
var dbAdapter = new DbDataAdapter(dbConnection, dbCmdBuilder);
// map select results to POCO models
var employeeModelsList = dbAdapter.Select<Employee>( new Query("Employees") ).ToList(); 
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
```
**RecordSet** - efficient replacement for DataTable/DataRow:
```
var rs = dbAdapter.Select(new Query("Employees")).ToRecordSet();
rs.SetPrimaryKey("EmployeeID");
foreach (var row in rs) {
	Console.WriteLine("ID={0}", row["EmployeeID"]);
	if ("Canada".Equals(row["Country"]))
		row.Delete();
}
dbAdapter.Update(rs);
```
**Relex** - compact relational query expressions:
```
var relexParser = new NReco.Data.Relex.RelexParser();
Query q = relexParser.Parse("Employees(BirthDate>"1960-01-01":datetime)[Name,BirthDate]");
```

## More examples
* [Command Builder](https://github.com/nreco/data/tree/master/examples/SqliteDemo.CommandBuilder) (includes code for batching inserts)
* [Data Adapter](https://github.com/nreco/data/tree/master/examples/SqliteDemo.DataAdapter)
