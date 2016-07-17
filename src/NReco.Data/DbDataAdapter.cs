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
using System.Text;
using System.Threading.Tasks;
using System.Data.Common;
using System.Data;

namespace NReco.Data {

	/// <summary>
	/// Data adapter between database and application data models. Implements select, insert, update and delete operations.
	/// </summary>
	public class DbDataAdapter {

		/// <summary>
		/// Gets <see cref="IDbConnection"/> associated with this data adapter.
		/// </summary>
		public IDbConnection Connection { get; private set; }

		/// <summary>
		/// Gets <see cref="IDbCommandBuilder"/> associated with this data adapter.
		/// </summary>
		public IDbCommandBuilder CommandBuilder { get; private set; }

		/// <summary>
		/// Gets or sets <see cref="IDbTransaction"/> initiated for the <see cref="Connection"/>.
		/// </summary>
		public IDbTransaction Transaction { get; private set; }

		/// <summary>
		/// Initializes a new instance of the DbDataAdapter.
		/// </summary>
		/// <param name="connection">database connection instance</param>
		/// <param name="cmdBuilder">command builder instance</param>
		public DbDataAdapter(IDbConnection connection, IDbCommandBuilder cmdBuilder) {
			Connection = connection;
			CommandBuilder = cmdBuilder;
		}

		private void InitCmd(IDbCommand cmd) {
			cmd.Connection = Connection;
			if (Transaction!=null)
				cmd.Transaction = Transaction;
		}

		/// <summary>
		/// Returns prepared select query. 
		/// </summary>
		/// <param name="q">query to execute</param>
		/// <returns>prepared select query</returns>
		public SelectQuery Select(Query q) {
			var selectCmd = CommandBuilder.GetSelectCommand(q);
			InitCmd(selectCmd);
			return new SelectQuery(this, selectCmd, q, null);
		}

		/// <summary>
		/// Returns prepared select query with POCO-model fields mapping configuration. 
		/// </summary>
		/// <param name="q">query to execute</param>
		/// <returns>prepared select query</returns>
		public SelectQuery Select(Query q, IDictionary<string,string> fieldToPropertyMap) {
			var selectCmd = CommandBuilder.GetSelectCommand(q);
			InitCmd(selectCmd);
			return new SelectQuery(this, selectCmd, q, fieldToPropertyMap);
		}

		public int Insert(string tableName, IEnumerable<KeyValuePair<string,IQueryValue>> data) {
			var insertCmd = CommandBuilder.GetInsertCommand(tableName, data);
			InitCmd(insertCmd);
			return ExecuteNonQuery(insertCmd);
		}

		public int Insert(string tableName, object pocoModel) {
			return Insert(tableName, DataHelper.GetChangeset( pocoModel, null) );
		}

		public int Insert(string tableName, object pocoModel, IDictionary<string,string> propertyToFieldMap) {
			return Insert(tableName, DataHelper.GetChangeset( pocoModel, propertyToFieldMap) );
		}

		public int Update(Query q, IEnumerable<KeyValuePair<string,IQueryValue>> data) {
			var updateCmd = CommandBuilder.GetUpdateCommand(q, data);
			InitCmd(updateCmd);
			return ExecuteNonQuery(updateCmd);
		}

		public int Update(Query q, object pocoModel) {
			return Update(q, DataHelper.GetChangeset( pocoModel, null) );
		}

		public int Update(Query q, object pocoModel, IDictionary<string,string> propertyToFieldMap) {
			return Update(q, DataHelper.GetChangeset( pocoModel, propertyToFieldMap) );
		}

		public int Delete(Query q) {
			var deleteCmd = CommandBuilder.GetDeleteCommand(q);
			InitCmd(deleteCmd);
			return ExecuteNonQuery(deleteCmd);
		}

		private int ExecuteNonQuery(IDbCommand cmd) {
			int affectedRecords = 0;
			DataHelper.EnsureConnectionOpen(Connection, () => {
				affectedRecords = cmd.ExecuteNonQuery();
			});
			return affectedRecords;
		}

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

			/// <summary>
			/// Returns the first record from the query results. 
			/// </summary>
			/// <returns>depending on T, single value or all fields values from the first record</returns>
			public T First<T>() {
				T result = default(T);
				var resTypeCode = Type.GetTypeCode(typeof(T));
				DataHelper.ExecuteReader(SelectCommand, CommandBehavior.SingleRow, Query.RecordOffset, 1, 
					(rdr) => {
						result = Read<T>(resTypeCode, rdr);
					} );
				return result;
			}

			/// <summary>
			/// Returns dictionary with first record values.
			/// </summary>
			/// <returns>dictionary with field values or null if query returns zero records.</returns>
			public Dictionary<string,object> ToDictionary() {
				Dictionary<string,object> result = null;
				DataHelper.ExecuteReader(SelectCommand, CommandBehavior.SingleRow, Query.RecordOffset, 1, 
					(rdr) => {
						result = ReadDictionary(rdr);
					} );
				return result;
			}

			/// <summary>
			/// Returns a list with all query results.
			/// </summary>
			/// <returns>list with query results</returns>
			public List<T> ToList<T>() {
				var result = new List<T>();
				var resTypeCode = Type.GetTypeCode(typeof(T));
				DataHelper.ExecuteReader(SelectCommand, CommandBehavior.Default, Query.RecordOffset, Query.RecordCount,
					(rdr) => {
						result.Add( Read<T>(resTypeCode, rdr) );
					} );
				return result;
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

			private T Read<T>(TypeCode typeCode, IDataReader rdr) {
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
