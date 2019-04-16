using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Data;

using Microsoft.Extensions.DependencyInjection;
using NReco.Data;

namespace SqliteDemo.WebApi
{
    
	public static class NRecoDataServiceCollectionsExt {

		public static IServiceCollection AddNRecoDataSqlite(this IServiceCollection services, string dbConnectionString = null) {
			
			services.AddSingleton<IDbFactory,DbFactory>( (servicePrv) => {
				// db-provider specific configuration code:
				return new DbFactory(Microsoft.Data.Sqlite.SqliteFactory.Instance) {
					LastInsertIdSelectText = "SELECT last_insert_rowid()"
				};
			});
			services.AddSingleton<IDbCommandBuilder,DbCommandBuilder>( (servicePrv) => {
				var dbCmdBuilder = new DbCommandBuilder(servicePrv.GetRequiredService<IDbFactory>() );
				// initialize dataviews here:
				//dbCmdBuilder.Views["some_view"] = new DbDataView(...);
				return dbCmdBuilder;
			} );

			if (dbConnectionString!=null) {
				// lets add IDbConnection to services; otherwise NReco.Data components will use IDbConnection instance defined outside
				services.AddScoped<IDbConnection>( (servicePrv) => {
					var dbFactory = servicePrv.GetRequiredService<IDbFactory>();
					var conn = dbFactory.CreateConnection();
					conn.ConnectionString = dbConnectionString;
					return conn;
				} );
			}
			services.AddScoped<DbDataAdapter>();

			return services;
		}

    }
}
