using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations.Schema;

using NReco.Data;

namespace SqliteDemo.GraphQLApi.Db.Models {
	
	public class TableMetadata {

		[Column("table_name")]
		public string TableName { get; set; }

		public List<ColumnMetadata> Columns { get; set; }

	}
}
