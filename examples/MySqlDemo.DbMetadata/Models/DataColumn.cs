using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MySqlDemo.DbMetadata.Models {
	public class DataColumn {

		[Column("column_name")]
		public string ColumnName {
			get; set;
		}

		[Column("data_type")]
		public string DataType {
			get; set;
		}

		[Column("is_nullable")]
		public string IsNullable {
			get; set;
		}

	}
}
