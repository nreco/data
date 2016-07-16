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
using System.ComponentModel;
using System.Text.RegularExpressions;

namespace NReco.Data
{
	/// <summary>
	/// Represents query table information
	/// </summary>
	//[Serializable]
	public class QTable
	{

		/// <summary>
		/// Get source name (string identifier)
		/// </summary>
		public string Name { get; private set; }

		/// <summary>
		/// Get source name alias used in query nodes
		/// </summary>
		public string Alias { get; private set; }

		/// <summary>
		/// Initializes a new instance of the QSource with the specified source name
		/// </summary>
		/// <param name="tableName">source name string</param>
		public QTable(string tableName) {
			int dotIdx = tableName.LastIndexOf('.'); // allow dot in table name (alias for this case is required), like dbo.users.u
			if (dotIdx >= 0) {
				Name = tableName.Substring(0, dotIdx);
				Alias = tableName.Substring(dotIdx+1);
			}
			else {
				Name = tableName;
				Alias = null;
			}

		}

		/// <summary>
		/// Initializes a new instance of the QSource with the specified source name and alias
		/// </summary>
		/// <param name="tableName">source name string</param>
		/// <param name="alias">alias string</param>
		public QTable(string tableName, string alias) {
			Name = tableName;
			Alias = alias;
		}
		
		/// <summary>
		/// Returns a string representation of this QSource
		/// </summary>
		/// <returns>string that represents QSource in [name].[alias] format</returns>
		public override string ToString() {
			return String.IsNullOrEmpty(Alias) ? Name : Name+"."+Alias;
		}

		public static implicit operator QTable(string value) {
			return new QTable(value);
		}
		public static implicit operator string(QTable value) {
			return value.ToString();
		}
		
	}
}
