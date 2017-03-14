using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SqliteDemo.GraphQLApi.Db.Models {

	public class ColumnMetadata {

		[Column("name")]
		public string ColumnName {
			get; set;
		}

		[Column("type")]
		public string DataType {
			get; set;
		}

		[Column("notnull")]
		public string IsNullable {
			get; set;
		}

	}
}
