using System;
using System.Data;
using System.Data.Common;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Data.SqlClient;

using Xunit;
using NReco.Data;

namespace NReco.Data.Tests {

	public class DbCommandBuilderTests : IClassFixture<SqliteDbFixture> {

		SqliteDbFixture SqliteDb;

		public DbCommandBuilderTests(SqliteDbFixture sqliteDb) {
			SqliteDb = sqliteDb;
		}

		[Fact]
		public void Sqlite_Select() {
			var cmdBuilder = new DbCommandBuilder(SqliteDb.DbFactory);

			var countCmd = cmdBuilder.GetSelectCommand( new Query("contacts").Select(QField.Count) );
			countCmd.Connection = SqliteDb.DbConnection;
			SqliteDb.OpenConnection( () => {
				Assert.Equal(5, Convert.ToInt32( countCmd.ExecuteScalar() ) );
			});
		}

		QNode createTestQuery() {
			return  (
						(QField)"name" % (QConst)"Anna" | new QNegationNode( (QField)"age" >= (QConst)18 )
					) & (
						(QField)"weight" == (QConst)54.3
						&
						new QConditionNode(
							(QField)"type",
							Conditions.In, 
							(QConst)new string[] {"Str1", "Str2"}
						)
					) | (
						(QField)"name"!=(QConst)"Petya"
						&
						new QConditionNode(
							(QField)"type", Conditions.Not|Conditions.Null,	null)
					);		
		}

		[Fact]
		public void Select_Speed() {
			var dbFactory = new DbFactory( SqlClientFactory.Instance );
			var cmdGenerator = new DbCommandBuilder(dbFactory);
			
			Query q = new Query( "test" );
			q.Condition = createTestQuery();
			q.Fields = new QField[] { "name", "age" };

			// SELECT TEST
			var stopwatch = new System.Diagnostics.Stopwatch();
			stopwatch.Start();
			for (int i=0; i<10000; i++) {
				IDbCommand cmd = cmdGenerator.GetSelectCommand( q );	
			}
			stopwatch.Stop();

			Console.WriteLine("Speedtest for select command generation (10000 times): {0}", stopwatch.Elapsed); 
		}


