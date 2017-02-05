using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace SqliteDemo.GraphQLApi.Db.Models {
	public class Supplier {

		public int SupplierID {
			get; set;
		}

		public string CompanyName {
			get; set;
		}

		public string ContactName {
			get; set;
		}
	}
}