using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SqlserverDemo.DbMetadata.Models {
	public class DataTable {

		public string table_name {
			get; set;
		}

		public DateTime? create_time {
			get; set;
		}

		public List<DataColumn> Columns {
			get; set;
		}

	}
}
