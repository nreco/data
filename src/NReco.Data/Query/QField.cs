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

namespace NReco.Data
{
	/// <summary>
	/// Represents query field.
	/// </summary>
	//[Serializable]
	public class QField : IQueryValue
	{
		/// <summary>
		/// Get field name
		/// </summary>
		public string Name { get; private set; }

		/// <summary>
		/// Get field prefix (usually matches query source name alias)
		/// </summary>
		public string Prefix { get; private set; }

		/// <summary>
		/// Get optional expression string that represents calculated field
		/// </summary>
		public string Expression { get; private set; }

		private static char[] ExpressionChars = new[] { '(', ')','+','-','*','/' };

		/// <summary>
		/// Initializes a new instance of QField with specified field name
		/// </summary>
		/// <remarks>If field name contains expression specific characters (like '(',')','*') it is treated as calculated field expression</remarks>
		/// <param name="fld">field name</param>
		public QField(string fld) {
			if (fld.IndexOfAny(ExpressionChars) >= 0) {
				Expression = fld;
			}
			SetName(fld);
		}

		/// <summary>
		/// Initializes a new instance of QField with specified field name and expression
		/// </summary>
		/// <param name="fld">field name</param>
		/// <param name="expression">expression string</param>
		public QField(string fld, string expression) {
			SetName(fld);
			Expression = expression;
		}

		/// <summary>
		/// Initializes a new instance of QField with specified field prefix, name and expression
		/// </summary>
		/// <param name="prefix">field prefix</param>
		/// <param name="fld">field name</param>
		/// <param name="expression">expression string</param>
		public QField(string prefix, string fld, string expression) {
			Prefix = prefix;
			Name = fld;
			Expression = expression;
		}

		private void SetName(string nameStr) {
			var dotIdx = nameStr!=null ? nameStr.LastIndexOf('.') : -1;
			if (dotIdx > 0) {
				Prefix = nameStr.Substring(0, dotIdx);
				Name = nameStr.Substring(dotIdx + 1);
			} else {
				Name = nameStr;
			}
		}

		/// <summary>
		/// Returns a string representation of QField
		/// </summary>
		/// <returns>string in [prefix].[field name] format</returns>
		public override string ToString() {
			return String.IsNullOrEmpty(Prefix) ? Name : Prefix+"."+Name;
		}

		public static implicit operator QField(string fld) {
			return new QField(fld);
		}
		public static implicit operator string(QField fld) {
			return fld.ToString();
		}
		
		public static QConditionNode operator ==(QField lvalue, IQueryValue rvalue) {
			if (rvalue == null || ((rvalue is QConst) && ((QConst)rvalue).Value == null)) {
				return new QConditionNode(lvalue, Conditions.Null, null);
			}
			return new QConditionNode( lvalue, Conditions.Equal, rvalue );
		}		

		public static QConditionNode operator !=(QField lvalue, IQueryValue rvalue) {
			if (rvalue == null || ((rvalue is QConst) && ((QConst)rvalue).Value == null)) {
				return new QConditionNode(lvalue, Conditions.Null|Conditions.Not, null);
			}
			return new QConditionNode( lvalue, Conditions.Equal|Conditions.Not, rvalue );
		}		

		public static QConditionNode operator <(QField lvalue, IQueryValue rvalue) {
			return new QConditionNode( lvalue, Conditions.LessThan, rvalue );
		}		

		public static QConditionNode operator >(QField lvalue, IQueryValue rvalue) {
			return new QConditionNode( lvalue, Conditions.GreaterThan, rvalue );
		}		

		public static QConditionNode operator >=(QField lvalue, IQueryValue rvalue) {
			return new QConditionNode( lvalue, Conditions.GreaterThan|Conditions.Equal, rvalue );
		}		

		public static QConditionNode operator <=(QField lvalue, IQueryValue rvalue) {
			return new QConditionNode( lvalue, Conditions.LessThan|Conditions.Equal, rvalue );
		}		

		public static QConditionNode operator %(QField lvalue, IQueryValue rvalue) {
			return new QConditionNode( lvalue, Conditions.Like, rvalue );
		}		
		
		
	}
}
