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
using System.Threading;
using System.Threading.Tasks;
using System.Data;
using System.Data.Common;
using System.Reflection;
using System.IO;

namespace NReco.Data {
	
	internal static class DataHelper {

		internal static bool IsNullOrDBNull(object v) {
			return v==null || DBNull.Value.Equals(v);
		}

		internal static void EnsureConnectionOpen(IDbConnection connection, Action a) {
			bool closeConn = false;
			if (connection.State != ConnectionState.Open) {
				connection.Open();
				closeConn = true;
			}
			try {
				a();
			} finally {
				if (closeConn && connection.State!=ConnectionState.Closed)
					connection.Close();
			}
		}

		internal static QNode MapQValue(QNode qNode, Func<IQueryValue,IQueryValue> mapFunc) {
			if (qNode is QGroupNode) {
				var group = new QGroupNode((QGroupNode)qNode);
				for (int i = 0; i < group.Nodes.Count; i++)
					group.Nodes[i] = MapQValue(group.Nodes[i], mapFunc);
				return group;
			}
			if (qNode is QConditionNode) {
				var origCndNode = (QConditionNode)qNode;
				var cndNode = new QConditionNode(origCndNode.Name,
						mapFunc(origCndNode.LValue),
						origCndNode.Condition,
						mapFunc(origCndNode.RValue));
				return cndNode;
			}
			if (qNode is QNegationNode) {
				var negNode = new QNegationNode((QNegationNode)qNode);
				for (int i = 0; i < negNode.Nodes.Count; i++)
					negNode.Nodes[i] = MapQValue(negNode.Nodes[i], mapFunc);
				return negNode;
			}
			return qNode;
		}

		internal static RecordSet GetRecordSetByReader(IDataReader rdr) {
			var rsCols = new List<RecordSet.Column>(rdr.FieldCount);
			var rsPkCols = new List<RecordSet.Column>();

			#if NET_STANDARD
			// lets populate data schema
			if (rdr is DbDataReader) {
				var dbRdr = (DbDataReader)rdr;
				if (dbRdr.CanGetColumnSchema()) {
					foreach (var dbCol in dbRdr.GetColumnSchema()) {
						var c = new RecordSet.Column(dbCol);
						// if this is default implementation resolve DataType with GetFieldType
						if (c.DataType==null) {
							c.DataType = rdr.GetFieldType(rdr.GetOrdinal(c.Name));
						}
						rsCols.Add(c);
						if (dbCol.IsKey.HasValue && dbCol.IsKey.Value)
							rsPkCols.Add(c);
					}
				}
			}
			#endif

			if (rsCols.Count==0) {
				// lets suggest columns by standard IDataReader interface
				for (int i=0; i<rdr.FieldCount; i++) {
					var colName = rdr.GetName(i);
					var colType = rdr.GetFieldType(i);
					rsCols.Add( new RecordSet.Column(colName, colType) );
				}
			}
			var rs = new RecordSet(rsCols.ToArray(), 1);
			if (rsPkCols.Count>0)
				rs.PrimaryKey = rsPkCols.ToArray();
			return rs;
		}

		internal static void EnsureDataTableColumnsByReader(DataTable tbl, IDataReader rdr) {
			#if NET_STANDARD
			// lets populate data schema
			if (rdr is DbDataReader) {
				var dbRdr = (DbDataReader)rdr;
				if (dbRdr.CanGetColumnSchema()) {
					var pkCols = new List<DataColumn>();
					foreach (var dbCol in dbRdr.GetColumnSchema()) {
						DataColumn col = null;
						if (!tbl.Columns.Contains(dbCol.ColumnName)) {
							col = tbl.Columns.Add(dbCol.ColumnName, dbCol.DataType);
							if (dbCol.AllowDBNull.HasValue)
								col.AllowDBNull = dbCol.AllowDBNull.Value;
							if (dbCol.IsAutoIncrement.HasValue && dbCol.IsAutoIncrement.Value)
								col.AutoIncrement = true;
							if (dbCol.IsReadOnly.HasValue)
								col.ReadOnly = dbCol.IsReadOnly.Value;
						} else {
							col = tbl.Columns[dbCol.ColumnName];
						}
						if (dbCol.IsKey.HasValue && dbCol.IsKey.Value)
							pkCols.Add(col);
					}
					if (pkCols.Count > 0 && (tbl.PrimaryKey == null || tbl.PrimaryKey.Length == 0))
						tbl.PrimaryKey = pkCols.ToArray();
				}
			}
			#endif

			// lets suggest columns by standard IDataReader interface
			for (int i = 0; i < rdr.FieldCount; i++) {
				var colName = rdr.GetName(i);
				var colType = rdr.GetFieldType(i);
				if (!tbl.Columns.Contains(colName)) {
					tbl.Columns.Add(colName, colType);
				}
			}

		}

		internal static IEnumerable<KeyValuePair<string, IQueryValue>> GetChangeset(IDictionary data) {
			if (data == null)
				yield break;
			foreach (DictionaryEntry entry in data) {
				var qVal = entry.Value is IQueryValue ? (IQueryValue)entry.Value : new QConst(entry.Value);
				yield return new KeyValuePair<string, IQueryValue>( Convert.ToString( entry.Key ), qVal );
			}
		}

		internal static IEnumerable<KeyValuePair<string, IQueryValue>> GetChangeset(IDictionary<string,object> data) {
			if (data == null)
				yield break;
			foreach (var entry in data) {
				var qVal = entry.Value is IQueryValue ? (IQueryValue)entry.Value : new QConst(entry.Value);
				yield return new KeyValuePair<string, IQueryValue>( entry.Key, qVal );
			}
		}

		internal static IEnumerable<KeyValuePair<string, IQueryValue>> GetChangeset(object o, DataMapper dtoMapper) {
			if (o == null)
				yield break;
			var oType = o.GetType();
			var schema = (dtoMapper??DataMapper.Instance).GetSchema(oType);
			foreach (var columnMapping in schema.Columns) {
				if (columnMapping.IsReadOnly || columnMapping.GetVal==null)
					continue;
				var pVal = columnMapping.GetVal(o);
				var qVal = pVal is IQueryValue ? (IQueryValue)pVal : new QConst(pVal);
				var fldName = columnMapping.ColumnName;
				yield return new KeyValuePair<string, IQueryValue>(fldName, qVal );
			}
		}

		internal static bool IsSimpleIdentifier(string s) {
			if (s!=null)
				for (int i=0; i<s.Length; i++) {
					var ch = s[i];
					if (!Char.IsLetterOrDigit(ch) && ch!='-' && ch!='_')
						return false;
				}
			return true;			
		}

	}
}
