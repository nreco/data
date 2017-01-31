using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using SqliteDemo.MVCApplication.Db.Models;
using SqliteDemo.MVCApplication.Db.Views;

namespace SqliteDemo.MVCApplication.Db.Interfaces {
    interface IArticleRepository {
			void Add(Article a);
			Task<int> Edit(Article a);
			void Remove(int id);
			IEnumerable<ArticleView> GetArticles();
			Article FindById(int id);
			IEnumerable<User> GetAllAuthors();
		}
}
