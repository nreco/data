using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;

using System.Data;
using System.Data.Common;
using Microsoft.Data.Sqlite;
using System.Threading;

namespace NReco.Data.Tests
{
    public class SqliteDbFixture : IDisposable
    {
		public SqliteConnection DbConnection;	
		public SqlLogDbFactory DbFactory;

		public string DbFileName;

		public SqliteDbFixture() {
			DbFileName = Path.GetTempFileName()+".sqlite";

			DbConnection = new SqliteConnection( $"Data Source={DbFileName}" );
			DbFactory = new SqlLogDbFactory(Microsoft.Data.Sqlite.SqliteFactory.Instance) {
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

		// collects SQL command texts of executed queries.
		public class SqlLogDbFactory : DbFactory {

			public List<string> SqlLog { get; private set; } = new List<string>();

			public SqlLogDbFactory(DbProviderFactory dbPrvFactory) : base(dbPrvFactory) {

			}

			public override IDbCommand CreateCommand() {
				var realCmd = (DbCommand)base.CreateCommand();
				return new SqlLogDbCommand(realCmd, this);
			}

			public class SqlLogDbCommand : DbCommand {
				DbCommand DbCmd;
				SqlLogDbFactory LogDbFactory;

				internal SqlLogDbCommand(DbCommand realCmd, SqlLogDbFactory logDbFactory) {
					DbCmd = realCmd;
					LogDbFactory = logDbFactory;
				}

				public override string CommandText { get => DbCmd.CommandText; set => DbCmd.CommandText = value; }
				public override int CommandTimeout { get => DbCmd.CommandTimeout; set => DbCmd.CommandTimeout = value; }
				public override CommandType CommandType { get => DbCmd.CommandType; set => DbCmd.CommandType = value; }
				public override bool DesignTimeVisible { get => DbCmd.DesignTimeVisible; set => DbCmd.DesignTimeVisible = value; }
				public override UpdateRowSource UpdatedRowSource { get => DbCmd.UpdatedRowSource; set => DbCmd.UpdatedRowSource = value; }
				protected override DbConnection DbConnection { get => DbCmd.Connection; set => DbCmd.Connection = value; }
				protected override DbParameterCollection DbParameterCollection => DbCmd.Parameters;
				protected override DbTransaction DbTransaction { get => DbCmd.Transaction; set => DbCmd.Transaction = value; }

				public override void Cancel() {
					DbCmd.Cancel();
				}

				public override void Prepare() {
					DbCmd.Prepare();
				}

				protected override DbParameter CreateDbParameter() {
					return DbCmd.CreateParameter();
				}

				T ExecuteWithLogging<T>(Func<T> exec) {
					LogDbFactory.SqlLog.Add(CommandText);
					return exec();
				}

				Task<T> ExecuteWithLogging<T>(Func<Task<T>> exec) {
					LogDbFactory.SqlLog.Add(CommandText);
					return exec();
				}

				public override int ExecuteNonQuery() {
					return ExecuteWithLogging(DbCmd.ExecuteNonQuery);
				}

				public override Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken) {
					return ExecuteWithLogging(() => DbCmd.ExecuteNonQueryAsync(cancellationToken));
				}

				public override object ExecuteScalar() {
					return ExecuteWithLogging(DbCmd.ExecuteScalar);
				}

				public override Task<object> ExecuteScalarAsync(CancellationToken cancellationToken) {
					return ExecuteWithLogging(() => DbCmd.ExecuteScalarAsync(cancellationToken));
				}

				protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) {
					return ExecuteWithLogging(() => DbCmd.ExecuteReader(behavior));
				}

				protected override Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken) {
					return ExecuteWithLogging(() => DbCmd.ExecuteReaderAsync(behavior, cancellationToken));
				}
			}


		}



	}
}
