using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;

using System.Data;
using Microsoft.Data.Sqlite;

namespace NReco.Data.Tests
{
    public class SqliteDbFixture : IDisposable
    {
		public SqliteConnection DbConnection;	
		public DbFactory DbFactory;

		public string DbFileName;

		public SqliteDbFixture() {
			DbFileName = Path.GetTempFileName()+".sqlite";

			DbConnection = new SqliteConnection( $"Data Source={DbFileName}" );
			DbFactory = new DbFactory(Microsoft.Data.Sqlite.SqliteFactory.Instance) {
				LastInsertIdSelectText = "SELECT last_insert_rowid()"
			};
			CreateDb();
		}

		void CreateDb() {
			Execute(@"CREATE TABLE [companies]  (
					[id] INTEGER PRIMARY KEY AUTOINCREMENT,
					[title] TEXT,
					[country] TEXT,
					[size] INTEGER,
					[registered] TEXT,
					[logo_image] BLOB
				)");
			Execute(@"INSERT INTO [companies] (title,country,size,registered) VALUES ('Microsoft', 'USA', 118000, '1975-04-04')");
			Execute(@"INSERT INTO [companies] (title,country,size,registered) VALUES ('Atlassian', 'Australia', 1259, '2002')");

			Execute(@"CREATE TABLE [contacts]  (
					[id] INTEGER PRIMARY KEY AUTOINCREMENT,
					[name] TEXT,
					[company_id] INTEGER,
					[score] INTEGER
				)");
			Execute(@"INSERT INTO [contacts] (name,company_id,score) VALUES ('John Doe', 1, 5)");			
			Execute(@"INSERT INTO [contacts] (name,company_id,score) VALUES ('Morris Scott', 1, 4)");	
			Execute(@"INSERT INTO [contacts] (name,company_id,score) VALUES ('Bill Glover', 1, 3)");

			Execute(@"INSERT INTO [contacts] (name,company_id,score) VALUES ('Edward Jordan', 2, 4)");
			Execute(@"INSERT INTO [contacts] (name,company_id,score) VALUES ('Viola Garrett', 2, 5)");
		}

		void Execute(string sql) {
			var cmd = new SqliteCommand(sql);
			cmd.Connection = DbConnection;
			OpenConnection( () => cmd.ExecuteNonQuery() );
		}

		public void OpenConnection( Action a ) {
			DbConnection.Open();
			try {
				a();
			} finally {
				DbConnection.Close();
			}			
		}

		public void Dispose() {
			DbConnection.Dispose();
			if (File.Exists(DbFileName))
				File.Delete(DbFileName);
		}
	}
}
