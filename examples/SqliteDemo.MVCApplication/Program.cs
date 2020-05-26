using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;

namespace SqliteDemo.MVCApplication
{
    // CRUD app that uses both EF Core AND NReco.Data
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
