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
	/// Represents abstract SQL builder interface.
	/// </summary>
	public interface ISqlExpressionBuilder
	{
		/// <summary>
		/// Build string representation of specified IQueryValue
		/// </summary>
		string BuildValue(IQueryValue v);

		/// <summary>
		/// Build string representation of specified QueryNode (condition)
		/// </summary>
		string BuildExpression(QNode node);
	}
}
