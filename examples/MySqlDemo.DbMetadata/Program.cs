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

			Console.WriteLine("Fetch all columns from table 'transcript' of database 'ailuropoda_melanoleuca_otherfeatures_86_1'");
			var customerTable = FetchTableMetaData("transcript");
			Console.Write("Table added: {0}", customerTable.create_time);
			Console.WriteLine();
			Console.WriteLine();
			foreach (var col in customerTable.Columns) {
				Console.Write("Column name: {0}; Data type: {1}; IsNullable: {2} |", col.ColumnName, col.DataType, col.IsNullable);
				Console.WriteLine();
			}
			Console.ReadKey();
		}

		protected static MySqlDemo.DbMetadata.Models.DataTable FetchTableMetaData(string tableName) {
			var query = new Query(
				new QTable("information_schema.tables", null),
				(QField)"table_name" == (QConst)tableName).Select("table_name", "create_time"
			);
			var table = dbAdapter.Select(
				query
			).Single<MySqlDemo.DbMetadata.Models.DataTable>();

			GetColumnsMetadata(table);

			return table;
		}

		protected static void GetColumnsMetadata(MySqlDemo.DbMetadata.Models.DataTable table) {
			var query = new Query(
				new QTable("INFORMATION_SCHEMA.COLUMNS", null),
				(QField)"TABLE_NAME" == (QConst)table.table_name).Select("column_name", "data_type", "is_nullable"
			);
			var tableColumns = dbAdapter.Select(
				query
			).ToList<DataColumn>();

			table.Columns = tableColumns;
		}
	}
}
