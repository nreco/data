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
using System.Data;
using System.Data.Common;

namespace NReco.Data {

	/// <summary>
	/// Generic <see cref="IDbFactory"/> implementation that may be used with most ADO.NET Data Providers.
	/// </summary>
	public class DbFactory : IDbFactory {

		protected DbProviderFactory DbPrvFactory;

		public string ParamPlaceholderFormat { get; set; }

		public int CommandTimeout { get; set; }

		public string ParamNameFormat { get; set; } = "@p{0}";

		public string LastInsertIdSelectText { get; set; } //= "SELECT @@IDENTITY";

		public DbFactory(DbProviderFactory dbProviderFactory) {
			DbPrvFactory = dbProviderFactory;
			CommandTimeout = -1;
		}

		public virtual IDbCommand CreateCommand() {
			var cmd = DbPrvFactory.CreateCommand();
			if (CommandTimeout >= 0)
				cmd.CommandTimeout = CommandTimeout;
			return cmd;
		}

		public IDbConnection CreateConnection() {
			return DbPrvFactory.CreateConnection();
		}

		public string AddCommandParameter(IDbCommand cmd, object value) {
			var param = DbPrvFactory.CreateParameter();
			param.ParameterName = String.Format(ParamNameFormat, cmd.Parameters.Count);
			param.Value = value ?? DBNull.Value;
			cmd.Parameters.Add(param);
			return GetCmdParameterPlaceholder(param.ParameterName);
		}

		public virtual ISqlExpressionBuilder CreateSqlBuilder(IDbCommand dbCommand, Func<Query,string> buildSubquery) {
			return new DbSqlExpressionBuilder(dbCommand, this, buildSubquery);
		}

		public virtual object GetInsertId(IDbConnection connection) {
			if (String.IsNullOrEmpty(LastInsertIdSelectText)) {
				throw new NotSupportedException("LastInsertIdSelectText is not defined");
			}
			if (connection.State != ConnectionState.Open)
				throw new InvalidOperationException("GetInsertId requires opened connection");
			using (var cmd = CreateCommand()) {
				cmd.CommandText = LastInsertIdSelectText;
				cmd.Connection = connection;
				return cmd.ExecuteScalar();
			}
		}

		protected virtual string GetCmdParameterPlaceholder(string paramName) {
			if (ParamPlaceholderFormat == null)
				return paramName;
			return String.Format(ParamPlaceholderFormat, paramName);
		}

	}
}
