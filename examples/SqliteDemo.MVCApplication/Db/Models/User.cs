using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using System.ComponentModel.DataAnnotations;

namespace SqliteDemo.MVCApplication.Db.Models {
    public class User {
		public int Id {
			get; set;
		}
		public string FirstName {
			get; set;
		}
		public string SecondName {
			get; set;
		}
	}
}