		[Fact]
		public void InsertUpdateDelete_Speed() {
			var dbFactory = new DbFactory( SqlClientFactory.Instance );
			var cmdGenerator = new DbCommandBuilder(dbFactory);

			var testData = new Dictionary<string,object> {
				{"name", "Test" },
				{"age", 20 },
				{"created", DateTime.Now }
			};
			var testQuery = new Query("test", (QField)"id" == (QConst)5 );
			var stopwatch = new Stopwatch();

			int iterations = 0;
			stopwatch.Start();
			while (iterations < 500) {
				iterations++;
				var insertCmd = cmdGenerator.GetInsertCommand( "test", testData);
				var updateCmd = cmdGenerator.GetUpdateCommand( testQuery, testData);
				var deleteCmd = cmdGenerator.GetDeleteCommand( testQuery );
			}

			stopwatch.Stop();
			Console.WriteLine("Speedtest for DbCommandGenerator Insert+Update+Delete commands ({1} times): {0}", stopwatch.Elapsed, iterations);
		}
		
		
		[Fact]
		public void BuildCommands() {
			var dbFactory = new DbFactory( SqlClientFactory.Instance );
			var cmdGenerator = new DbCommandBuilder(dbFactory);
			
			var q = new Query( new QTable("test","t") );
			q.Condition = createTestQuery();
			q.Fields = new QField[] { "name", "t.age", new QField("age_months", "t.age*12") };

			// SELECT TEST with prefixes and expressions
			IDbCommand cmd = cmdGenerator.GetSelectCommand( q );	
			string masterSQL = "SELECT name,(t.age) as age,(t.age*12) as age_months FROM test t WHERE (((name LIKE @p0) Or (NOT(age>=@p1))) And ((weight=@p2) And (type IN (@p3,@p4)))) Or ((name<>@p5) And (type IS NOT NULL))";
			
			Assert.Equal( masterSQL, cmd.CommandText.Trim() );

			// SELECT WITH TABLE ALIAS TEST
			cmd = cmdGenerator.GetSelectCommand(
					new Query("accounts.a",
						new QConditionNode( (QField)"a.id", Conditions.In, 
							new Query("dbo.accounts.b", (QField)"a.id"!=(QField)"b.id" ) ) ) );
			masterSQL = "SELECT * FROM accounts a WHERE a.id IN (SELECT * FROM dbo.accounts b WHERE a.id<>b.id)";
			Assert.Equal( masterSQL, cmd.CommandText);
			
			var testData = new Dictionary<string,object> {
				{"name", "Test" },
				{"age", 20 },
				{"weight", 75.6 },
				{"type", "staff" }
			};


			// INSERT TEST
			cmd = cmdGenerator.GetInsertCommand( "test", testData );
			masterSQL = "INSERT INTO test (name,age,weight,type) VALUES (@p0,@p1,@p2,@p3)";
			
			Assert.Equal( cmd.CommandText, masterSQL);
			Assert.Equal( cmd.Parameters.Count, 4);
			
			// UPDATE TEST
			cmd = cmdGenerator.GetUpdateCommand( new Query("test", (QField)"name"==(QConst)"test"), testData );
			masterSQL = "UPDATE test SET name=@p0,age=@p1,weight=@p2,type=@p3 WHERE name=@p4";
			
			Assert.Equal( cmd.CommandText, masterSQL);
			Assert.Equal( cmd.Parameters.Count, 5);
			
			// UPDATE TEST (by query)
			var changes = new Dictionary<string,IQueryValue>() {
				{ "age", (QConst)21 }, { "name", (QConst)"Alexandra" } };
			cmd = cmdGenerator.GetUpdateCommand(new Query("test", (QField)"id" == (QConst)1), changes);
			masterSQL = "UPDATE test SET age=@p0,name=@p1 WHERE id=@p2";

			Assert.Equal(masterSQL, cmd.CommandText);
			Assert.Equal(3, cmd.Parameters.Count);
			
			// DELETE BY QUERY TEST
			cmd = cmdGenerator.GetDeleteCommand( new Query("test", (QField)"id"==(QConst)5 ) );
			masterSQL = "DELETE FROM test WHERE id=@p0";
			
			Assert.Equal( cmd.CommandText, masterSQL);
			Assert.Equal( cmd.Parameters.Count, 1);
		}

		[Fact]
		public void DataView() {
			var dbFactory = new DbFactory( SqlClientFactory.Instance );
			var cmdGenerator = new DbCommandBuilder(dbFactory);

			cmdGenerator.Views = new Dictionary<string,DbDataView>() {
				{ "persons_view", 
					new DbDataView(
						@"SELECT @columns FROM persons p LEFT JOIN countries c ON (c.id=p.country_id) @where[ WHERE {0}] @orderby[ ORDER BY {0}]") {
							FieldMapping = new Dictionary<string,string>() {
								{"id", "p.id"},  
								{"count(*)", "count(p.id)" },
								{"*", "p.*" },
								{"expired", "CASE WHEN DATEDIFF(dd, p.added_date, NOW() )>30 THEN 1 ELSE 0 END" } 
							}			
						}
				} };
			
			// simple count query test
			Assert.Equal(
				"SELECT (count(p.id)) as cnt FROM persons p LEFT JOIN countries c ON (c.id=p.country_id)",
				cmdGenerator.GetSelectCommand( new Query("persons_view").Select(QField.Count) ).CommandText.Trim()
			);
			
			// field mapping in select columns
			Assert.Equal(
				"SELECT (p.id) as id,name,(CASE WHEN DATEDIFF(dd, p.added_date, NOW() )>30 THEN 1 ELSE 0 END) as expired FROM persons p LEFT JOIN countries c ON (c.id=p.country_id)",
				cmdGenerator.GetSelectCommand( new Query("persons_view")
					.Select("id", "name", "expired") ).CommandText.Trim()
			);

			// field mapping in conditions
			Assert.Equal(
				"SELECT p.* FROM persons p LEFT JOIN countries c ON (c.id=p.country_id)  WHERE (p.id>@p0) And (CASE WHEN DATEDIFF(dd, p.added_date, NOW() )>30 THEN 1 ELSE 0 END=@p1)",
				cmdGenerator.GetSelectCommand( new Query("persons_view",
					(QField)"id">(QConst)5 & (QField)"expired"==new QConst(true)
				) ).CommandText.Trim()
			);
		}
		

		
		
	}
}
