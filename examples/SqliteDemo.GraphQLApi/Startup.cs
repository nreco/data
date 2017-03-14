using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using SqliteDemo.GraphQLApi.Db.Models;

using NReco.Data;
using GraphQL;
using GraphQL.Types;
using SqliteDemo.GraphQLApi.Db.GraphQL;

namespace SqliteDemo.GraphQLApi {
	public class Startup {
		const string dbConnectionFile = "northwind.db";
		protected string ApplicationPath;

		public Startup(IHostingEnvironment env) {
			var builder = new ConfigurationBuilder()
				.SetBasePath(env.ContentRootPath)
				.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
				.AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
				.AddEnvironmentVariables();
			ApplicationPath = env.ContentRootPath;

			Configuration = builder.Build();
		}

		public IConfigurationRoot Configuration {
			get;
		}

		// This method gets called by the runtime. Use this method to add services to the container.
		public void ConfigureServices(IServiceCollection services) {
			// let's inject NReco.Data services (based on EF DBConnection)
			InjectNRecoDataService(services);
			//
			InjectGraphQLSchema(services);
			services.AddScoped<IDatabaseMetadata, DatabaseMetadata>();
			// Add framework services.
			services.AddMvc();
		}

		protected void InjectGraphQLSchema(IServiceCollection services) {
			services.AddScoped<Schema>((servicePrv) => {
				var dbAdapter = servicePrv.GetRequiredService<DbDataAdapter>();
				var metaDatabase = servicePrv.GetRequiredService<IDatabaseMetadata>();
				var schema = new Schema { Query = new GraphQLQuery(dbAdapter, metaDatabase) };
				schema.Initialize();
				return schema;
			});
		}

		protected void InjectNRecoDataService(IServiceCollection services) {

			services.AddSingleton<IDbFactory, DbFactory>((servicePrv) => {
				// db-provider specific configuration code:
				return new DbFactory(Microsoft.Data.Sqlite.SqliteFactory.Instance) {
					LastInsertIdSelectText = "SELECT last_insert_rowid()"
				};
			});
			services.AddSingleton<IDbCommandBuilder, DbCommandBuilder>((servicePrv) => {
				var dbCmdBuilder = new DbCommandBuilder(servicePrv.GetRequiredService<IDbFactory>());
				// initialize dataviews here:
				//dbCmdBuilder.Views["articles_view"] = ConfigureArticlesView();//new DbDataView(...);
				return dbCmdBuilder;
			});

			services.AddScoped<IDbConnection>((servicePrv) => {
				var dbFactory = servicePrv.GetRequiredService<IDbFactory>();
				var conn = dbFactory.CreateConnection();
				conn.ConnectionString = String.Format("Filename={0}", Path.Combine(ApplicationPath, dbConnectionFile));
				return conn;
			});

			services.AddScoped<DbDataAdapter>();
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory) {
			loggerFactory.AddConsole(Configuration.GetSection("Logging"));
			loggerFactory.AddDebug();
			//app.UseDeveloperExceptionPage();
			//app.UseBrowserLink();

			app.UseMvc();
		}

	}
}