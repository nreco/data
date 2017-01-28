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

namespace SqliteDemo.MVCApplication.Db.Repositories {
    public class ArticleRepository : IArticleRepository {

		protected DbCoreContext dbContext;
		protected DbDataAdapter _DbNRecoAdapter;

		public ArticleRepository(IServiceProvider serviceProvider) {
			dbContext = serviceProvider.GetService<DbCoreContext>();
			_DbNRecoAdapter = serviceProvider.GetService<DbDataAdapter>();
		}

		public async void Add(Article a) {
			dbContext.Articles.Add(a);
			await dbContext.SaveChangesAsync();
		}

		public async Task<int> Edit(Article a) {
			dbContext.Articles.Update(a);
			return await dbContext.SaveChangesAsync();
		}

		public Article FindById(int id) {
			var result = dbContext.Articles.Where(c => c.Id == id).FirstOrDefault();
			return result;
		}

		public IEnumerable<Article> GetArticles() {
			//return dbContext.Articles.ToArray();
			return _DbNRecoAdapter.Select( new Query("Articles") ).ToList<Article>(); 
		}

		public void Remove(int id) {
			var a = new Article { Id = id };
			dbContext.Entry(a).State = EntityState.Deleted;
			//Article p = dbContext.Articles.Where(c => c.ArticleId == id).FirstOrDefault();
			//dbContext.Articles.Remove(p);
			dbContext.SaveChanges();
		}
	}
} 
