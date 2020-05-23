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
using System.Text.RegularExpressions;
using System.Text;

namespace NReco.Data
{
	/// <summary>
	/// Represents query aggregate field.
	/// </summary>
	public class QAggregateField : QField
	{
		/// <summary>
		/// Aggregate function name.
		/// </summary>
		public string AggregateFunction { get; private set; }

		/// <summary>
		/// Aggregate function arguments.
		/// </summary>
		public QField[] Arguments { get; private set; }

		/// <summary>
		/// Initializes a new instance of QAggregateField.
		/// </summary>
		/// <param name="fld">field name</param>
		/// <param name="aggregateFunction">aggregate function name</param>
		/// <param name="argFields">list of arguments for the aggregate function</param>
		public QAggregateField(string fld, string aggregateFunction, params QField[] argFields) 
			: base(null, fld, GetAggrExpr(aggregateFunction, argFields)) {
			AggregateFunction = aggregateFunction;
			Arguments = argFields;
		}

		static string GetAggrExpr(string aggrFunc, QField[] args) {
			var sb = new StringBuilder(aggrFunc);
			sb.Append('(');
			for (int i = 0; i < args.Length; i++) {
				if (i > 0)
					sb.Append(',');
				sb.Append(args[i].ToString());
			}
			sb.Append(')');
			return sb.ToString();
		}

		/// <summary>
		/// Returns a string representation of QField
		/// </summary>
		/// <returns>string in [prefix].[field name] format</returns>
		public override string ToString() {
			return String.IsNullOrEmpty(Prefix) ? Name : Prefix+"."+Name;
		}
		
	}
}
