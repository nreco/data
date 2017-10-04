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
	/// Represents query result that can be mapped to POCO model.
	/// </summary>
	public interface IQueryModelResult {

		/// <summary>
		/// Returns the first record from the query result. 
		/// </summary>
		/// <returns>depending on T, single value or all fields values from the first record</returns>
		T Single<T>();

		/// <summary>
		/// Asynchronously returns the first record from the query result. 
		/// </summary>
		/// <returns>depending on T, single value or all fields values from the first record</returns>
		Task<T> SingleAsync<T>(CancellationToken cancel = default(CancellationToken));

		/// <summary>
		/// Returns a list with all query results.
		/// </summary>
		/// <returns>list with query results</returns>
		List<T> ToList<T>();

		/// <summary>
		/// Asynchronously returns a list with all query results.
		/// </summary>
		Task<List<T>> ToListAsync<T>(CancellationToken cancel = default(CancellationToken));

	}
}
