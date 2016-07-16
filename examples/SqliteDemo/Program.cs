using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;

using System.Data;
using System.Data.Common;

using NReco.Data;

namespace SqliteDemo
{
    public class Program
    {
        public static void Main(string[] args)
        {
			
			var dbFactory = new DbFactory(Microsoft.Data.Sqlite.SqliteFactory.Instance) {
				LastInsertIdSelectText = "SELECT last_insert_rowid()"
			};
			var dbCmdBuilder = new DbCommandBuilder(dbFactory);
			var sqliteDbPath = Path.Combine( Directory.GetCurrentDirectory(), "northwind.db");

			using (var conn = dbFactory.CreateConnection()) {
				conn.ConnectionString = String.Format("Data Source={0}", sqliteDbPath);
				conn.Open();
				try {
					var selectCmd = dbCmdBuilder.GetSelectCommand(new Query("Employees", (QField)"BirthDate" > new QConst(new DateTime(1960,1,1)) ));
					Console.WriteLine(selectCmd.CommandText);
					selectCmd.Connection = conn;

					Console.WriteLine("Employees with BirthDay > 1960:");
					using (var rdr = selectCmd.ExecuteReader()) {
						while (rdr.Read()) {
							Console.WriteLine("#{0}: {1} {2}", rdr["EmployeeID"], rdr["FirstName"], rdr["LastName"]);
						}

					}
				} finally {
					conn.Close();
				}
			}
        }
    }
}
