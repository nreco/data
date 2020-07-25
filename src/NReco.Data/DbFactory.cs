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
using System.Threading;
using System.Threading.Tasks;
using System.Data;
using System.Data.Common;

namespace NReco.Data {

	/// <summary>
	/// Generic <see cref="IDbFactory"/> implementation that may be used with most ADO.NET Data Providers.
	/// </summary>
	/// <example>
	/// var dbFactory = new DbFactory(System.Data.SqlClient.SqlClientFactory.Instance);
	/// </example>
	public class DbFactory : IDbFactory {

		protected DbProviderFactory DbPrvFactory;

		public string ParamPlaceholderFormat { get; set; }

		public int CommandTimeout { get; set; }

		public string ParamNameFormat { get; set; } = "@p{0}";

		public string IdentifierFormat { get; set; }

		/// <summary>
		/// Gets or sets SQL command for retrieving ID for last inserted row.
		/// </summary>
		/// <remarks>
		/// LastInsertIdSelectText for popular databases:
		/// <list>
		/// <item>MsSql (System.Data.SqlClient): "SELECT @@IDENTITY"</item>
		/// <item>Sqlite (Microsoft.Data.Sqlite): "SELECT last_insert_rowid()"</item>
		/// <item>PostgreSql (Npgsql): "SELECT lastval()"</item>
		/// <item>MySql (MySql.Data): "SELECT LAST_INSERT_ID()"</item>
		/// </list>
		/// </remarks>
		public string LastInsertIdSelectText { get; set; }

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

		public virtual CommandParameter AddCommandParameter(IDbCommand cmd, object value) {
			var param = DbPrvFactory.CreateParameter();
			param.ParameterName = GetCmdParameterName(cmd.Parameters.Count);
			param.Value = value ?? DBNull.Value;
			cmd.Parameters.Add(param);
			return new CommandParameter( GetCmdParameterPlaceholder(param.ParameterName), param );
		}

		public virtual ISqlExpressionBuilder CreateSqlBuilder(IDbCommand dbCommand, Func<Query,string> buildSubquery) {
			var sqlBuilder = new DbSqlExpressionBuilder(dbCommand, this) {
				BuildSubquery = buildSubquery,
				FormatIdentifier = ApplyIdentifierFormat
			};
			return sqlBuilder;
		}

		protected virtual string ApplyIdentifierFormat(string name) {
			return IdentifierFormat!=null ? String.Format(IdentifierFormat, name) : name;
		}

		public virtual object GetInsertId(IDbConnection connection, IDbTransaction transaction) {
			if (String.IsNullOrEmpty(LastInsertIdSelectText)) {
				return null;
			}
			if (connection.State != ConnectionState.Open)
				throw new InvalidOperationException("GetInsertId requires opened connection");
			using (var cmd = CreateCommand()) {
				cmd.CommandText = LastInsertIdSelectText;
				cmd.Connection = connection;
				cmd.Transaction = transaction;
				return cmd.ExecuteScalar();
			}
		}

		public async Task<object> GetInsertIdAsync(IDbConnection connection, IDbTransaction transaction, CancellationToken cancel) {
			if (String.IsNullOrEmpty(LastInsertIdSelectText)) {
				return null;
			}
			if (connection.State != ConnectionState.Open)
				throw new InvalidOperationException("GetInsertId requires opened connection");
			using (var cmd = CreateCommand()) {
				cmd.CommandText = LastInsertIdSelectText;
				cmd.Connection = connection;
				cmd.Transaction = transaction;
				return await cmd.ExecuteScalarAsync(cancel);
			}
		}

		protected virtual string GetCmdParameterName(int paramIndex) {
			return String.Format(ParamNameFormat, paramIndex);
		}

		protected virtual string GetCmdParameterPlaceholder(string paramName) {
			if (ParamPlaceholderFormat == null)
				return paramName;
			return String.Format(ParamPlaceholderFormat, paramName);
		}

	}
}
