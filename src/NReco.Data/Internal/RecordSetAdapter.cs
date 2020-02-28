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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Data;
using System.Data.Common;

namespace NReco.Data {


	internal class RecordSetAdapter : IDisposable {
		RecordSet RS;
		DbDataAdapter DbAdapter;
		string TableName;
		IDbCommand InsertCmd = null;
		IDbCommand UpdateCmd = null;
		IDbCommand DeleteCmd = null;
		RecordSet.Column[] setColumns;
		RecordSet.Column autoIncrementCol;

		internal RecordSetAdapter(DbDataAdapter dbAdapter, string tblName, RecordSet rs) {
			RS = rs;
			DbAdapter = dbAdapter;
			TableName = tblName;
			setColumns = RS.Columns.Where(c=>!c.ReadOnly).ToArray();
			autoIncrementCol = RS.Columns.Where(c=>c.AutoIncrement).FirstOrDefault();
		}

		IEnumerable<KeyValuePair<string,IQueryValue>> GetSetColumns() {
			return setColumns.Select( c => new KeyValuePair<string,IQueryValue>(c.Name, new QVar(c.Name).Set(null) ) );
		}
		Query GetPkQuery() {
			var q = new Query(new QTable(TableName, null));
			var grpAnd = QGroupNode.And();
			q.Condition = grpAnd;
			foreach (var pkCol in RS.PrimaryKey) {
				grpAnd.Nodes.Add( (QField)pkCol.Name == new QVar(pkCol.Name).Set(null) );
			}
			return q;
		}

		bool IsBinaryType(Type t) {
			return t==typeof(byte[])
				|| t==typeof(System.Data.SqlTypes.SqlBytes) || t==typeof(System.Data.SqlTypes.SqlBinary)	;
		}

		void FillCmdParams(IDbCommand cmd, RecordSet.Row row) {
			foreach (DbParameter p in cmd.Parameters) {
				if (p.SourceColumn!=null) {
					var rowVal = row[p.SourceColumn];
					if (rowVal==null) {
						p.Value = DBNull.Value;
						var col = RS.Columns[p.SourceColumn];
						if (IsBinaryType(col.DataType))
							p.DbType = DbType.Binary;
					} else {
						p.Value = rowVal;
					}
				}
			}			
		}

		void PrepareInsertCmd(RecordSet.Row row) {
			if (InsertCmd==null) {
				InsertCmd = DbAdapter.CommandBuilder.GetInsertCommand(TableName, GetSetColumns() );
				InsertCmd.Connection = DbAdapter.Connection;
				InsertCmd.Transaction = DbAdapter.Transaction;
			}
			FillCmdParams(InsertCmd, row);		
		}

		int ExecuteInsertCmd(RecordSet.Row row) {
			PrepareInsertCmd(row);
			var affected = InsertCmd.ExecuteNonQuery();
			if (autoIncrementCol!=null) {
				row[autoIncrementCol.Name] = DbAdapter.CommandBuilder.DbFactory.GetInsertId(DbAdapter.Connection, DbAdapter.Transaction);
			}
			return affected;
		}

		async Task<int> ExecuteInsertCmdAsync(RecordSet.Row row, CancellationToken cancel) {
			PrepareInsertCmd(row);
			var affected = await InsertCmd.ExecuteNonQueryAsync(cancel).ConfigureAwait(false);
			if (autoIncrementCol!=null) {
				row[autoIncrementCol.Name] = DbAdapter.CommandBuilder.DbFactory.GetInsertId(DbAdapter.Connection, DbAdapter.Transaction);
			}
			return affected;
		}

		void PrepareUpdateCmd(RecordSet.Row row) {
			if (UpdateCmd==null) {
				UpdateCmd = DbAdapter.CommandBuilder.GetUpdateCommand( GetPkQuery(), GetSetColumns() );
				UpdateCmd.Connection = DbAdapter.Connection;
				UpdateCmd.Transaction = DbAdapter.Transaction;
			}
			FillCmdParams(UpdateCmd, row);
		}

		int ExecuteUpdateCmd(RecordSet.Row row) {
			PrepareUpdateCmd(row);
			return UpdateCmd.ExecuteNonQuery();
		}

		Task<int> ExecuteUpdateCmdAsync(RecordSet.Row row, CancellationToken cancel) {
			PrepareUpdateCmd(row);
			return UpdateCmd.ExecuteNonQueryAsync(cancel);
		}

		void PrepareDeleteCmd(RecordSet.Row row) {
			if (DeleteCmd==null) {
				DeleteCmd = DbAdapter.CommandBuilder.GetDeleteCommand( GetPkQuery() );
				DeleteCmd.Connection = DbAdapter.Connection;
				DeleteCmd.Transaction = DbAdapter.Transaction;
			}
			FillCmdParams(DeleteCmd, row);
		}

		int ExecuteDeleteCmd(RecordSet.Row row) {
			PrepareDeleteCmd(row);
			return DeleteCmd.ExecuteNonQuery();
		}

		Task<int> ExecuteDeleteCmdAsync(RecordSet.Row row, CancellationToken cancel) {
			PrepareDeleteCmd(row);	
			return DeleteCmd.ExecuteNonQueryAsync(cancel);					
		}

		internal int Update() {
			int affected = 0;
			DataHelper.EnsureConnectionOpen( DbAdapter.Connection, () => {
				foreach (var row in RS) {
					if ( (row.State&RecordSet.RowState.Added) == RecordSet.RowState.Added) {
						affected += ExecuteInsertCmd(row);
					} else if ( (row.State&RecordSet.RowState.Deleted) == RecordSet.RowState.Deleted ) {
						affected += ExecuteDeleteCmd(row);
					} else if ( (row.State&RecordSet.RowState.Modified) == RecordSet.RowState.Modified ) {
						affected += ExecuteUpdateCmd(row);
					}
					row.AcceptChanges();
				}
			});
			return affected;
		}

		internal async Task<int> UpdateAsync(CancellationToken cancel) {
			int affected = 0;
			var isOpenConn = DbAdapter.Connection.State != ConnectionState.Closed;
			if (!isOpenConn) {
				await DbAdapter.Connection.OpenAsync(cancel).ConfigureAwait(false);
			}
			try {
				foreach (var row in RS) {
					if ( (row.State&RecordSet.RowState.Added) == RecordSet.RowState.Added) {
						affected += await ExecuteInsertCmdAsync(row,cancel).ConfigureAwait(false);
					} else if ( (row.State&RecordSet.RowState.Deleted) == RecordSet.RowState.Deleted ) {
						affected += await ExecuteDeleteCmdAsync(row,cancel).ConfigureAwait(false);
					} else if ( (row.State&RecordSet.RowState.Modified) == RecordSet.RowState.Modified ) {
						affected += await ExecuteUpdateCmdAsync(row,cancel).ConfigureAwait(false);
					}
					row.AcceptChanges();
				}
			} finally {
				if (!isOpenConn)
					DbAdapter.Connection.Close();
			}
			return affected;
		}

		public void Dispose() {
			RS = null;
			DbAdapter = null;
			setColumns = null;
			autoIncrementCol = null;
			if (InsertCmd!=null) {
				InsertCmd.Dispose();
				InsertCmd = null;
			}
			if (UpdateCmd!=null) {
				UpdateCmd.Dispose();
				UpdateCmd = null;
			}
			if (DeleteCmd!=null) {
				DeleteCmd.Dispose();
				DeleteCmd = null;
			}
		}	

	}

}
