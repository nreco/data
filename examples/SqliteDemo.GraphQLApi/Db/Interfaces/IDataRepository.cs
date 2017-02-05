using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using SqliteDemo.GraphQLApi.Db.Models;
//using SqliteDemo.GraphQLApi.Db.Views;

namespace SqliteDemo.GraphQLApi.Db.Interfaces {
	public interface IDataRepository {
		//void Add(Article a);
		//Task<int> Edit(Article a);
		//void Remove(int id);
		//IEnumerable<ArticleView> GetArticles();
		Task<Supplier> GetSupplierById(int id);
	}
}
