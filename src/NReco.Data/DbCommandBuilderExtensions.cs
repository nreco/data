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
using System.Collections;
using System.Linq;
using System.Text;
using System.Data.Common;
using System.Data;
using System.Reflection;

namespace NReco.Data {
	
	/// <summary>
	/// Extension methods for <see cref="IDbCommandBuilder"/> interface.
	/// </summary>
	public static class DbCommandBuilderExtensions {

		internal static IEnumerable<KeyValuePair<string, IQueryValue>> GetChangeset(IDictionary<string,object> data) {
			if (data == null)
				yield break;
			foreach (var entry in data) {
				var qVal = entry.Value is IQueryValue ? (IQueryValue)entry.Value : new QConst(entry.Value);
				yield return new KeyValuePair<string, IQueryValue>( entry.Key, qVal );
			}
		}

		internal static IEnumerable<KeyValuePair<string, IQueryValue>> GetChangeset(object o) {
			if (o == null)
				yield break;
			var oType = o.GetType();
			foreach (var p in oType.GetProperties()) {
				var pVal = p.GetValue(o, null);
				var qVal = pVal is IQueryValue ? (IQueryValue)pVal : new QConst(pVal);
				yield return new KeyValuePair<string, IQueryValue>( p.Name, qVal );
			}
		}

		public static IDbCommand GetUpdateCommand(this IDbCommandBuilder cmdBuilder, Query q, IDictionary<string,object> data) {
			return cmdBuilder.GetUpdateCommand(q, GetChangeset(data) );
		}
		public static IDbCommand GetUpdateCommand(this IDbCommandBuilder cmdBuilder, Query q, object poco) {
			return cmdBuilder.GetUpdateCommand(q, GetChangeset(poco) );
		}

		public static IDbCommand GetInsertCommand(this IDbCommandBuilder cmdBuilder, string table, IDictionary<string,object> data) {
			return cmdBuilder.GetInsertCommand(table, GetChangeset(data) );
		}
		public static IDbCommand GetInsertCommand(this IDbCommandBuilder cmdBuilder, string table, object poco) {
			return cmdBuilder.GetInsertCommand(table, GetChangeset(poco) );
		}

	}
}
