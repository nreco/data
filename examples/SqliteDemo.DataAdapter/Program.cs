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
			
			// note: DbDataAdapter automatically opens (if it is not opened) and closes DB connection
			
			// lets remove all employees with ID>=1000 (cleanup)
			dbAdapter.Delete(new Query("Employees", (QField)"EmployeeID">=(QConst)1000 ));

			// demo for select queries
			SelectDemo(dbAdapter);

			// demo for DbDataAdapter Insert/Update/Delete for one record
			InsertUpdateDeleteForOneRecord(dbAdapter);

			// demo for DbDataAdapter mass Update (record set)
			UpdateForRecordSet(dbAdapter);

        }

		public static void SelectDemo(DbDataAdapter dbAdapter) {
			// select single value
			Console.WriteLine("Records count in 'Employees' table: {0}",
					dbAdapter.Select( new Query("Employees").Select( QField.Count ) ).First<int>()
				);

			// select data into POCO models (columns are mapped to object properties)
			Console.WriteLine("All names from 'Employees' table:");
			foreach (var employee in dbAdapter.Select( new Query("Employees") ).ToList<Employee>() ) {
				Console.WriteLine("#{0}: {1} {2}", employee.EmployeeID, employee.FirstName, employee.LastName);
			}
			Console.WriteLine();

			// select data into dictionaries (illustrates subquery) 
			Console.WriteLine("Products from 'Seafood' category:");
			var productBySeafoodQuery = new Query("Products", 
					new QConditionNode( (QField)"CategoryID", Conditions.In, 
						new Query("Categories", (QField)"CategoryName"==(QConst)"Seafood" ).Select("CategoryID")
					)
				).Select("ProductName", "UnitPrice");
			foreach (var product in dbAdapter.Select( productBySeafoodQuery ).ToDictionaryList() ) {
				Console.WriteLine("{0} for ${1}", product["ProductName"], product["UnitPrice"]);
			}

			// select data into RecordSet
			Console.WriteLine("Customers from USA:");
			var customersRS = dbAdapter.Select( 
					new Query("Customers", (QField)"Country"==(QConst)"USA").Select("CustomerID","CompanyName","ContactName")
				).ToRecordSet();
			foreach (var row in customersRS) {
				foreach (var col in customersRS.Columns) {
					Console.Write("{0}={1} |", col.Name, row[col]);
				}
				Console.WriteLine();
			}

			Console.WriteLine();
		}

		public static void InsertUpdateDeleteForOneRecord(DbDataAdapter dbAdapter) {

			// add new employee by POCO model
			var newEmployee = new Employee() { EmployeeID = 1000, FirstName = "John", LastName = "Smith" };
			dbAdapter.Insert("Employees", newEmployee );
			Console.WriteLine("Added new employee: John Smith (ID=1000)");

			// add new employee by dictionary
			dbAdapter.Insert("Employees", new Dictionary<string,object>() {
				{"EmployeeID", 1001},
				{"FirstName", "Jim"},
				{"LastName", "Gordon"}
			});
			Console.WriteLine("Added new employee: Jim Gordon (ID=1001)");

			// update employee by poco model
			newEmployee.FirstName = "Bart";
			var newEmployeeByIdQuery = new Query("Employees", (QField)"EmployeeID"==(QConst)newEmployee.EmployeeID );
			dbAdapter.Update( newEmployeeByIdQuery, newEmployee);

			Console.WriteLine("New first name for EmployeeID=1000: {0}", 
				dbAdapter.Select( new Query("Employees", (QField)"EmployeeID"==(QConst)1000).Select("FirstName") ).First<string>() );

			// update employee by dictionary
			dbAdapter.Update( 
				new Query("Employees", (QField)"EmployeeID"==(QConst)1001 ),
				new Dictionary<string,object>() {
					{"FirstName", "Bruce" },
					{"LastName", "Wayne" }
				}
			);
			var employee_1001_data = dbAdapter.Select( new Query("Employees", (QField)"EmployeeID"==(QConst)1001 )).ToDictionary();
			Console.WriteLine("New name for EmployeeID=1001: {0} {1}", employee_1001_data["FirstName"], employee_1001_data["LastName"]);

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

		public static void UpdateForRecordSet(DbDataAdapter dbAdapter) {
			var customersRS = dbAdapter.Select( 
					new Query("Customers").OrderBy("CustomerID asc")
				).ToRecordSet();			
			Console.WriteLine("Loaded {0} customer records ({1} columns)", customersRS.Count, customersRS.Columns.Length);

			// in most cases primary key should be set explicetly
			customersRS.SetPrimaryKey("CustomerID");

			// lets change USA,Canada to 'North America'
			int updateRows = 0;
			foreach (var row in customersRS)
				if ("USA".Equals(row["Country"]) || "Canada".Equals(row["Country"])) {
					row["Country"] = "North America";
					updateRows++;
				}
			// lets delete customers from Venezuela
			var deleteRows = 0;
			foreach (var row in customersRS)
				if ("Venezuela".Equals(row["Country"])) {
					row.Delete();
					deleteRows++;
				}
			// lets add one customer from Ukraine
			var uaCustomer = customersRS.NewRow();
			uaCustomer["CompanyName"] = "MegaSuperAwsome Inc";
			uaCustomer["CustomerID"] = "MEGAUA";
			uaCustomer["Country"] = "Ukraine";
			
			Console.WriteLine("RecordSet rows: update={0} delete={1} insert={2}", updateRows, deleteRows, 1);

			dbAdapter.Connection.Open();
			using (var tr = dbAdapter.Connection.BeginTransaction()) {
				dbAdapter.Transaction = tr; // associate transaction for adapter commands
				try {
					
					dbAdapter.Update("Customers", customersRS);		
					
					Console.WriteLine("Customers count={0}", dbAdapter.Select(new Query("Customers").Select(QField.Count) ).First<int>() );

					// lets throw an exception
					throw new Exception("Rollback Test");

					tr.Commit();
				} catch (Exception ex) {
					Console.WriteLine("Exception is thrown: {0}", ex.ToString() );
					tr.Rollback();
				} finally {
					dbAdapter.Transaction = null;
					dbAdapter.Connection.Close();
				}
			}
		}

		public class Employee {
			public int EmployeeID { get; set; }
			public string FirstName { get; set; }
			public string LastName { get; set; }
			public DateTime? BirthDate { get; set; }
		}
    }
}
