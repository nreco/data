using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Microsoft.EntityFrameworkCore;

using SqliteDemo.MVCApplication.Db.Context;
using SqliteDemo.MVCApplication.Db.Models;
using SqliteDemo.MVCApplication.Db.Interfaces;
using SqliteDemo.MVCApplication.Db.Repositories;

using NReco.Data;

namespace SqliteDemo.MVCApplication
{
    public class Startup
    {
        const string dbConnectionFile = "mvcapp_database.db";
        protected string ApplicationPath;

        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();

            ApplicationPath = env.ContentRootPath;
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
			// Add framework services.
			services.AddDbContext<DbCoreContext>(options => options.UseSqlite(Path.Combine(ApplicationPath, dbConnectionFile)));
			services.AddScoped<ArticleRepository>();

            InjectNRecoDataService(services);
            // Add framework services.
            services.AddMvc();
        }

        protected void InjectNRecoDataService(IServiceCollection services) {
            //
            var dbFactory = new DbFactory(Microsoft.Data.Sqlite.SqliteFactory.Instance) {
                LastInsertIdSelectText = "SELECT last_insert_rowid()"
            };
            var dbConnection = dbFactory.CreateConnection();
            dbConnection.ConnectionString = String.Format("Data Source={0}", Path.Combine(ApplicationPath, dbConnectionFile));
            var dbCmdBuilder = new DbCommandBuilder(dbFactory);

            var dbAdapter = new DbDataAdapter(dbConnection, dbCmdBuilder);
            services.AddSingleton<DbDataAdapter>(dbAdapter);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            app.UseDeveloperExceptionPage();
            app.UseBrowserLink();

            app.UseStaticFiles();

            app.UseMvc(routes =>
            {
				routes.MapRoute(
					name: "articlelist",
					template: "articles",
					defaults: new {
						controller = "Article",
						action = "List"
					}
				);

				routes.MapRoute(
					name: "addnewarticle",
					template: "article/add/{*id}",
					defaults: new {
						controller = "Article",
						action = "Add"
					}
				);

				routes.MapRoute(
					name: "deletearticle",
					template: "article/delete/{*id}",
					defaults: new {
						controller = "Article",
						action = "Delete"
					}
				);

				routes.MapRoute(
					name: "articleedit",
					template: "article/edit/{*id}",
					defaults: new {
						controller = "Article",
						action = "Edit"
					}
				);

                routes.MapRoute(
                    name: "default",
                    template: "{controller=Article}/{action=List}/{id?}");
            });
        }
    }
}
