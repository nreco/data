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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;

namespace NReco.Data {

	/// <summary>
	/// Batch command builder that can produce several SQL statements into one <see cref="IDbCommand"/>.
	/// </summary>
	public class DbBatchCommandBuilder : DbCommandBuilder {
		
		/// <summary>
		/// Gets current <see cref="IDbCommand"/> with batch of SQL statements.
		/// </summary>
		public IDbCommand CurrentBatchCommand { get; private set; } = null;

		/// <summary>
		/// Gets or sets separator between SQL statements (';' by default).
		/// </summary>
		public string SqlStatementSeparator { get; set; } = ";";

		public DbBatchCommandBuilder(IDbFactory dbFactory) : base(dbFactory) {

		}

		public void BeginBatch() {
			CurrentBatchCommand = base.GetCommand();
		}

		public IDbCommand EndBatch() {
			if (CurrentBatchCommand==null)
				throw new InvalidOperationException("BatchEnd should follow after BeginBatch");
			var cmd = CurrentBatchCommand;
			CurrentBatchCommand = null;
			return cmd;
		}

		protected override IDbCommand GetCommand() {
			if (CurrentBatchCommand!=null)
				return CurrentBatchCommand;
			return base.GetCommand();
		}

		protected override void SetCommandText(IDbCommand cmd, string sqlStatement) {
			if (CurrentBatchCommand!=null && CurrentBatchCommand.CommandText!=null && CurrentBatchCommand.CommandText.Length>0) {
				CurrentBatchCommand.CommandText += SqlStatementSeparator + sqlStatement;
			} else {
				base.SetCommandText(cmd, sqlStatement);
			}
		}

	}
}
