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

using System.Data;

namespace NReco.Data {

	/// <summary>
	/// Represents query result that can be mapped to <see cref="DataTable"/>.
	/// </summary>
	/// <remarks>This interface is not available in netstandard1.5 build.</remarks>
	public interface IQueryDataTableResult {

		/// <summary>
		/// Returns all query results as <see cref="DataTable"/>.
		/// </summary>
		DataTable ToDataTable();

		/// <summary>
		/// Asynchronously returns all query results as <see cref="DataTable"/>.
		/// </summary>
		Task<DataTable> ToDataTableAsync(CancellationToken cancel = default(CancellationToken));

		/// <summary>
		/// Loads all query results into specified <see cref="DataTable"/>.
		/// </summary>
		DataTable ToDataTable(DataTable tbl);

		/// <summary>
		/// Asynchronously loads all query results into specified <see cref="DataTable"/>.
		/// </summary>
		Task<DataTable> ToDataTableAsync(DataTable tbl, CancellationToken cancel = default(CancellationToken));

	}
}
