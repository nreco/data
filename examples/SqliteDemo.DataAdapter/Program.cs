using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using System.IO;

using NReco.Data;

namespace SqliteDemo.DataAdapter
{
	/// <summary>
	/// Example illustrates how to use DbDataAdapter for accessing and updating DB in a schema-less way.
	/// </summary>
    public class Program
    {
        public static void Main(string[] args)
        {
			var sqliteDbPath = Path.Combine( Directory.GetCurrentDirectory(), "northwind.db");

			// configure ADO.NET and NReco.Data components
			var dbFactory = new DbFactory(Microsoft.Data.Sqlite.SqliteFactory.Instance) {
				LastInsertIdSelectText = "SELECT last_insert_rowid()"
			};
			var dbConnection = dbFactory.CreateConnection();
			dbConnection.ConnectionString = String.Format("Data Source={0}", sqliteDbPath);

			var dbCmdBuilder = new DbCommandBuilder(dbFactory);
			var dbAdapter = new DbDataAdapter(dbConnection, dbCmdBuilder);
			
			// note: DbDataAdapter will automatically open (if it is not opened) and close DB connection
			
			Console.WriteLine("Records count in 'Employees' table: {0}",
					dbAdapter.Select( new Query("Employees").Select( QField.Count ) ).First<int>()
				);

			// select into POCO model (columns are mapped to object properties)
			Console.WriteLine("All names from 'Employees' table:");
			foreach (var employee in dbAdapter.Select( new Query("Employees") ).ToList<Employee>() ) {
				Console.WriteLine("#{0}: {1} {2}", employee.EmployeeID, employee.FirstName, employee.LastName);
			}
			Console.WriteLine();

			// select with subquery into dictionaries
			Console.WriteLine("Products from 'Seafood' category:");
			var productBySeafoodQuery = new Query("Products", 
					new QConditionNode( (QField)"CategoryID", Conditions.In, 
						new Query("Categories", (QField)"CategoryName"==(QConst)"Seafood" ).Select("CategoryID")
					)
				).Select("ProductName", "UnitPrice");
			foreach (var product in dbAdapter.Select( productBySeafoodQuery ).ToList<Dictionary<string,object>>() ) {
				Console.WriteLine("{0} for ${1}", product["ProductName"], product["UnitPrice"]);
			}
			Console.WriteLine();

			// lets remove all employees with ID>=1000 (cleanup from previous example run)
			dbAdapter.Delete(new Query("Employees", (QField)"EmployeeID">=(QConst)1000 ));

			// add new employee
			var newEmployee = new Employee() { EmployeeID = 1000, FirstName = "John", LastName = "Smith" };
			dbAdapter.Insert("Employees", newEmployee );
			Console.WriteLine("Added new employee: John Smith (ID=1000)");

			// update employee
			newEmployee.FirstName = "Bart";
			var newEmployeeByIdQuery = new Query("Employees", (QField)"EmployeeID"==(QConst)newEmployee.EmployeeID );
			dbAdapter.Update( newEmployeeByIdQuery, newEmployee);

			Console.WriteLine("New first name for EmployeeID=1000: {0}", 
				dbAdapter.Select( new Query("Employees", (QField)"EmployeeID"==(QConst)1000).Select("FirstName") ).First<string>() );
			Console.WriteLine();

			// update only some fields from model
			newEmployee.LastName = "Simpson";
			newEmployee.FirstName = "Homer";

			dbAdapter.Update(newEmployeeByIdQuery, newEmployee,  
					// update takes only model properties that are specified in property-to-field mapping
					new Dictionary<string,string>() {
						{"LastName", "LastName"}
					}
				);
			
			var newEmployeeNameFromDb = dbAdapter.Select( new Query(newEmployeeByIdQuery).Select("FirstName","LastName") ).ToDictionary();
			Console.WriteLine("First+Last for EmployeeID=1000 after update: {0} {1}",
				newEmployeeNameFromDb["FirstName"], newEmployeeNameFromDb["LastName"]);

        }


		public class Employee {
			public int EmployeeID { get; set; }
			public string FirstName { get; set; }
			public string LastName { get; set; }
			public DateTime? BirthDate { get; set; }
		}
    }
}
