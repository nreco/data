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
using System.Threading;
using System.Threading.Tasks;

namespace NReco.Data {
	
	public partial class DbDataAdapter {

		/// <summary>
		/// Represents select query (returned by <see cref="DbDataAdapter.Select"/> method).
		/// </summary>
		public abstract class SelectQuery {
			readonly protected DbDataAdapter Adapter;
			Func<string,string> FieldToPropertyMapper;

			internal SelectQuery(DbDataAdapter adapter, Func<string,string> fldToPropMapper) {
				Adapter = adapter;
				FieldToPropertyMapper = fldToPropMapper;
			}

			int DataReaderRecordOffset {
				get {
					return Adapter.ApplyOffset ? RecordOffset : 0;
				}
			}

			protected virtual int RecordOffset { get { return 0; } }

			protected virtual int RecordCount { get { return Int32.MaxValue; } }

			protected abstract IDbCommand GetSelectCmd();

			protected virtual string FirstFieldName { get { return null; } }

			/// <summary>
			/// Returns the first record from the query result. 
			/// </summary>
			/// <returns>depending on T, single value or all fields values from the first record</returns>
			public T Single<T>() where T : new() {
				T result = default(T);
				using (var selectCmd = GetSelectCmd()) {
					DataHelper.ExecuteReader(selectCmd, CommandBehavior.SingleRow, DataReaderRecordOffset, 1, 
						(rdr) => {
							result = Read<T>(rdr);
						} );
				}
				return result;
			}

			/// <summary>
			/// Asynchronously returns the first record from the query result. 
			/// </summary>
			/// <returns>depending on T, single value or all fields values from the first record</returns>
			public Task<T> SingleAsync<T>() where T : new() {
				return SingleAsync<T>(CancellationToken.None);
			}

			/// <summary>
			/// Asynchronously returns the first record from the query result. 
			/// </summary>
			/// <returns>depending on T, single value or all fields values from the first record</returns>
			public Task<T> SingleAsync<T>(CancellationToken cancel) where T : new() {
				using (var selectCmd = GetSelectCmd()) {
					return DataHelper.ExecuteReaderAsync<T>(selectCmd, CommandBehavior.SingleRow, DataReaderRecordOffset, 1,
						new SingleDataReaderResult<T>( Read<T> ), cancel
					);
				}
			}


			/// <summary>
			/// Returns dictionary with first record values.
			/// </summary>
			/// <returns>dictionary with field values or null if query returns zero records.</returns>
			public Dictionary<string,object> ToDictionary() {
				Dictionary<string,object> result = null;
				using (var selectCmd = GetSelectCmd()) {
					DataHelper.ExecuteReader(selectCmd, CommandBehavior.SingleRow, DataReaderRecordOffset, 1, 
						(rdr) => {
							result = ReadDictionary(rdr);
						} );
				}
				return result;
			}

			/// <summary>
			/// Asynchronously returns dictionary with first record values.
			/// </summary>
			public Task<Dictionary<string,object>> ToDictionaryAsync() {
				return ToDictionaryAsync(CancellationToken.None);
			}

