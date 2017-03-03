using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations.Schema;

namespace MySqlDemo.DbMetadata.Models {
	
	public class TableMetadata {

		[Column("table_name")]
		public string TableName { get; set; }

		[Column("create_time")]
		public DateTime? CreateTime { get; set; }

		public List<ColumnMetadata> Columns { get; set; }

	}
}
