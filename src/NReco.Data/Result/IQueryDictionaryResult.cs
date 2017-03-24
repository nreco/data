#region License
/*
 * NReco Data library (http://www.nrecosite.com/)
 * Copyright 2017 Vitaliy Fedorchenko
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
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NReco.Data {

	/// <summary>
	/// Represents query result that can be mapped to dictionary.
	/// </summary>
	public interface IQueryDictionaryResult {

		/// <summary>
		/// Returns dictionary with first record values.
		/// </summary>
		/// <returns>dictionary with field values or null if query returns zero records.</returns>
		Dictionary<string, object> ToDictionary();

		/// <summary>
		/// Asynchronously returns dictionary with first record values.
		/// </summary>
		Task<Dictionary<string, object>> ToDictionaryAsync();

		/// <summary>
		/// Asynchronously returns dictionary with first record values.
		/// </summary>
		Task<Dictionary<string, object>> ToDictionaryAsync(CancellationToken cancel);

		/// <summary>
		/// Returns a list of dictionaries with all query results.
		/// </summary>
		List<Dictionary<string, object>> ToDictionaryList();

		/// <summary>
		/// Asynchronously a list of dictionaries with all query results.
		/// </summary>
		Task<List<Dictionary<string, object>>> ToDictionaryListAsync();

		/// <summary>
		/// Asynchronously a list of dictionaries with all query results.
		/// </summary>
		Task<List<Dictionary<string, object>>> ToDictionaryListAsync(CancellationToken cancel);

	}
}
