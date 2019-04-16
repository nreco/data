using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations.Schema;

using NReco.Data;

namespace SqliteDemo.GraphQLApi.Db.Models {
	
	public class DatabaseMetadata : IDatabaseMetadata {

		protected DbDataAdapter _DbNRecoAdapter;

		public DatabaseMetadata(DbDataAdapter dbAdapter) {
			_DbNRecoAdapter = dbAdapter;
			DatabaseName = _DbNRecoAdapter.Connection.Database;
			if (Tables == null)
				LoadMetaData();
		}

		public string DatabaseName { get; set; }

		public List<TableMetadata> Tables { get; set; }

		private void LoadMetaData() {
			var res = new List<TableMetadata>();
			res.Add(
				FetchTableMetaData("Customers")
			);
			Tables = res;
		}

		public void ReloadMetadata() {
			LoadMetaData();
		}

		public List<TableMetadata> GetMetadataTables() {
			if (Tables == null)
				return new List<TableMetadata>();

			return Tables;
		}

		private TableMetadata FetchTableMetaData(string tableName) {
			var metaTable = new TableMetadata { TableName = tableName };
			GetColumnsMetadata(metaTable);
			return metaTable;
		}

		private void GetColumnsMetadata(TableMetadata table) {
			var tableColumns = _DbNRecoAdapter.Select(
				$"PRAGMA table_info('{@table.TableName}');"
			).ToList<ColumnMetadata>();
			table.Columns = tableColumns;
		}
	}

	public interface IDatabaseMetadata {

		void ReloadMetadata();
		List<TableMetadata> GetMetadataTables();
	}
}
