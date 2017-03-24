using System;
using System.Data;
using System.Data.Common;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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
			// count for schema-qualified query
			Assert.Equal(5, DbAdapter.Select( new Query( new QTable( "main.contacts",null) ).Select(QField.Count) ).Single<int>() );

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

			// check in
			var contactsWithFourAndFiveScore = DbAdapter.Select(
				new Query("contacts_view", new QConditionNode( (QField)"score", Conditions.In, new QConst(new[] {4,5}) ) ).OrderBy("name desc")
			);
			Assert.Equal(4, contactsWithFourAndFiveScore.ToRecordSet().Count);

			// select to annotated object
			var companies = DbAdapter.Select(new Query("companies").OrderBy("id")).ToList<CompanyModelAnnotated>();
			Assert.Equal(2, companies.Count);
			Assert.Equal("Microsoft", companies[0].Name);

		}

		[Fact]
		public async Task Select_Async() {
			// count: table
			Assert.Equal(5, await DbAdapter.Select(new Query("contacts").Select(QField.Count) ).SingleAsync<int>() );
			
			var contactsWithHighScoreQuery = DbAdapter.Select(
				new Query("contacts_view", (QField)"score" > (QConst)4 ).OrderBy("name desc")
			);
			
			var contactsWithHighDicts = await contactsWithHighScoreQuery.ToDictionaryListAsync();
			Assert.Equal(2, contactsWithHighDicts.Count );	
			
			var contactsWithHightRS = await contactsWithHighScoreQuery.ToRecordSetAsync();
			Assert.Equal(2, contactsWithHightRS.Count );											
		}

		[Fact]
		public void SelectRawSql() {
			//no params
			Assert.Equal(5, DbAdapter.Select("select count(*) from contacts").Single<int>() );
			// simple param
			Assert.Equal(5, DbAdapter.Select("select count(*) from contacts where id<{0}", 100).Single<int>() );
			Assert.Equal(1, DbAdapter.Select("select count(*) from contacts where id>{0} and id<{1}", 1, 3).Single<int>() );

			// custom db param
			var customParam = new Microsoft.Data.Sqlite.SqliteParameter("test", "%John%");
			Assert.Equal(1, DbAdapter.Select("select company_id from contacts where name like @test", customParam).Single<int>() );
		}
		
		[Fact]
		public void Select_CustomMapper() {
			
			var res = DbAdapter
				.Select(new Query("contacts_view", (QField)"name" == (QConst)"Morris Scott") )
				.SetMapper( (context)=> {
					if (context.ObjectType==typeof(ContactModel)) {
						var contact = (ContactModel)context.MapTo(context.ObjectType);
						contact.Company = new CompanyModelAnnotated();
						contact.Company.Id = Convert.ToInt32( context.DataReader["company_id"] );
						contact.Company.Name = (string)context.DataReader["company_title"];
						return contact;
					}
					// default handler
					return context.MapTo(context.ObjectType);
				})
				.Single<ContactModel>();
			Assert.NotNull(res.Company);
			Assert.Equal(1, res.Company.Id.Value);
			Assert.Equal("Microsoft", res.Company.Name);
		}


		[Fact]
		public void InsertUpdateDelete_Dictionary() {
			// insert
			object recordId = null;
			SqliteDb.OpenConnection( () => { 
				Assert.Equal(1,
					DbAdapter.Insert("main.companies", new Dictionary<string,object>() {
						{"title", "Test Inc"},
						{"country", "Norway"},
						{"logo_image", null}
					}) );				
				recordId = DbAdapter.CommandBuilder.DbFactory.GetInsertId(DbAdapter.Connection); 
			} );
			// update - schema qualified
			Assert.Equal(1, 
				DbAdapter.Update( new Query( new QTable("main.companies", null), (QField)"id"==new QConst(recordId) ), 
					new Dictionary<string,object>() {
						{"title", "Megacorp"}
					}
				) );
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

			// delete - schema qualified (affects 0 records)
			Assert.Equal(0, DbAdapter.Delete( new Query( new QTable("main.companies",null), (QField)"country"==(QConst)"bla" ) ) );
		}

		[Fact]
		public async Task InsertUpdateDelete_DictionaryAsync() {
			// insert
			DbAdapter.Connection.Open();
			Assert.Equal(1,
				await DbAdapter.InsertAsync("companies", new Dictionary<string,object>() {
					{"title", "Test Inc"},
					{"country", "Norway"}
				}).ConfigureAwait(false) );				
			object recordId = DbAdapter.CommandBuilder.DbFactory.GetInsertId(DbAdapter.Connection); 
			DbAdapter.Connection.Close();

			// update
			Assert.Equal(1, 
				await DbAdapter.UpdateAsync( new Query("companies", (QField)"id"==new QConst(recordId) ), 
					new Dictionary<string,object>() {
						{"title", "Megacorp Inc"}
					}
				).ConfigureAwait(false) );

			var norwayCompanyQ = new Query("companies", (QField)"country"==(QConst)"Norway" );

			Assert.Equal("Megacorp Inc", DbAdapter.Select(norwayCompanyQ).ToDictionary()["title"]);

			// cleanup
			Assert.Equal(1, await DbAdapter.DeleteAsync( norwayCompanyQ ).ConfigureAwait(false) );
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
			Assert.Equal(1, DbAdapter.Update("main.companies", companyRS));
			Assert.Equal(RecordSet.RowState.Unchanged, newCompany2Row.State);

			// cleanup
			newCompany1Row.Delete();
			newCompany2Row.Delete();
			DbAdapter.Update("companies", companyRS);

			Assert.Equal(2, companyRS.Count);
			Assert.Equal(2, DbAdapter.Select(new Query("companies").Select(QField.Count) ).Single<int>() );
		}

		[Fact]
		public async Task InsertUpdateDeleteAsync_RecordSet() {
			var companyRS = await DbAdapter.Select(new Query("companies")).ToRecordSetAsync().ConfigureAwait(false);
			Assert.Equal(2, companyRS.Count);
			companyRS.SetPrimaryKey("id");
			companyRS.Columns["id"].AutoIncrement = true;
			
			var newCompany1Row = companyRS.Add();
			newCompany1Row["title"] = "Test Inc";
			newCompany1Row["country"] = "Ukraine";

			Assert.Equal(1, DbAdapter.UpdateAsync("companies", companyRS).GetAwaiter().GetResult() );

			Assert.NotNull(newCompany1Row["id"]);
			Assert.Equal( RecordSet.RowState.Unchanged, newCompany1Row.State );

			newCompany1Row["title"] = "Mega Corp";
			var newCompany2Row = companyRS.Add();
			newCompany2Row["title"] = "Cool Inc";
			newCompany2Row["country"] = "France";
			
			Assert.Equal(2, DbAdapter.UpdateAsync("companies", companyRS).GetAwaiter().GetResult() );
			
			Assert.Equal(2, await DbAdapter.DeleteAsync(new Query("companies", (QField)"id">= new QConst(newCompany1Row["id"]) )) );				
		}

		[Fact]
		public void InsertUpdateDelete_PocoModel() {
			// insert
			var newCompany = new CompanyModelAnnotated();
			newCompany.Id = 5000; // should be ignored
			newCompany.Name = "Test Super Corp";
			newCompany.registered = false; // should be ignored
			newCompany.Logo = new byte[] { 1,2,3 }; // lets assume this is sample binary data
			DbAdapter.Insert(newCompany);
			
			Assert.True(newCompany.Id.HasValue);
			Assert.NotEqual(5000, newCompany.Id.Value);

			Assert.Equal("Test Super Corp", DbAdapter.Select(new Query("companies", (QField)"id"==(QConst)newCompany.Id.Value).Select("title") ).Single<string>() );
			
			newCompany.Name = "Super Corp updated";
			Assert.Equal(1, DbAdapter.Update( newCompany) );

			Assert.Equal(newCompany.Name, DbAdapter.Select(new Query("companies", (QField)"id"==(QConst)newCompany.Id.Value).Select("title") ).Single<string>() );

			Assert.Equal(1, DbAdapter.Delete( newCompany ) );
		}
		
		[Fact]
		public async Task InsertUpdateDelete_PocoModelAsync() {
			// insert
			var newCompany = new CompanyModelAnnotated();
			newCompany.Id = 5000; // should be ignored
			newCompany.Name = "Test Super Corp";
			newCompany.registered = false; // should be ignored
			Assert.Equal(1, await DbAdapter.InsertAsync(newCompany).ConfigureAwait(false) );
			
			Assert.True(newCompany.Id.HasValue);
			Assert.NotEqual(5000, newCompany.Id.Value);

			Assert.Equal("Test Super Corp", DbAdapter.Select(new Query("companies", (QField)"id"==(QConst)newCompany.Id.Value).Select("title") ).Single<string>() );
			
			newCompany.Name = "Super Corp updated";
			Assert.Equal(1, await DbAdapter.UpdateAsync( newCompany).ConfigureAwait(false) );

			Assert.Equal(newCompany.Name, DbAdapter.Select(new Query("companies", (QField)"id"==(QConst)newCompany.Id.Value).Select("title") ).Single<string>() );

			Assert.Equal(1, await DbAdapter.DeleteAsync( newCompany ).ConfigureAwait(false) );
		}

		[Fact]
		public void Select_MultipleResultSets() {
			var batchCmdBuilder = new DbBatchCommandBuilder(DbAdapter.CommandBuilder.DbFactory);
			batchCmdBuilder.BeginBatch();
			batchCmdBuilder.GetSelectCommand(new Query("companies"));
			batchCmdBuilder.GetSelectCommand(new Query("contacts"));
			var selectMultipleCmd = batchCmdBuilder.EndBatch();

			(var companies, var contacts) = DbAdapter.Select(selectMultipleCmd).ExecuteReader( (rdr) => {
				var companiesRes = new DataReaderResult(rdr).ToList<CompanyModelAnnotated>();
				rdr.NextResult();
				var contactsRes = new DataReaderResult(rdr).ToList<ContactModel>();
				return (companiesRes, contactsRes);
			});
			Assert.Equal(2, companies.Count);
			Assert.Equal(5, contacts.Count);
		}


		public class ContactModel {
			public int? id { get; set; }
			public string name { get; set; }
			public int? company_id { get; set; }

			// property is used in custom POCO mapping handler test
			public CompanyModelAnnotated Company { get; set; }
		}

		[Table("companies")]
		public class CompanyModelAnnotated {
			
			[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
			[Key]
			[Column("id")]
			public int? Id { get; set; }
			
			[Column("title")]
			public string Name { get; set; }
			
			[Column("logo_image")]
			public byte[] Logo { get; set; }

			[NotMapped]
			public bool registered { get; set; }
		}		
		
	}
}
