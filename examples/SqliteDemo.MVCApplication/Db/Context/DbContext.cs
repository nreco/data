using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using SqliteDemo.MVCApplication.Db.Models;

namespace SqliteDemo.MVCApplication.Db.Context {
    public class DbCoreContext : DbContext {
		public DbSet<Article> Articles {
			get; set;
		}

		public DbSet<User> Users {
			get; set;
		}

		public DbCoreContext(DbContextOptions<DbCoreContext> options):base(options) {
		}

		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
			//optionsBuilder.UseSqlite("Filename=./coreApp2.db");
		}
	}
}
