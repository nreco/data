using System;
using System.IO;
using NReco.Data;

namespace SqliteDemo.SqlLogging { 
    class Program {

        static void Main(string[] args) {

			// configure ADO.NET and NReco.Data components
			var dbFactory = new LoggingDbFactory(Microsoft.Data.Sqlite.SqliteFactory.Instance) {
				LastInsertIdSelectText = "SELECT last_insert_rowid()"
			};
			var dbCmdBuilder = new DbCommandBuilder(dbFactory);
			var dbConn = dbFactory.CreateConnection();
			dbConn.ConnectionString = "Data Source="+Path.Combine(Directory.GetCurrentDirectory(), "northwind.db");
			var dbAdapter = new DbDataAdapter(dbConn, dbCmdBuilder);

			// lets perform some queries to illustrate that logging works
			var employeesCnt = dbAdapter.Select(new Query("Employees").Select(QField.Count)).Single<int>();

			dbConn.Open(); // open connection for transaction
			try {
				using (var tr = dbConn.BeginTransaction()) {
					dbAdapter.Transaction = tr;

					// some updates
					dbAdapter.Insert("Employees", new {
						EmployeeID = 1001,
						FirstName = "Test",
						LastName = "Test"
					});
					var deleted = dbAdapter.DeleteAsync(new Query("Employees", (QField)"EmployeeID">(QConst)1000)).Result;

					tr.Rollback(); // do not save these changes
					dbAdapter.Transaction = null; // clear transaction context
				}
			} finally {
				dbConn.Close();
			}

			Console.WriteLine("Press any key to continue...");
			Console.ReadKey();
		}

    }
}