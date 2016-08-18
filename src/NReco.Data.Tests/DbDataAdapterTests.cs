using System;
using System.Data;
using System.Data.Common;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

using Xunit;

namespace NReco.Data.Tests {

	public class DbDataAdapterTests : IClassFixture<SqliteDbFixture> {

		SqliteDbFixture SqliteDb;
		DbDataAdapter DbAdapter;

		public DbDataAdapterTests(SqliteDbFixture sqliteDb) {
			SqliteDb = sqliteDb;
			
			var cmdBuilder = new DbCommandBuilder(SqliteDb.DbFactory);
			cmdBuilder.Views["contacts_view"] = new DbDataView(
				@"SELECT @columns FROM contacts c
				  LEFT JOIN companies cc ON (cc.id=c.company_id)
				@where[ WHERE {0}] @orderby[ ORDER BY {0}]"
			) {
				FieldMapping = new Dictionary<string,string>() {
					{"id", "c.id"},
					{"*", "c.*, cc.title as company_title"}
				}
			};

			DbAdapter = new DbDataAdapter(sqliteDb.DbConnection, cmdBuilder );
		}

		[Fact]
		public void Select() {
			// count: table
			Assert.Equal(5, DbAdapter.Select(new Query("contacts").Select(QField.Count) ).Single<int>() );
			// count: data view
			Assert.Equal(5, DbAdapter.Select(new Query("contacts_view").Select(QField.Count) ).Single<int>() );

			var johnContactQuery = DbAdapter.Select(new Query("contacts", new QConditionNode((QField)"name",Conditions.Like,(QConst)"%john%") ) );
			var johnDict = johnContactQuery.ToDictionary();
			Assert.Equal("John Doe", johnDict["name"]);
			Assert.Equal(4, johnDict.Count);

			var johnModel = johnContactQuery.Single<ContactModel>();
			Assert.Equal("John Doe", johnModel.name);

			var contactsWithHighScoreQuery = DbAdapter.Select(
				new Query("contacts_view", (QField)"score" > (QConst)4 ).OrderBy("name desc")
			);

			var contactsWithHighDicts = contactsWithHighScoreQuery.ToDictionaryList();
			Assert.Equal(2, contactsWithHighDicts.Count );

			var contactsWithHightRS = contactsWithHighScoreQuery.ToRecordSet();
			Assert.Equal(2, contactsWithHightRS.Count );
			Assert.Equal(5, contactsWithHightRS.Columns.Count );
			Assert.Equal("Viola Garrett", contactsWithHightRS[0]["name"] );
		}

		[Fact]
		public void InsertUpdateDelete_Dictionary() {
			// insert
			object recordId = null;
			SqliteDb.OpenConnection( () => { 
				Assert.Equal(1,
					DbAdapter.Insert("companies", new Dictionary<string,object>() {
						{"title", "Test Inc"},
						{"country", "Norway"}
					}) );				
				recordId = DbAdapter.CommandBuilder.DbFactory.GetInsertId(DbAdapter.Connection); 
			} );
			// update
			Assert.Equal(1, 
				DbAdapter.Update( new Query("companies", (QField)"id"==new QConst(recordId) ), 
				new Dictionary<string,object>() {
					{"title", "Megacorp Inc"}
				}
			) );

			var norwayCompanyQ = new Query("companies", (QField)"country"==(QConst)"Norway" );

			Assert.Equal("Megacorp Inc", DbAdapter.Select(norwayCompanyQ).ToDictionary()["title"]);

			// cleanup
			Assert.Equal(1, DbAdapter.Delete( norwayCompanyQ ) );
		}

		[Fact]
		public void InsertUpdateDelete_RecordSet() {
			
			var companyRS = DbAdapter.Select(new Query("companies")).ToRecordSet();
			companyRS.SetPrimaryKey("id");
			companyRS.Columns["id"].AutoIncrement = true;

			var newCompany1Row = companyRS.Add();
			newCompany1Row["title"] = "Test Inc";
			newCompany1Row["country"] = "Ukraine";

			var newCompany2Row = companyRS.Add();
			newCompany2Row["title"] = "Cool Inc";
			newCompany2Row["country"] = "France";

			Assert.Equal(4, companyRS.Count );
			Assert.Equal(2, DbAdapter.Update("companies", companyRS) );

			Assert.True( newCompany1Row.Field<int>("id")>0 );
			Assert.True( newCompany2Row.Field<int>("id")>0 );
			
			Assert.Equal(RecordSet.RowState.Unchanged, newCompany1Row.State);

			newCompany2Row["title"] = "Awesome Corp";
			Assert.Equal(1, DbAdapter.Update("companies", companyRS));
			Assert.Equal(RecordSet.RowState.Unchanged, newCompany2Row.State);

			// cleanup
			newCompany1Row.Delete();
			newCompany2Row.Delete();
			DbAdapter.Update("companies", companyRS);

			Console.WriteLine(newCompany1Row.State.ToString() );

			Assert.Equal(2, companyRS.Count);
			Assert.Equal(2, DbAdapter.Select(new Query("companies").Select(QField.Count) ).Single<int>() );
		}
		

		public class ContactModel {
			public int? id { get; set; }
			public string name { get; set; }
			public int? company_id { get; set; }
		}
		
	}
}
