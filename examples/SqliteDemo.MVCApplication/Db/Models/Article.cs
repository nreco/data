using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace SqliteDemo.MVCApplication.Db.Models {
	public class Article {
		public int? Id {
			get; set;
		}

		[Required]
		public string Title {
			get; set;
		}
		[Required]
		public int AuthorId {
			get; set;
		}
		public string Content {
			get; set;
		}

		[NotMapped]
		public List<User> UsersList {get; set; } = new List<User>();
		[NotMapped]
		public string AuthorName {get; set; }
	}
}