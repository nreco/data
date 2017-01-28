using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using SqliteDemo.MVCApplication.Db.Models;

namespace SqliteDemo.MVCApplication.Db.Interfaces {
    interface IArticleRepository {
		void Add(Article a);
		Task<int> Edit(Article a);
		void Remove(int id);
		IEnumerable<Article> GetArticles();
		Article FindById(int id);
    }
}
