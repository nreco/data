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
using System.Data;
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

		internal static void ExecuteReader(IDbCommand cmd, CommandBehavior cmdBehaviour, int recordOffset, int recordCount, Action<IDataReader> recordHandler) {
			EnsureConnectionOpen(cmd.Connection, () => {
				using (var rdr = cmd.ExecuteReader(cmdBehaviour)) {
					int index = 0;
					int processed = 0;
					while (rdr.Read() && processed < recordCount) {
						if (index>=recordOffset) {
							processed++;
							recordHandler(rdr);
						}
						index++;
					}
				}
			});
		}

		internal static void MapTo(IDataRecord record, object o, IDictionary<string,string> fieldToPropertyMap) {
			var type = o.GetType();
			for (int i = 0; i < record.FieldCount; i++) {
				var fieldName = record.GetName(i);
				var fieldValue = record.GetValue(i);
				
				var propName = fieldToPropertyMap!=null && fieldToPropertyMap.ContainsKey(fieldName) ? fieldToPropertyMap[fieldName] : fieldName;
				var pInfo =type.GetProperty(propName);
				if (pInfo!=null) {
					if (IsNullOrDBNull(fieldValue)) {
						fieldValue = null;
						if (Nullable.GetUnderlyingType(pInfo.PropertyType) == null && pInfo.PropertyType._IsValueType() )
							fieldValue = Activator.CreateInstance(pInfo.PropertyType);
					} else {
						var propType = pInfo.PropertyType;
						if (Nullable.GetUnderlyingType(propType) != null)
							propType = Nullable.GetUnderlyingType(propType);

						if (propType._IsEnum()) {
							fieldValue = Enum.Parse(propType, fieldValue.ToString(), true); 
						} else {
							fieldValue = Convert.ChangeType(fieldValue, propType, System.Globalization.CultureInfo.InvariantCulture);
						}
					}
					pInfo.SetValue(o, fieldValue, null);
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

		internal static IEnumerable<KeyValuePair<string, IQueryValue>> GetChangeset(object o, IDictionary<string,string> propertyToFieldMap) {
			if (o == null)
				yield break;
			var oType = o.GetType();
			foreach (var p in oType.GetProperties()) {
				var pVal = p.GetValue(o, null);
				var qVal = pVal is IQueryValue ? (IQueryValue)pVal : new QConst(pVal);
				var fldName = p.Name;
				if (propertyToFieldMap!=null)
					if (!propertyToFieldMap.TryGetValue(fldName, out fldName))
						continue;
				yield return new KeyValuePair<string, IQueryValue>(fldName, qVal );
			}
		}


	}
}
