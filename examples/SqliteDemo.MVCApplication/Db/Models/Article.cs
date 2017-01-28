using System.Collections.Generic;
using System.IO;
using Microsoft.EntityFrameworkCore;

using System.ComponentModel.DataAnnotations;


namespace SqliteDemo.MVCApplication.Db.Models {
	public class Article {
		public int Id {
			get; set;
		}

		[Required]
		public string Title {
			get; set;
		}
		public string Content {
			get; set;
		}
	}
}