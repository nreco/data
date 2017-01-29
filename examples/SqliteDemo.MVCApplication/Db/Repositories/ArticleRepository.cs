using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using SqliteDemo.MVCApplication.Db.Models;
using SqliteDemo.MVCApplication.Db.Interfaces;
using SqliteDemo.MVCApplication.Db.Context;

using NReco.Data;
using NReco.Data.Relex;

namespace SqliteDemo.MVCApplication.Db.Repositories {
    public class ArticleRepository : IArticleRepository {

		protected DbCoreContext dbContext;
		protected DbDataAdapter _DbNRecoAdapter;

		public ArticleRepository(IServiceProvider serviceProvider) {
			dbContext = serviceProvider.GetService<DbCoreContext>();
			_DbNRecoAdapter = serviceProvider.GetService<DbDataAdapter>();
		}

		public async void Add(Article a) {
			//dbContext.Articles.Add(a);
			//await dbContext.SaveChangesAsync();
			await _DbNRecoAdapter.InsertAsync("Articles", a);
		}

		public async Task<int> Edit(Article a) {
			return await _DbNRecoAdapter.UpdateAsync( 
				new Query(
					"Articles", 
					(QField)"Id" == (QConst)a.Id 
				), 
				a
			);
		}

		public Article FindById(int id) {
			var result = _DbNRecoAdapter.Select( 
				new Query(
					"Articles", 
					(QField)"Id" == (QConst)id
				)
			).Single<Article>(); 
			return result;
		}

		public IEnumerable<Article> GetArticles() {
			return _DbNRecoAdapter.Select( new Query("Articles") ).ToList<Article>(); 
		}

		public void Remove(int id) {
			_DbNRecoAdapter.Delete(
				new Query(
					"Articles", 
					(QField)"Id" == (QConst)id 
				)
			);
		}

		public IEnumerable<User> GetAllAuthors() {
			var relexParser = new RelexParser();
			var relexQuery = "Users[*;Id asc]";
			var q = relexParser.Parse(relexQuery);

			return _DbNRecoAdapter.Select( q ).ToList<User>();
		}
	}
} 
