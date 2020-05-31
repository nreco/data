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
using System.Linq;
using System.Collections;
using System.Globalization;
using System.Text;

namespace NReco.Data.Relex {
	
	/// <summary>
	/// Relex expression builder by <see cref="Query"/> structure. 
	/// </summary>
	public class RelexBuilder {
		
		public string BuildRelex(QNode node) {
			InternalBuilder builder = new InternalBuilder();
			return builder.BuildExpression(node);
		}

        public string BuildRelex(Query node) {
            InternalBuilder builder = new InternalBuilder();
            return builder.BuildQueryString(node, false);
        }
		
		class InternalBuilder : SqlExpressionBuilder {

			public override string BuildExpression(QNode node) {
				if (node is Query)
					return BuildQueryString((Query)node, false);
				return base.BuildExpression(node);
			}

			protected override string BuildGroup(QGroupNode node) {
				var grp = base.BuildGroup(node);
				if (!String.IsNullOrEmpty( node.Name ) ) {
					return String.Format("(<{0}> {1})", node.Name, grp);
				} else return grp;
			}

			public string BuildQueryString(Query q, bool isNested) {
				string rootExpression = BuildExpression(q.Condition);
				if (rootExpression != null && rootExpression.Length > 0)
					rootExpression = String.Format("({0})", rootExpression);
				string fieldExpression = q.Fields != null ? 
					String.Join(",", q.Fields.Select(v=>(string)v).ToArray() ) : "*";
				if (q.Sort != null && q.Sort.Length > 0) {
					fieldExpression = String.Format("{0};{1}", fieldExpression, 
						String.Join(",", q.Sort.Select(v=>(string)v).ToArray()));
				}
				string limitExpression = isNested || (q.RecordOffset==0 && q.RecordCount==Int32.MaxValue) ? 
					String.Empty : String.Format("{{{0},{1}}}", q.RecordOffset, q.RecordCount);
				var tblName = q.Table.ToString();
				if (!DataHelper.IsSimpleIdentifier(q.Table.Name) || !DataHelper.IsSimpleIdentifier(q.Table.Alias))
					tblName = BuildValue(tblName)+":table";
				return String.Format("{0}{1}[{2}]{3}", tblName, rootExpression,
					fieldExpression, limitExpression);
			}

			static readonly string[] stringConditions = new string[] {
					"=", ">", ">=", "<", "<=", " in ", " like ", "="
			};
			static readonly Conditions[] enumConditions = new Conditions[] {
					Conditions.Equal, Conditions.GreaterThan, 
					Conditions.GreaterThan|Conditions.Equal,
					Conditions.LessThan, Conditions.LessThan|Conditions.Equal,
					Conditions.In, Conditions.Like, Conditions.Null
			};

			protected override string BuildCondition(QConditionNode node) {
				string lvalue = BuildValue(node.LValue);
				string rvalue = BuildValue(node.RValue);
				Conditions condition = (node.Condition | Conditions.Not) ^ Conditions.Not;
				string res = null;
				for (int i=0; i<enumConditions.Length; i++)
					if (enumConditions[i]==condition) {
						res = stringConditions[i];
						break; // first match
					}
				if (res==null)
					throw new ArgumentException("Invalid conditions set", condition.ToString());
				if ((node.Condition & Conditions.Not)==Conditions.Not)
					res = "!" + res;
				if ((node.Condition & Conditions.Null) == Conditions.Null)
					rvalue = "null";
				string result = String.Format("{0}{1}{2}", lvalue, res, rvalue);
				if ( !String.IsNullOrEmpty( node.Name ) )
					result = String.Format("(<{0}> {1})", node.Name, result);
				return result;
			}
			

			public override string BuildValue(IQueryValue value) {
				if (value is Query)
					return BuildQueryString((Query)value, true);
				if (value is QRawSql)
					return BuildValue(((QRawSql)value).SqlText) + ":sql";
				if ( (value is QField) && !DataHelper.IsSimpleIdentifier( ((QField)value).Name ) )
					return "\""+base.BuildValue(value).Replace("\"", "\"\"")+"\":field";
				return base.BuildValue(value);
			}

			protected override string BuildValue(QConst qConst) {
				if (qConst is QVar qVar) {
					return BuildValue(qVar.Name) + ":var";
				}

				object constValue = qConst.Value;
				if (constValue == null)
					return "null";
				
				// special processing for arrays
				if (constValue is IList)
					return BuildValue((IList)constValue);
				if (constValue is string && qConst.Type==TypeCode.String)
					return BuildValue((string)constValue);
				
				TypeCode constTypeCode = qConst.Type;
				string typeSuffix = constTypeCode!=TypeCode.Empty && constTypeCode!=TypeCode.Object ? ":"+constTypeCode.ToString() : String.Empty;
				return BuildValue( Convert.ToString(constValue, CultureInfo.InvariantCulture ) ) + typeSuffix;
			}

			protected override string BuildValue(IList list) {
				string[] paramNames = new string[list.Count];
				// in relexes only supported arrays that can be represented as comma-delimeted string 
				for (int i = 0; i < list.Count; i++)
					paramNames[i] = Convert.ToString(list[i]);
				return BuildValue( String.Join(",", paramNames) ) + ":string[]"; // TODO: array type suggestion logic!
			}			
			
			protected override string BuildValue(string str) {
				return "\""+str.Replace("\"", "\"\"")+"\"";
			}

		}		
		
	}
}
