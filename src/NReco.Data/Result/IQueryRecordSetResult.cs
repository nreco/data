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
	/// Represents query result that can be mapped to <see cref="RecordSet"/>.
	/// </summary>
	public interface IQueryRecordSetResult {

		/// <summary>
		/// Returns all query results as <see cref="RecordSet"/>.
		/// </summary>
		RecordSet ToRecordSet();

		/// <summary>
		/// Asynchronously returns all query results as <see cref="RecordSet"/>.
		/// </summary>
		Task<RecordSet> ToRecordSetAsync();

		/// <summary>
		/// Asynchronously returns all query results as <see cref="RecordSet"/>.
		/// </summary>
		Task<RecordSet> ToRecordSetAsync(CancellationToken cancel);

	}
}
