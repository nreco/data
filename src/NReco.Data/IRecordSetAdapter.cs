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
using System.Data;
using System.Data.Common;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NReco.Data
{

	/// <summary>
	/// Represents data adapter between database and <see cref="RecordSet"/> models.
	/// </summary>
	public interface IRecordSetAdapter {
		
		/// <summary>
		/// Returns all query results as <see cref="RecordSet"/>.
		/// </summary>
		RecordSet Select(Query q);

		/// <summary>
		/// Asynchronously returns all query results as <see cref="RecordSet"/>.
		/// </summary>
		Task<RecordSet> SelectAsync(Query q);

		/// <summary>
		/// Commits <see cref="RecordSet"/> changes to the database.
		/// </summary>
		int Update(string table, RecordSet rs);

		/// <summary>
		/// An asynchronous version of <see cref="Update(string, RecordSet)"/>
		/// </summary>
		Task<int> UpdateAsync(string table, RecordSet rs);
	}

}
