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
using System.ComponentModel;

namespace NReco.Data
{
	/// <summary>
	/// Generaic SQL expressions builder.
	/// </summary>
	public class SqlExpressionBuilder : ISqlExpressionBuilder
	{
		
		public SqlExpressionBuilder()
		{
		}
		
		public virtual string BuildTableName(QTable tbl) {
			var tblName = BuildIdentifier( tbl.Name );
			if (!String.IsNullOrEmpty(tbl.Alias))
				tblName += " " + BuildIdentifier(tbl.Alias);
			return tblName;
		}

		public virtual string BuildExpression(QNode node) {
			if (node==null) return null;

			if (node is QRawSqlNode)
				return ((QRawSqlNode)node).SqlText;
			if (node is QGroupNode)
				return BuildGroup( (QGroupNode)node );
			if (node is QConditionNode)
				return BuildCondition( (QConditionNode)node );
			if (node is QNegationNode)
				return BuildNegation( (QNegationNode)node );
			
			throw new ArgumentException("Cannot build node with such type", node.GetType().ToString() );
		}
		
		protected virtual string BuildGroup(QGroupNode node) {
			// do not render empty group
			if (node.Nodes==null || node.Nodes.Count==0) return null;

			// if group contains only one node ignore group rendering logic
			if (node.Nodes.Count==1)
				return BuildExpression( node.Nodes[0] );

			var subNodes = new List<string>();
			foreach (QNode childNode in node.Nodes) {
				string childNodeExpression = BuildExpression( childNode );
				if (childNodeExpression!=null)
					subNodes.Add( "("+childNodeExpression+")" ); 
			}
			
			return String.Join(
				" "+node.GroupType.ToString()+" ",
				subNodes.ToArray() );
		}
		
		protected virtual string BuildNegation(QNegationNode node) {
			if (node.Nodes.Count==0) return null;
			string expression = BuildExpression(node.Nodes[0]);
			if (expression==null) return null;
			return String.Format("NOT({0})", expression);
		}

		protected virtual string BuildCondition(QConditionNode node) {
			Conditions condition = node.Condition & (
				Conditions.Equal | Conditions.GreaterThan |
				Conditions.In | Conditions.LessThan |
				Conditions.Like | Conditions.Null
				);
			string lvalue = BuildConditionLValue(node);
			string rvalue = BuildConditionRValue(node);
			string res = null;
			
			switch (condition) {
				case Conditions.GreaterThan:
					res = String.Format("{0}>{1}", lvalue, rvalue );
					break;
				case Conditions.LessThan:
					res = String.Format("{0}<{1}", lvalue, rvalue );
					break;
				case (Conditions.LessThan | Conditions.Equal):
					res = String.Format("{0}<={1}", lvalue, rvalue );
					break;
				case (Conditions.GreaterThan | Conditions.Equal):
					res = String.Format("{0}>={1}", lvalue, rvalue );
					break;
				case Conditions.Equal:
					res = String.Format("{0}{2}{1}", lvalue, rvalue, (node.Condition & Conditions.Not)!=0 ? "<>" : "=" );
					break;
				case Conditions.Like:
					res = String.Format("{0} LIKE {1}", lvalue, rvalue );
					break;
				case Conditions.In:
					res = String.IsNullOrWhiteSpace(rvalue) ? "0=1" : String.Format("{0} IN ({1})", lvalue, rvalue );
					break;
				case Conditions.Null:
					res = String.Format("{0} IS {1} NULL", lvalue, (node.Condition & Conditions.Not)!=0 ? "NOT" : "" );
					break;
				default:
					throw new ArgumentException("Invalid conditions set", condition.ToString() );
			}
				
			if ( (node.Condition & Conditions.Not)!=0
				&& (condition & Conditions.Null)==0
				&& (condition & Conditions.Equal)==0 )
				return String.Format("NOT ({0})", res);
			return res;
		}
		
		protected virtual string BuildConditionLValue(QConditionNode node) {
			return BuildValue( node.LValue);
		}

		protected virtual string BuildConditionRValue(QConditionNode node) {
			if ( (node.Condition & Conditions.In)==Conditions.In && IsMultivalueConst( node.RValue ) ) {
				var multiValue = ((QConst)node.RValue).Value as IList;
				return BuildValue(multiValue);
			}
			return BuildValue( node.RValue);
		}

		bool IsMultivalueConst(IQueryValue val) {
			return val is QConst && ((QConst)val).Value is IList;
		}

		public virtual string BuildValue(IQueryValue value) {
			if (value==null) return null;
			
			if (value is QField)
				return BuildValue( (QField)value );
			
			if (value is QConst)
				return BuildValue( (QConst)value );
			
			if (value is QRawSql)
				return ((QRawSql)value).SqlText;

			throw new NotSupportedException( "Unknown query value: "+ value.GetType().ToString() );
		}

		protected virtual string BuildValue(QConst value) {
			object constValue = value.Value;
			if (constValue==null)
				return "NULL";

			if (constValue is string)
				return BuildValue( (string)constValue );
									
			return Convert.ToString(constValue, System.Globalization.CultureInfo.InvariantCulture);
		}
		
		protected virtual string BuildValue(IList list) {
			string[] vals = new string[list.Count];
			for (int i=0; i<list.Count; i++)
				vals[i] = BuildValue( new QConst(list[i]) );
			return String.Join(",", vals);
		}
		
		protected virtual string BuildValue(string str) {
			return "'"+str.Replace(@"'", @"\'")+"'";
		}
		
		protected virtual string BuildValue(QField fieldValue) {
			if (!String.IsNullOrEmpty(fieldValue.Expression))
				return fieldValue.Expression;
			var name = BuildIdentifier(fieldValue.Name);
			if (!String.IsNullOrEmpty(fieldValue.Prefix))
				name = BuildIdentifier(fieldValue.Prefix)+"."+name;
			return name;
		}
		
		protected virtual string BuildIdentifier(string name) {
			return name;
		}

	}
}
