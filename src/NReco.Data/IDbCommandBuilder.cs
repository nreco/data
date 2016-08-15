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
using System.ComponentModel;
using System.Collections;
using System.Collections.Generic;

namespace NReco.Data
{

	/// <summary>
	/// Automatically generates single-table commands to create-update-delete-retrieve database records.
	/// </summary>
	public interface IDbCommandBuilder 
	{
		IDbCommand GetSelectCommand(Query query);

		IDbCommand GetInsertCommand(string tableName, IEnumerable<KeyValuePair<string,IQueryValue>> data);

		IDbCommand GetDeleteCommand(Query query);

		IDbCommand GetUpdateCommand(Query query, IEnumerable<KeyValuePair<string,IQueryValue>> data);

		IDbFactory DbFactory { get; }
	}
}
