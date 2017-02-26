using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SqlserverDemo.DbMetadata.Models {
	public class DataColumn {

		public string column_name {
			get; set;
		}

		public string data_type {
			get; set;
		}

		public string is_nullable {
			get; set;
		}

	}
}
