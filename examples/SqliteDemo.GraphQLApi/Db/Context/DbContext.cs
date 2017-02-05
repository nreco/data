using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using SqliteDemo.GraphQLApi.Db.Models;

namespace SqliteDemo.GraphQLApi.Db.Context {
	public class DbCoreContext : DbContext {
		public DbSet<Supplier> Suppliers {
			get; set;
		}

		public DbCoreContext(DbContextOptions<DbCoreContext> options) : base(options) {
		}

		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {

		}
	}
}