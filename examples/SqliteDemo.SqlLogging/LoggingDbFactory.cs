using System;
using System.Collections.Generic;
using System.Text;
using System.Data.Common;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

using NReco.Data;


namespace SqliteDemo.SqlLogging {

	/// <summary>
	/// Extends generic DbFactory by wrapping IDbCommand with special proxy implementation.
	/// </summary>
    public class LoggingDbFactory : DbFactory {

		public LoggingDbFactory(DbProviderFactory dbPrvFactory) : base(dbPrvFactory) {

		}

		protected void DbCommandExecuting(DbCommand cmd) {

		}

		protected void DbCommandExecuted(DbCommand cmd, TimeSpan execTime) {
			// call your logging library here
			// in this example console is used for the sake of simplicity
			Console.WriteLine($"Executed ({execTime.TotalMilliseconds.ToString("0.###")}ms): {cmd.CommandText}");
		}

		public override IDbCommand CreateCommand() {
			var realCmd = (DbCommand)base.CreateCommand();
			return new LoggingDbCommand(realCmd, this);
		}

		public class LoggingDbCommand : DbCommand {
			DbCommand DbCmd;
			LoggingDbFactory LogDbFactory;

			internal LoggingDbCommand(DbCommand realCmd, LoggingDbFactory logDbFactory) {
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
				LogDbFactory.DbCommandExecuting(this);
				var sw = new Stopwatch();
				sw.Start();
				T res = exec();
				sw.Stop();
				LogDbFactory.DbCommandExecuted(this, sw.Elapsed);
				return res;
			}

			async Task<T> ExecuteWithLogging<T>(Func<Task<T>> exec) {
				LogDbFactory.DbCommandExecuting(this);
				var sw = new Stopwatch();
				sw.Start();
				T res = await exec().ConfigureAwait(false);
				sw.Stop();
				LogDbFactory.DbCommandExecuted(this, sw.Elapsed);
				return res;
			}

			public override int ExecuteNonQuery() {
				return ExecuteWithLogging( DbCmd.ExecuteNonQuery );
			}

			public override Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken) {
				return ExecuteWithLogging( ()=> DbCmd.ExecuteNonQueryAsync(cancellationToken) );
			}

			public override object ExecuteScalar() {
				return ExecuteWithLogging( DbCmd.ExecuteScalar );
			}

			public override Task<object> ExecuteScalarAsync(CancellationToken cancellationToken) {
				return ExecuteWithLogging( () => DbCmd.ExecuteScalarAsync(cancellationToken) );
			}

			protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) {
				return ExecuteWithLogging( () => DbCmd.ExecuteReader(behavior) );
			}

			protected override Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken) {
				return ExecuteWithLogging( () => DbCmd.ExecuteReaderAsync(behavior, cancellationToken) );
			}
		}


	}
}
