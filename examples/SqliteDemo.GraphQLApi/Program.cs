using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;

namespace SqliteDemo.GraphQLApi
{
    // Simple grapql API based on Graphql.NET + NReco.Data
    // If you're looking for production-ready Graphql-to-SQL engine try this component:
    // https://www.nrecosite.com/graphql_to_sql_database.aspx
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = new WebHostBuilder()
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .UseIIS()
                .UseStartup<Startup>()
                .Build();

            host.Run();
        }
    }
}