			/// <summary>
			/// Asynchronously returns dictionary with first record values.
			/// </summary>
			public Task<Dictionary<string,object>> ToDictionaryAsync(CancellationToken cancel) {
				using (var selectCmd = GetSelectCmd()) {
					return DataHelper.ExecuteReaderAsync<Dictionary<string,object>>(
						selectCmd, CommandBehavior.SingleRow, DataReaderRecordOffset, 1,
						new SingleDataReaderResult<Dictionary<string,object>>( ReadDictionary ), cancel
					);
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
			public Task<List<Dictionary<string,object>>> ToDictionaryListAsync() {
				return ToDictionaryListAsync(CancellationToken.None);
			}

			/// <summary>
			/// Asynchronously a list of dictionaries with all query results.
			/// </summary>
			public Task<List<Dictionary<string,object>>> ToDictionaryListAsync(CancellationToken cancel) {
				using (var selectCmd = GetSelectCmd()) {
					return DataHelper.ExecuteReaderAsync<List<Dictionary<string,object>>>(selectCmd, CommandBehavior.Default, DataReaderRecordOffset, RecordCount,
						new ListDataReaderResult<Dictionary<string,object>>( ReadDictionary ), cancel
					);
				}
			}

			/// <summary>
			/// Returns a list with all query results.
			/// </summary>
			/// <returns>list with query results</returns>
			public List<T> ToList<T>()  where T : new() {
				var result = new List<T>();
				using (var selectCmd = GetSelectCmd()) {
					DataHelper.ExecuteReader(selectCmd, CommandBehavior.Default, DataReaderRecordOffset, RecordCount,
						(rdr) => {
							result.Add( Read<T>(rdr) );
						} );
				}
				return result;
			}

			/// <summary>
			/// Asynchronously returns a list with all query results.
			/// </summary>
			public Task<List<T>> ToListAsync<T>() where T : new() {
				return ToListAsync<T>(CancellationToken.None);
			}

			/// <summary>
			/// Asynchronously returns a list with all query results.
			/// </summary>
			public Task<List<T>> ToListAsync<T>(CancellationToken cancel) where T : new() {
				using (var selectCmd = GetSelectCmd()) {
					return DataHelper.ExecuteReaderAsync<List<T>>(selectCmd, CommandBehavior.Default, DataReaderRecordOffset, RecordCount,
						new ListDataReaderResult<T>( Read<T> ), cancel
					);
				}			
			}

			/// <summary>
			/// Returns all query results as <see cref="RecordSet"/>.
			/// </summary>
			public RecordSet ToRecordSet() {
				var result = new RecordSetDataReaderResult();
				using (var selectCmd = GetSelectCmd()) {
					DataHelper.ExecuteReader(selectCmd, CommandBehavior.Default, DataReaderRecordOffset, RecordCount,
						result.Read );
				}
				return result.Result;
			}

			/// <summary>
			/// Asynchronously returns all query results as <see cref="RecordSet"/>.
			/// </summary>
			public Task<RecordSet> ToRecordSetAsync() {
				return ToRecordSetAsync(CancellationToken.None);
			}

			/// <summary>
			/// Asynchronously returns all query results as <see cref="RecordSet"/>.
			/// </summary>
			public Task<RecordSet> ToRecordSetAsync(CancellationToken cancel) {
				using (var selectCmd = GetSelectCmd()) {
					return DataHelper.ExecuteReaderAsync<RecordSet>(selectCmd, CommandBehavior.Default, DataReaderRecordOffset, RecordCount,
						new RecordSetDataReaderResult(), cancel
					);
				}				
			}

			private T ChangeType<T>(object o, TypeCode typeCode) {
				return (T)Convert.ChangeType( o, typeCode, System.Globalization.CultureInfo.InvariantCulture );
			}

			private Dictionary<string,object> ReadDictionary(IDataReader rdr) {
				var dictionary = new Dictionary<string,object>(rdr.FieldCount);
				for (int i = 0; i < rdr.FieldCount; i++)
					dictionary[rdr.GetName(i)] = rdr.GetValue(i);
				return dictionary;
			}

			private T Read<T>(IDataReader rdr) where T : new() {
				var typeCode = Type.GetTypeCode(typeof(T));
				// handle primitive single-value result
				if (typeCode!=TypeCode.Object) {
					if (rdr.FieldCount==1) {
						return ChangeType<T>( rdr[0], typeCode);
					} else if (rdr.FieldCount>1) {
						var firstFld = FirstFieldName;
						var val = firstFld!=null ? rdr[firstFld] : rdr[0];
						return ChangeType<T>( val, typeCode);
					} else {
						return default(T);
					}
				}
				// T is a structure
				// special handling for dictionaries
				var type = typeof(T);
				if (type==typeof(IDictionary) || type==typeof(IDictionary<string,object>) || type==typeof(Dictionary<string,object>)) {
					return (T)((object)ReadDictionary(rdr));
				}
				// handle as poco model
				var res = new T();
				DataHelper.MapTo(rdr, res, FieldToPropertyMapper);
				return (T)res;
			}
		}
		
		internal class SelectQueryByQuery : SelectQuery {
			
			readonly Query Query;

			internal SelectQueryByQuery(DbDataAdapter adapter, Query q, Func<string,string> fldToPropMapper) 
				: base(adapter,fldToPropMapper) {
				Query = q;
			} 

			protected override IDbCommand GetSelectCmd() {
				var selectCmd = Adapter.CommandBuilder.GetSelectCommand(Query);
				Adapter.SetupCmd(selectCmd);
				return selectCmd;
			}

			protected override int RecordOffset { get { return Query.RecordOffset; } }

			protected override int RecordCount { get { return Query.RecordCount; } }

			protected override string FirstFieldName { 
				get { 
					return Query.Fields!=null && Query.Fields.Length>0 ? Query.Fields[0].Name : null; 
				} 
			}
		}
		
		internal class SelectQueryBySql : SelectQuery {
			
			readonly string Sql;
			object[] Parameters;

			internal SelectQueryBySql(DbDataAdapter adapter, string sql, object[] parameters, Func<string,string> fldToPropMapper) 
				: base(adapter,fldToPropMapper) {
				Sql = sql;
				Parameters = parameters;
			} 

			protected override IDbCommand GetSelectCmd() {
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
		
			
		
	}
}
