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
		public class SelectQuery {
			DbDataAdapter Adapter;
			IDbCommand SelectCommand;
			Query Query;
			IDictionary<string,string> FieldToPropertyMap;

			internal SelectQuery(DbDataAdapter adapter, IDbCommand cmd, Query q, IDictionary<string,string> fldToPropMap) {
				Adapter = adapter;
				SelectCommand = cmd;
				Query = q;
				FieldToPropertyMap = fldToPropMap;
			}

			int DataReaderRecordOffset {
				get {
					return Adapter.ApplyOffset ? Query.RecordOffset : 0;
				}
			}

			/// <summary>
			/// Returns the first record from the query result. 
			/// </summary>
			/// <returns>depending on T, single value or all fields values from the first record</returns>
			public T Single<T>() {
				T result = default(T);
				DataHelper.ExecuteReader(SelectCommand, CommandBehavior.SingleRow, DataReaderRecordOffset, 1, 
					(rdr) => {
						result = Read<T>(rdr);
					} );
				return result;
			}

			/// <summary>
			/// Asynchronously returns the first record from the query result. 
			/// </summary>
			/// <returns>depending on T, single value or all fields values from the first record</returns>
			public Task<T> SingleAsync<T>() {
				return SingleAsync<T>(CancellationToken.None);
			}

			/// <summary>
			/// Asynchronously returns the first record from the query result. 
			/// </summary>
			/// <returns>depending on T, single value or all fields values from the first record</returns>
			public Task<T> SingleAsync<T>(CancellationToken cancel) {
				return DataHelper.ExecuteReaderAsync<T>(SelectCommand, CommandBehavior.SingleRow, DataReaderRecordOffset, 1,
					new SingleDataReaderResult<T>( Read<T> ), cancel
				);
			}


			/// <summary>
			/// Returns dictionary with first record values.
			/// </summary>
			/// <returns>dictionary with field values or null if query returns zero records.</returns>
			public Dictionary<string,object> ToDictionary() {
				Dictionary<string,object> result = null;
				DataHelper.ExecuteReader(SelectCommand, CommandBehavior.SingleRow, DataReaderRecordOffset, 1, 
					(rdr) => {
						result = ReadDictionary(rdr);
					} );
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
				return DataHelper.ExecuteReaderAsync<Dictionary<string,object>>(
					SelectCommand, CommandBehavior.SingleRow, DataReaderRecordOffset, 1,
					new SingleDataReaderResult<Dictionary<string,object>>( ReadDictionary ), cancel
				);
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
				return DataHelper.ExecuteReaderAsync<List<Dictionary<string,object>>>(SelectCommand, CommandBehavior.Default, DataReaderRecordOffset, Query.RecordCount,
					new ListDataReaderResult<Dictionary<string,object>>( ReadDictionary ), cancel
				);
			}

			/// <summary>
			/// Returns a list with all query results.
			/// </summary>
			/// <returns>list with query results</returns>
			public List<T> ToList<T>() {
				var result = new List<T>();
				DataHelper.ExecuteReader(SelectCommand, CommandBehavior.Default, DataReaderRecordOffset, Query.RecordCount,
					(rdr) => {
						result.Add( Read<T>(rdr) );
					} );
				return result;
			}

			/// <summary>
			/// Asynchronously returns a list with all query results.
			/// </summary>
			public Task<List<T>> ToListAsync<T>() {
				return ToListAsync<T>(CancellationToken.None);
			}

			/// <summary>
			/// Asynchronously returns a list with all query results.
			/// </summary>
			public Task<List<T>> ToListAsync<T>(CancellationToken cancel) {
				return DataHelper.ExecuteReaderAsync<List<T>>(SelectCommand, CommandBehavior.Default, DataReaderRecordOffset, Query.RecordCount,
					new ListDataReaderResult<T>( Read<T> ), cancel
				);				
			}

			/// <summary>
			/// Returns all query results as <see cref="RecordSet"/>.
			/// </summary>
			public RecordSet ToRecordSet() {
				var result = new RecordSetDataReaderResult();
				DataHelper.ExecuteReader(SelectCommand, CommandBehavior.Default, DataReaderRecordOffset, Query.RecordCount,
					result.Read );
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
				return DataHelper.ExecuteReaderAsync<RecordSet>(SelectCommand, CommandBehavior.Default, DataReaderRecordOffset, Query.RecordCount,
					new RecordSetDataReaderResult(), cancel
				);					
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

			private T Read<T>(IDataReader rdr) {
				var typeCode = Type.GetTypeCode(typeof(T));
				// handle primitive single-value result
				if (typeCode!=TypeCode.Object) {
					if (rdr.FieldCount==1) {
						return ChangeType<T>( rdr[0], typeCode);
					} else if (Query.Fields!=null && Query.Fields.Length>0) {
						return ChangeType<T>( rdr[Query.Fields[0].Name], typeCode);
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
				var res = Activator.CreateInstance(type);
				DataHelper.MapTo(rdr, res, FieldToPropertyMap);
				return (T)res;
			}
		}		
		
	}
}
