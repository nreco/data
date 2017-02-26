using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SqlserverDemo.DbMetadata.Models {
	public class Database {
		public string Name {
			get; set;
		}

		public List<DataTable> DataTables {
			get; set;
		}
	}
}
