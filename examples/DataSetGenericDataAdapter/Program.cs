using System;
using System.IO;
using System.Data;
using System.Data.Common;

using NReco.Data;

namespace DataSetGenericDataAdapter
{
	class Program {
		static void Main(string[] args) {

			GenericDataAdapterForSqlite();

			GenericDataAdapterForMySql();
		}

		static void GenericDataAdapterForSqlite() {
			Console.WriteLine("Sqlite database (northwind.db)");

			var dbFactory = new DbFactory(Microsoft.Data.Sqlite.SqliteFactory.Instance) {
				LastInsertIdSelectText = "SELECT last_insert_rowid()",
				IdentifierFormat = "\"{0}\""
			};
			var dbCmdBuilder = new NReco.Data.DbCommandBuilder(dbFactory);

			var sqliteDbPath = Path.Combine(Directory.GetCurrentDirectory(), "northwind.db");
			var sqliteConnStr = String.Format("Data Source={0}", sqliteDbPath);

			using (var conn = dbFactory.CreateConnection()) {
				conn.ConnectionString = sqliteConnStr;

				var selectCmd = dbCmdBuilder.GetSelectCommand(new Query("Employees"));
				selectCmd.Connection = conn;

				var dsDataAdapter = new GenericDataAdapter(dbCmdBuilder, (DbCommand)selectCmd);
				var ds = new DataSet();
				dsDataAdapter.Fill(ds, "Employees");

				Console.WriteLine("Fill: loaded {0} rows", ds.Tables["Employees"].Rows.Count);

				// lets set PK
				ds.Tables["Employees"].PrimaryKey = new[] { ds.Tables["Employees"].Columns["EmployeeID"] };
				ds.Tables["Employees"].Columns["Deleted"].ReadOnly = true;  // do not insert/update this column

				// and modify some rows
				ds.Tables["Employees"].Rows.Find(1)["FirstName"] = "Nancy1";
				var newRow = ds.Tables["Employees"].NewRow();
				newRow["EmployeeID"] = 10;
				newRow["FirstName"] = "John";
				newRow["LastName"] = "Smith";
				newRow["ReportsTo"] = 2;
				newRow["Deleted"] = 1;  // this will be ignored 
				ds.Tables["Employees"].Rows.Add(newRow);

				Console.WriteLine("Update (insert+update): affected {0} rows", dsDataAdapter.Update(ds.Tables["Employees"]));

				ds.Tables["Employees"].Rows.Find(10).Delete();
				Console.WriteLine("Update (delete): affected {0} rows", dsDataAdapter.Update(ds.Tables["Employees"]));

			}
			Console.WriteLine();
		}

		static void GenericDataAdapterForMySql() {
			Console.WriteLine("Mysql database (sample server, may respond slowly)");

			var dbFactory = new DbFactory(MySql.Data.MySqlClient.MySqlClientFactory.Instance) {
				LastInsertIdSelectText = "SELECT LAST_INSERT_ID()",
				IdentifierFormat = "`{0}`"
			};
			var dbCmdBuilder = new NReco.Data.DbCommandBuilder(dbFactory);

			var mysqlConnStr = "Server=db4free.net;Database=nreco_sampledb;Uid=nreco_sampledb;Pwd=HRt5UbVD;";
			using (var conn = dbFactory.CreateConnection()) {
				conn.ConnectionString = mysqlConnStr;

				var selectCmd = dbCmdBuilder.GetSelectCommand(new Query("orders"));
				selectCmd.Connection = conn;

				var dsDataAdapter = new GenericDataAdapter(dbCmdBuilder, (DbCommand)selectCmd);
				var ds = new DataSet();
				dsDataAdapter.Fill(ds, "orders");

				Console.WriteLine("Fill: loaded {0} rows", ds.Tables["orders"].Rows.Count);
			}
		}
	}
}
