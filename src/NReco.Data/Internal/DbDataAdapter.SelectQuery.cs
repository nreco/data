#region License
/*
 * NReco Data library (http://www.nrecosite.com/)
 * Copyright 2016 Vitaliy Fedorchenko
 * Distributed under the MIT license
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace NReco.Data {
	
	public partial class DbDataAdapter {

		/// <summary>
		/// Represents select query (returned by <see cref="DbDataAdapter.Select"/> method).
		/// </summary>
		public abstract class SelectQuery : IQueryModelResult, IQueryDictionaryResult, IQueryRecordSetResult
#if !NET_STANDARD1
		, IQueryDataTableResult
#endif
		{
			readonly protected DbDataAdapter Adapter;
			DataMapper DtoMapper;
			Func<IDataReaderMapperContext, object> CustomMappingHandler = null;

			internal SelectQuery(DbDataAdapter adapter) {
				Adapter = adapter;
				DtoMapper = DataMapper.Instance;
			}

			int DataReaderRecordOffset {
				get {
					return Adapter.ApplyOffset ? RecordOffset : 0;
				}
			}

			internal virtual int RecordOffset { get { return 0; } }

			internal virtual int RecordCount { get { return Int32.MaxValue; } }

			internal abstract IDbCommand GetSelectCmd();

			internal virtual string FirstFieldName { get { return null; } }

			internal virtual string TableName { get { return null; } }

			/// <summary>
			/// Configures custom mapping handler for POCO models.
			/// </summary>
			public SelectQuery SetMapper(Func<IDataReaderMapperContext,object> handler) {
				CustomMappingHandler = handler;
				return this;
			}

			/// <summary>
			/// Returns the first record from the query result. 
			/// </summary>
			/// <returns>depending on T, single value or all fields values from the first record</returns>
			public T Single<T>() {
				using (var selectCmd = GetSelectCmd()) {
					return ExecuteCommand(selectCmd, CommandBehavior.SingleRow,
						(rdr) => new DataReaderResult(rdr, DataReaderRecordOffset, 1, FirstFieldName)
							.SetMapper(CustomMappingHandler).Single<T>() 
					);
				}
			}

			/// <summary>
			/// Asynchronously returns the first record from the query result. 
			/// </summary>
			/// <returns>depending on T, single value or all fields values from the first record</returns>
			public Task<T> SingleAsync<T>(CancellationToken cancel = default(CancellationToken)) {
				using (var selectCmd = GetSelectCmd()) {
					return ExecuteCommandAsync(selectCmd, CommandBehavior.SingleRow,
						(rdr, c) => 
							new DataReaderResult(rdr, DataReaderRecordOffset, 1, FirstFieldName)
								.SetMapper(CustomMappingHandler).SingleAsync<T>(c),
						cancel );
				}
			}

			/// <summary>
			/// Returns a list with all query results.
			/// </summary>
			/// <returns>list with query results</returns>
			public List<T> ToList<T>() {
				using (var selectCmd = GetSelectCmd()) {
					return ExecuteCommand(selectCmd, CommandBehavior.Default,
						(rdr) => new DataReaderResult(rdr, DataReaderRecordOffset, RecordCount)
							.SetMapper(CustomMappingHandler).ToList<T>());
				}
			}

			/// <summary>
			/// Asynchronously returns a list with all query results.
			/// </summary>
			public Task<List<T>> ToListAsync<T>(CancellationToken cancel = default(CancellationToken)) {
				using (var selectCmd = GetSelectCmd()) {
					return ExecuteCommandAsync(selectCmd, CommandBehavior.Default,
						(rdr, c) => 
							new DataReaderResult(rdr, DataReaderRecordOffset, RecordCount)
								.SetMapper(CustomMappingHandler).ToListAsync<T>(c),
						cancel);
				}
			}

			/// <summary>
			/// Returns dictionary with first record values.
			/// </summary>
			/// <returns>dictionary with field values or null if query returns zero records.</returns>
			public Dictionary<string,object> ToDictionary() {
				using (var selectCmd = GetSelectCmd()) {
					return ExecuteCommand(selectCmd, CommandBehavior.SingleRow,
						(rdr) => new DataReaderResult(rdr, DataReaderRecordOffset, 1)
							.SetMapper(CustomMappingHandler).ToDictionary());
				}
			}

			/// <summary>
			/// Asynchronously returns dictionary with first record values.
			/// </summary>
			public Task<Dictionary<string,object>> ToDictionaryAsync(CancellationToken cancel = default(CancellationToken)) {
				using (var selectCmd = GetSelectCmd()) {
					return ExecuteCommandAsync(selectCmd, CommandBehavior.SingleRow,
						(rdr, c) => 
							new DataReaderResult(rdr, DataReaderRecordOffset, 1)
								.SetMapper(CustomMappingHandler).ToDictionaryAsync(c),
						cancel);
				}
			}

			/// <summary>
			/// Returns a list of dictionaries with all query results.
			/// </summary>
			public List<Dictionary<string,object>> ToDictionaryList() {
				return ToList<Dictionary<string,object>>();
			}

			/// <summary>
			/// Asynchronously a list of dictionaries with all query results.
			/// </summary>
			public Task<List<Dictionary<string, object>>> ToDictionaryListAsync(CancellationToken cancel = default(CancellationToken)) {
				using (var selectCmd = GetSelectCmd()) {
					return ExecuteCommandAsync(selectCmd, CommandBehavior.Default,
						(rdr, c) => 
							new DataReaderResult(rdr, DataReaderRecordOffset, RecordCount)
								.SetMapper(CustomMappingHandler).ToDictionaryListAsync(c),
						cancel);
				}
			}

			/// <summary>
			/// Returns all query results as <see cref="RecordSet"/>.
			/// </summary>
			public RecordSet ToRecordSet() {
				using (var selectCmd = GetSelectCmd()) {
					return ExecuteCommand(selectCmd, CommandBehavior.Default,
						(rdr) => new DataReaderResult(rdr, DataReaderRecordOffset, RecordCount)
							.SetMapper(CustomMappingHandler).ToRecordSet());
				}
			}

			/// <summary>
			/// Asynchronously returns all query results as <see cref="RecordSet"/>.
			/// </summary>
			public Task<RecordSet> ToRecordSetAsync(CancellationToken cancel = default(CancellationToken)) {
				using (var selectCmd = GetSelectCmd()) {
					return ExecuteCommandAsync(selectCmd, CommandBehavior.Default,
						(rdr, c) => 
							new DataReaderResult(rdr, DataReaderRecordOffset, RecordCount)
								.SetMapper(CustomMappingHandler).ToRecordSetAsync(c),
						cancel);
				}				
			}

#if !NET_STANDARD1

			/// <summary>
			/// Returns all query results as <see cref="DataTable"/>.
			/// </summary>
			public DataTable ToDataTable() => ToDataTable(TableName!=null ? new DataTable(TableName) : null);

			/// <summary>
			/// Loads all query results into specified <see cref="DataTable"/>.
			/// </summary>
			public DataTable ToDataTable(DataTable tbl) {
				using (var selectCmd = GetSelectCmd()) {
					return ExecuteCommand(selectCmd, CommandBehavior.Default,
						(rdr) => new DataReaderResult(rdr, DataReaderRecordOffset, RecordCount)
							.SetMapper(CustomMappingHandler).ToDataTable(tbl));
				}
			}

			/// <summary>
			/// Loads all query results into specified <see cref="DataTable"/>.
			/// </summary>
			public Task<DataTable> ToDataTableAsync(CancellationToken cancel = default(CancellationToken))
				=> ToDataTableAsync(TableName != null ? new DataTable(TableName) : null, cancel);

			/// <summary>
			/// Loads all query results into specified <see cref="DataTable"/>.
			/// </summary>
			public Task<DataTable> ToDataTableAsync(DataTable tbl, CancellationToken cancel = default(CancellationToken)) {
				using (var selectCmd = GetSelectCmd()) {
					return ExecuteCommandAsync(selectCmd, CommandBehavior.Default,
						(rdr, c) =>
							new DataReaderResult(rdr, DataReaderRecordOffset, RecordCount)
								.SetMapper(CustomMappingHandler).ToDataTableAsync(tbl, c),
						cancel);
				}
			}

#endif

			/// <summary>
			/// Executes data reader and returns custom handler result. 
			/// </summary>
			public T ExecuteReader<T>(Func<IDataReader,T> readHandler) {
				using (var selectCmd = GetSelectCmd()) {
					return ExecuteCommand<T>(selectCmd, CommandBehavior.Default, readHandler);
				}
			}

			/// <summary>
			/// Asynchronously executes data reader and returns custom handler result. 
			/// </summary>
			public Task<T> ExecuteReaderAsync<T>(Func<IDataReader, CancellationToken, Task<T>> readHandlerAsync, CancellationToken cancel) {
				using (var selectCmd = GetSelectCmd()) {
					return ExecuteCommandAsync<T>(selectCmd, CommandBehavior.Default, readHandlerAsync, cancel);
				}
			}

			internal T ExecuteCommand<T>(IDbCommand cmd, CommandBehavior cmdBehaviour, Func<IDataReader,T> getResult) {
				T res = default(T);
				DataHelper.EnsureConnectionOpen(cmd.Connection, () => {
					try {
						using (var rdr = cmd.ExecuteReader(cmdBehaviour)) {
							res = getResult(rdr);
						}
					} catch (Exception ex) {
						throw new ExecuteDbCommandException(cmd, ex);
					}
				});
				return res;
			}

			internal async Task<T> ExecuteCommandAsync<T>(
					IDbCommand cmd, CommandBehavior cmdBehaviour,
					Func<IDataReader,CancellationToken,Task<T>> getResultAsync, CancellationToken cancel) {

				var isOpenConn = cmd.Connection.State != ConnectionState.Closed;
				if (!isOpenConn) {
					await cmd.Connection.OpenAsync(cancel).ConfigureAwait(false);
				}
				IDataReader rdr = null;
				T res = default(T);
				try {
					if (cmd is DbCommand) {
						rdr = await ((DbCommand)cmd).ExecuteReaderAsync(cmdBehaviour, cancel).ConfigureAwait(false);
					} else {
						rdr = cmd.ExecuteReader(cmdBehaviour);
					}
					res = await getResultAsync(rdr, cancel).ConfigureAwait(false);
				} catch (Exception ex) {
					throw new ExecuteDbCommandException(cmd, ex);
				} finally {
					if (rdr!=null)
						rdr.Dispose();
					if (!isOpenConn)
						cmd.Connection.Close();
				}
				return res;
			}


		}
		
		internal class SelectQueryByQuery : SelectQuery {
			
			readonly Query Query;

			internal SelectQueryByQuery(DbDataAdapter adapter, Query q) 
				: base(adapter) {
				Query = q;
			}

			internal override IDbCommand GetSelectCmd() {
				var selectCmd = Adapter.CommandBuilder.GetSelectCommand(Query);
				Adapter.SetupCmd(selectCmd);
				return selectCmd;
			}

			internal override int RecordOffset { get { return Query.RecordOffset; } }

			internal override int RecordCount { get { return Query.RecordCount; } }

			internal override string FirstFieldName { 
				get { 
					return Query.Fields!=null && Query.Fields.Length>0 ? Query.Fields[0].Name : null; 
				} 
			}

			internal override string TableName {
				get {
					return Query.Table.Name;
				}
			}
		}
		
		internal class SelectQueryBySql : SelectQuery {
			
			readonly string Sql;
			object[] Parameters;

			internal SelectQueryBySql(DbDataAdapter adapter, string sql, object[] parameters) 
				: base(adapter) {
				Sql = sql;
				Parameters = parameters;
			}

			internal override IDbCommand GetSelectCmd() {
				var selectCmd = Adapter.CommandBuilder.DbFactory.CreateCommand();

				var fmtArgs = new string[Parameters.Length];
				for (int i=0; i<Parameters.Length; i++) {
					var paramVal = Parameters[i];
					
					if (paramVal is IDataParameter) {
						// this is already composed command parameter
						selectCmd.Parameters.Add(paramVal);
						fmtArgs[i] = ((IDataParameter)paramVal).ParameterName;
					} else {
						var cmdParam = Adapter.CommandBuilder.DbFactory.AddCommandParameter(selectCmd, paramVal);
						fmtArgs[i] = cmdParam.Placeholder;
					}
				}
				selectCmd.CommandText = String.Format(Sql, fmtArgs);

				Adapter.SetupCmd(selectCmd);
				return selectCmd;
			}

		}

		internal class SelectQueryByCmd : SelectQuery {

			readonly IDbCommand Cmd;

			internal SelectQueryByCmd(DbDataAdapter adapter, IDbCommand cmd)
				: base(adapter) {
				Cmd = cmd;
			}

			internal override IDbCommand GetSelectCmd() {
				Adapter.SetupCmd(Cmd);
				return Cmd;
			}

		}

	}
}
