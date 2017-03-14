using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

using MySql.Data.MySqlClient;

using NReco.Data;

using MySqlDemo.DbMetadata.Models;

namespace MySqlDemo.DbMetadata {

	/// <summary>
	/// This example illustrates how to use NReco.Data for getting database metadata (list of tables / table columns)
	/// by querying 'information_schema' views (part of SQL-92, https://en.wikipedia.org/wiki/Information_schema).
	/// </summary>
	/// <remarks>
	/// Note that 'information_schema' views are not supported by some databases (like Oracle, SQLite).
	/// </remarks>
	public class Program {

		private static DbDataAdapter _dbAdapter;
		protected static DbDataAdapter dbAdapter {
			get {
				if (_dbAdapter == null) {
					var sqlDbPath = "Server=useastdb.ensembl.org;Port=3306;Database=ailuropoda_melanoleuca_otherfeatures_86_1;Uid=anonymous;";

					var dbFactory = new DbFactory(MySqlClientFactory.Instance) {
						LastInsertIdSelectText = "SELECT last_insert_rowid()"
					};
					var dbConnection = dbFactory.CreateConnection();
					dbConnection.ConnectionString = sqlDbPath;

					var dbCmdBuilder = new DbCommandBuilder(dbFactory);
					_dbAdapter = new DbDataAdapter(dbConnection, dbCmdBuilder);
				}
				return _dbAdapter;
			}
		}

		public static void Main(string[] args) {

			Console.WriteLine("Fetch 'transcript table' columns (database 'ailuropoda_melanoleuca_otherfeatures_86_1'):");
			var customerTable = FetchTableMetaData("transcript");
			Console.Write("Table added: {0}", customerTable.CreateTime);
			Console.WriteLine();
			Console.WriteLine();
			foreach (var col in customerTable.Columns) {
				Console.WriteLine("Column name: {0}; Data type: {1}; IsNullable: {2}", col.ColumnName, col.DataType, col.IsNullable);
			}
			Console.WriteLine("Press any key to continue...");
			Console.ReadKey();
		}

		protected static TableMetadata FetchTableMetaData(string tableName) {
			var query = new Query(
				new QTable("INFORMATION_SCHEMA.tables", null),
				(QField)"table_name" == (QConst)tableName).Select("table_name", "create_time"
			);
			var table = dbAdapter.Select(query).Single<TableMetadata>();
			GetColumnsMetadata(table);
			return table;
		}

		protected static void GetColumnsMetadata(TableMetadata table) {
			var query = new Query(
				new QTable("INFORMATION_SCHEMA.columns", null),
				(QField)"TABLE_NAME" == (QConst)table.TableName).Select("column_name", "data_type", "is_nullable"
			);
			var tableColumns = dbAdapter.Select(query).ToList<ColumnMetadata>();

			table.Columns = tableColumns;
		}
	}
}
