using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace SqliteDemo.MVCApplication.Db.Views {
	public class ArticleView {
		public int? Id {
			get; set;
		}
		public string Title {
			get; set;
		}
		public int AuthorId {
			get; set;
		}
		public string Content {
			get; set;
		}
		public string AuthorFirstName {get; set; }
		public string AuthorLastName {get; set; }
	}
}