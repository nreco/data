using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using SqliteDemo.GraphQLApi.Db.Models;
using SqliteDemo.GraphQLApi.Db.Interfaces;
using SqliteDemo.GraphQLApi.Db.Context;

using NReco.Data;
using NReco.Data.Relex;

namespace SqliteDemo.GraphQLApi.Db.Repositories {
	public class DataRepository : IDataRepository {

		protected DbCoreContext dbContext;
		protected DbDataAdapter _DbNRecoAdapter;

		public DataRepository(IServiceProvider serviceProvider) {
			dbContext = serviceProvider.GetService<DbCoreContext>();
			_DbNRecoAdapter = serviceProvider.GetService<DbDataAdapter>();
		}

		/*public async void Add(Article a) {
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
        }*/

		public Task<Supplier> GetSupplierById(int id) {
			var result = _DbNRecoAdapter.Select(
				new Query(
					"Suppliers",
					(QField)"SupplierID" == (QConst)id
				)
			).Single<Supplier>();
			return Task.FromResult(result);
		}

		/*public IEnumerable<ArticleView> GetArticles() {
			return _DbNRecoAdapter.Select( new Query("articles_view") ).ToList<ArticleView>(); 
		}*/

		/*public void Remove(int id) {
            _DbNRecoAdapter.Delete(
                new Query(
                    "Articles",
                    (QField)"Id" == (QConst)id
                )
            );
        }*/
	}
}
