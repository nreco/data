using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;

using System.Data;
using System.Data.Common;
using System.Diagnostics;

using NReco.Data;

namespace SqliteDemo.CommandBuilder
{
	/// <summary>
	/// Example illustrates how to use DbCommandBuilder for generating SQL commands by Query structure.
	/// </summary>
	/// <remarks>
	/// This approach is useful if you really need to have full control over low-level ADO.NET components.
	/// In most cases it is much easier to use DbDataAdapter which provides simple interface for CRUD-operations (see SqliteDemo.DataAdapter example).
	/// </remarks>
    public class Program
    {
        public static void Main(string[] args)
        {
			// configure ADO.NET and NReco.Data components
			var dbFactory = new DbFactory(Microsoft.Data.Sqlite.SqliteFactory.Instance) {
				LastInsertIdSelectText = "SELECT last_insert_rowid()"
			};
			var dbCmdBuilder = new DbCommandBuilder(dbFactory);
			var sqliteDbPath = Path.Combine( Directory.GetCurrentDirectory(), "northwind.db");

			using (var conn = dbFactory.CreateConnection()) {
				conn.ConnectionString = String.Format("Data Source={0}", sqliteDbPath);
				conn.Open();

				// simple helper class that holds DB context.
				var dbContext = new DbContext() {
					CommandBuilder = dbCmdBuilder,
					Connection = conn,
					DbFactory = dbFactory
				};

				try {
					RunSelect(dbContext);

					// lets run insert / update in transaction
					using (var tr = conn.BeginTransaction()) {
						dbContext.Transaction = tr;
						try {
							RunInsert(dbContext);
							RunUpdate(dbContext);
							tr.Commit();
						} catch {
							tr.Rollback();
							throw;
						}
					}
					dbContext.Transaction = null;

					RunDelete(dbContext);

					// batch inserts: several SQL statements in one DbCommand
					RunBatchInserts(dbContext);

				} finally {
					conn.Close();
				}
			}
        }

		static void RunSelect(DbContext dbContext) {
			var selectCmd = dbContext.CommandBuilder.GetSelectCommand(new Query("Employees", (QField)"BirthDate" > new QConst(new DateTime(1960,1,1)) ));
			dbContext.InitCommand(selectCmd);

			Console.WriteLine("Selecting 'Employees' with BirthDay > 1960");
			Console.WriteLine("Generated SQL: {0}", selectCmd.CommandText);
			using (var rdr = selectCmd.ExecuteReader()) {
				while (rdr.Read()) {
					Console.WriteLine("#{0}: {1} {2}", rdr["EmployeeID"], rdr["FirstName"], rdr["LastName"]);
				}

			}
			Console.WriteLine();
		}

		static void RunInsert(DbContext dbContext) {
			var newEmployee = new Dictionary<string,object>() {
				{ "EmployeeID", 1000 },
				{ "FirstName", "John" },
				{ "LastName", "Smith" },
				{ "BirthDate", new DateTime(1980, 1, 1) }
			};
			var insertCmd = dbContext.CommandBuilder.GetInsertCommand("Employees", newEmployee );
			dbContext.InitCommand(insertCmd);

			Console.WriteLine("Inserting new record to 'Employees' table");
			Console.WriteLine("Generated SQL: {0}", insertCmd.CommandText);
			var affected = insertCmd.ExecuteNonQuery();
			Console.WriteLine("Done, affected: {0}", affected);
			Console.WriteLine();
		}

		static void RunUpdate(DbContext dbContext) {
			var changeset = new Dictionary<string,object>() {
				{ "FirstName", "Mike" }
			};
			var updateCmd = dbContext.CommandBuilder.GetUpdateCommand(
				new Query("Employees", (QField)"EmployeeID" == (QConst)1000 ), changeset );
			dbContext.InitCommand(updateCmd);

			Console.WriteLine("Updating just inserted record in 'Employees' table");
			Console.WriteLine("Generated SQL: {0}", updateCmd.CommandText);
			var affected = updateCmd.ExecuteNonQuery();
			Console.WriteLine("Done, affected: {0}", affected);
			Console.WriteLine();
		}

		static void RunDelete(DbContext dbContext) {
			var deleteCmd = dbContext.CommandBuilder.GetDeleteCommand(
				new Query("Employees", (QField)"EmployeeID" >= (QConst)1000 ) );
			dbContext.InitCommand(deleteCmd);

			Console.WriteLine("Deleting all records with EmployeeID >=100 from 'Employees' table");
			Console.WriteLine("Generated SQL: {0}", deleteCmd.CommandText);
			var affected = deleteCmd.ExecuteNonQuery();
			Console.WriteLine("Done, affected: {0}", affected);
			Console.WriteLine();
		}

		static void RunBatchInserts(DbContext dbContext) {
			// about SQL statements batches: https://msdn.microsoft.com/en-us/library/ms712553%28v=vs.85%29.aspx
			// note that depending on database/ADO.NET connector, executing SQL statements in batch may not lead to performance boost
			// (for example, for SQLite)
			// usually batches are efficient for inserting/updating many records in cloud DB 
			// (like Azure SQL: https://azure.microsoft.com/en-us/documentation/articles/sql-database-use-batching-to-improve-performance/ )

			var sw = new Stopwatch();
			var batchCmdBuilder = new DbBatchCommandBuilder(dbContext.DbFactory);

			// lets create 10,000 records to insert
			var insertsCount = 10000;
			var insertRecords = new List<Dictionary<string,object>>();
			for (int i=0; i<insertsCount; i++) {
				insertRecords.Add( new Dictionary<string, object>() {
					{ "EmployeeID", 1000+i },
					{ "FirstName", "First"+i.ToString() },
					{ "LastName", "Last"+i.ToString() }
				} );
			}

			sw.Start();

			// insert in batch (10-per-command)
			var batchSize = 10;
			var startIdx = 0;

			using (var tr = dbContext.Connection.BeginTransaction()) {

				while (startIdx<insertRecords.Count) {
					batchCmdBuilder.BeginBatch();
					for (var i=0; i<batchSize && (i+startIdx)<insertRecords.Count; i++) {
						batchCmdBuilder.GetInsertCommand("Employees", insertRecords[startIdx+i] );
					}
					var cmd = batchCmdBuilder.EndBatch();
					cmd.Connection = dbContext.Connection;
					cmd.Transaction = tr;
					cmd.ExecuteNonQuery();
					startIdx += batchSize;

					if ( (startIdx%1000)==0 )
						Console.WriteLine("Inserted {0} records...", startIdx);
				}
				tr.Commit();
			}

			sw.Stop();

			Console.WriteLine("Inserted {0} records in {1}", insertsCount, sw.Elapsed);
			
			// ensure that records are really inserted
			var employeesCountCmd = dbContext.CommandBuilder.GetSelectCommand(new Query("Employees").Select(QField.Count) );
			employeesCountCmd.Connection = dbContext.Connection;
			Console.WriteLine("Number of records in 'Employees' table: {0}", employeesCountCmd.ExecuteScalar() );
		}


		public class DbContext {
			public IDbConnection Connection { get; set; }
			public IDbCommandBuilder CommandBuilder { get; set; }
			public IDbTransaction Transaction { get; set; }
			public IDbFactory DbFactory { get; set; }

			public void InitCommand(IDbCommand cmd) {
				cmd.Connection = Connection;
				if (Transaction!=null)
					cmd.Transaction = Transaction;
			}
		}

    }
}
