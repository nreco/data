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
	/// Represents SQL builder interface.
	/// </summary>
	public interface ISqlExpressionBuilder
	{
		/// <summary>
		/// Builds SQL-compatible string by specified <see cref="QTable"/>. 
		/// </summary>
		string BuildTableName(QTable tbl);

		/// <summary>
		/// Builds SQL-compatible string by specified <see cref="IQueryValue"/>. 
		/// </summary>
		string BuildValue(IQueryValue v);

		/// <summary>
		/// Builds SQL-compatible string by specified <see cref="QNode"/>.
		/// </summary>
		string BuildExpression(QNode node);
	}
}
