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
	/// Represents raw SQL query value
	/// </summary>
	//[Serializable]
	public class QRawSql : IQueryValue {

		/// <summary>
		/// Get SQL text
		/// </summary>
		public string SqlText {
			get; private set;
		}
		
		/// <summary>
		/// Initializes a new instance of the QRawSql with specfield SQL text
		/// </summary>
		/// <param name="sqlText"></param>
		public QRawSql(string sqlText) {
			SqlText = sqlText;
		}
		
	}
}
