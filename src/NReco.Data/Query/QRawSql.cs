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
	/// Represents raw SQL query value
	/// </summary>
	//[Serializable]
	public class QRawSql : IQueryValue {

		/// <summary>
		/// Get SQL text
		/// </summary>
		public string SqlText => GetSqlText(ResolveToSqlConstant);

		string sqlTemplate;
		object[] args;

		/// <summary>
		/// Initializes a new instance of the QRawSql with specfield SQL.
		/// </summary>
		/// <param name="sqlText">Raw SQL</param>
		public QRawSql(string sqlText) {
			sqlTemplate = sqlText;
		}

		/// <summary>
		/// Initializes a new instance of the QRawSql with specfield SQL template and arguments.
		/// </summary>
		/// <param name="sqlTemplate">SQL template that can be resolved with <code>String.Format</code></param>
		/// <param name="args">arguments that should be used to get final SQL text</param>
		public QRawSql(string sqlTemplate, object[] args) {
			this.sqlTemplate = sqlTemplate;
			this.args = args;
		}

		/// <summary>
		/// Returns SQL text where arguments are resolved with specified handler.
		/// </summary>
		public string GetSqlText(Func<object, string> resolveArgValue) {
			if (args == null || args.Length == 0)
				return sqlTemplate;
			var resolvedArgs = new object[args.Length];
			for (int i = 0; i < args.Length; i++)
				resolvedArgs[i] = resolveArgValue(args[i]);
			return String.Format(sqlTemplate, resolvedArgs);
		}

		string ResolveToSqlConstant(object o) {
			if (o == null || DBNull.Value.Equals(o))
				return "NULL";
			var val = Convert.ToString(o, System.Globalization.CultureInfo.InvariantCulture);
			return "'" + val.Replace(@"'", @"\'") + "'";
		}

	}
}
