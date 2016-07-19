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
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Text;
#if !NET_STANDARD
using System.ComponentModel;
#endif

namespace NReco.Data
{
	/// <summary>
	/// Automatically generates SQL commands for SELECT/INSERT/UPDATE/DELETE queries.
	/// </summary>
	public class DbCommandBuilder : IDbCommandBuilder
	{

		/// <summary>
		/// Gets DB Factory component.
		/// </summary>
		protected IDbFactory DbFactory {  get; private set; }

		/// <summary>
		/// Gets or sets template for SQL SELECT query.
		/// </summary>
		/// <remarks>
		/// Template is processed with <see cref="StringTemplate"/>.
		/// List of available variables:
		/// <list>
		/// <item>@columns (comma-separated list of fields from Query or '*')</item>
		/// <item>@table (table name, possibly with alias like 'users u')</item>
		/// <item>@where (query conditions, may be empty)</item>
		/// <item>@orderby (order by expression, may be empty)</item>
		/// <item>@recordoffset (starting record index offset, 0 by default)</item>
		/// <item>@recordcount (max number of records to return, empty if not specified)</item>
		/// <item>@recordtop (recordoffset+recordcount, empty if recordcount is not specified)</item>			
		/// </list>
		/// @record* variables are useful for database-specific paging optimizations, for example:
		/// <code>
		/// // MS SQL TOP syntax
		/// DbCommandBuilder cmdBuilder;
		/// cmdBuilder = "SELECT @recordtop[TOP {0}] @columns FROM @table@where[ WHERE {0}]@orderby[ ORDER BY {0}]";
		/// </code>
		/// <code>
		/// // PostgreSql LIMIT and OFFSET syntax
		/// DbCommandBuilder cmdBuilder;
		/// cmdBuilder = "SELECT @columns FROM @table@where[ WHERE {0}]@orderby[ ORDER BY {0}] @recordcount[LIMIT {0}] @recordoffset[OFFSET {0}]";
		/// </code>
		/// Note that if offset is applied on DB level and <see cref="DbDataAdapter.ApplyOffset"/> should be false. 
		/// </remarks>
		public string SelectTemplate { get; set; } = "SELECT @columns FROM @table@where[ WHERE {0}]@orderby[ ORDER BY {0}]";

		/// <summary>
		/// Gets or sets template for SQL UPDATE query.
		/// </summary>
		/// <remarks>
		/// Template is processed with <see cref="StringTemplate"/>.
		/// List of available variables:
		/// <list>
		/// <item>@table (table name)</item>
		/// <item>@set (comma-separated set statements)</item>		
		/// <item>@where (query conditions, may be empty)</item>		
		/// </list>
		/// </remarks>
		public string UpdateTemplate { get; set; } = "UPDATE @table SET @set @where[WHERE {0}]";

		/// <summary>
		/// Gets or sets template for SQL INSERT query.
		/// </summary>
		/// <remarks>
		/// Template is processed with <see cref="StringTemplate"/>.
		/// List of available variables:
		/// <list>
		/// <item>@table (table name)</item>
		/// <item>@columns (comma-separated list of columns)</item>	
		/// <item>@values (comma-separated list of values)</item>		
		/// </list>
		/// </remarks>		
		public string InsertTemplate { get; set; } = "INSERT INTO @table (@columns) VALUES (@values)";
		
		/// <summary>
		/// Gets or sets template for SQL DELETE query.
		/// </summary>
		/// <remarks>
		/// Template is processed with <see cref="StringTemplate"/>.
		/// List of available variables:
		/// <list>
		/// <item>@table (table name)</item>
		/// <item>@where (query conditions, may be empty)</item>	
		/// </list>
		/// </remarks>			
		public string DeleteTemplate { get; set; } = "DELETE FROM @table @where[WHERE {0}]";

		/// <summary>
		/// Initializes a new instance of the DbCommandBuilder.
		/// </summary>
		/// <param name="dbFactory">DB provider-specific factory implementation</param>
		public DbCommandBuilder(IDbFactory dbFactory) {
			DbFactory = dbFactory;
		}

		protected ISqlExpressionBuilder GetSqlBuilder(IDbCommand cmd) {
			ISqlExpressionBuilder sqlBuilder = null;
			sqlBuilder = DbFactory.CreateSqlBuilder(cmd, (q) => { return BuildSelectInternal(q,sqlBuilder,true); } );
			return sqlBuilder;
		}

		protected virtual IDbCommand GetCommand() {
			return DbFactory.CreateCommand();
		}

		protected virtual void SetCommandText(IDbCommand cmd, string sqlStatement) {
			cmd.CommandText = sqlStatement;
		}

		/// <summary>
		/// Gets the automatically generated <see cref="IDbCommand"/> object to select rows by specified <see cref="Query"/>.
		/// </summary>
		/// <param name="query">query that determines table and select options</param>
		/// <returns></returns>
		public virtual IDbCommand GetSelectCommand(Query query) {
			var cmd = GetCommand();
			var cmdSqlBuilder = GetSqlBuilder(cmd);
			SetCommandText(cmd,BuildSelectInternal(query, cmdSqlBuilder, false));
			return cmd;
		}

		string BuildOrderBy(Query query, ISqlExpressionBuilder sqlBuilder) {
			var orderBy = new StringBuilder();
			if (query.Sort!=null && query.Sort.Length>0) {
				foreach (var sortFld in query.Sort) {
					if (orderBy.Length>0)
						orderBy.Append(',');
					orderBy.Append( sqlBuilder.BuildValue( (IQueryValue) sortFld.Field) );
					orderBy.Append(' ');
					orderBy.Append(sortFld.SortDirection == ListSortDirection.Ascending ? QSort.Asc : QSort.Desc);
				}
			} else {
				return null;
			}
			return orderBy.ToString();
		}

		string BuildSelectColumns(Query query, ISqlExpressionBuilder sqlBuilder) {
			// Compose fields part
			if (query.Fields == null || query.Fields.Length == 0)
				return "*";

			var columns = new StringBuilder();
			foreach (var f in query.Fields) {
				var fld = sqlBuilder.BuildValue((IQueryValue)f);
				if (fld != f.Name) { //skip "as" for usual fields
					fld = String.IsNullOrEmpty(f.Name) ? f.Expression : String.Format("({0}) as {1}", fld, f.Name);
				}
				if (columns.Length>0)
					columns.Append(',');
				columns.Append(fld);
			}
			return columns.ToString();
		}

		string GetSelectTableName(QTable table) {
			if (!String.IsNullOrEmpty(table.Alias))
				return table.Name + " " + table.Alias;
			return table.Name;
		}

		internal string BuildSelectInternal(Query query, ISqlExpressionBuilder sqlBuilder, bool isNested) {
			string columns = BuildSelectColumns(query, sqlBuilder);
			string orderBy = BuildOrderBy(query, sqlBuilder);
			string whereExpression = sqlBuilder.BuildExpression(query.Condition);
			
			var selectTpl = new StringTemplate(SelectTemplate);
			var sqlText = selectTpl.FormatTemplate( (varName) => {
				switch (varName) {
					case "table": return new StringTemplate.TokenResult( GetSelectTableName(query.Table) );
					case "where": return new StringTemplate.TokenResult( whereExpression );
					case "orderby": return new StringTemplate.TokenResult( orderBy );
					case "columns": return new StringTemplate.TokenResult( columns );
					case "recordoffset": return new StringTemplate.TokenResult( query.RecordOffset );
					case "recordcount": return query.RecordCount<Int32.MaxValue ? new StringTemplate.TokenResult( query.RecordCount ) : StringTemplate.TokenResult.NotDefined;
					case "recordtop": return query.RecordCount<Int32.MaxValue ? new StringTemplate.TokenResult( query.RecordOffset+query.RecordCount ) : StringTemplate.TokenResult.NotDefined;
				}
				return StringTemplate.TokenResult.NotDefined;
			});

			return sqlText;
		}

		/// <summary>
		/// Gets the automatically generated <see cref="IDbCommand"/> object to delete rows by specified <see cref="Query"/>.
		/// </summary>
		/// <param name="query">query that determines delete table and conditions</param>
		/// <returns>delete SQL command</returns>	
		public virtual IDbCommand GetDeleteCommand(Query query) {
			var cmd = GetCommand();
			var dbSqlBuilder = GetSqlBuilder(cmd);

			// prepare WHERE part
			var whereExpression = dbSqlBuilder.BuildExpression( query.Condition );
			
			var deleteTpl = new StringTemplate(DeleteTemplate);
			SetCommandText(cmd, deleteTpl.FormatTemplate( (varName) => {
				switch (varName) {
					case "table": return new StringTemplate.TokenResult(query.Table);
					case "where": return new StringTemplate.TokenResult(whereExpression);
				}
				return StringTemplate.TokenResult.NotDefined;
			}) );

			return cmd;
		}

		/// <summary>
		/// Gets the automatically generated <see cref="IDbCommand"/> object to update rows by specified <see cref="Query"/>.
		/// </summary>
		/// <param name="query">query that determines update table and conditions</param>
		/// <param name="data">changeset data</param>
		/// <returns>update SQL command</returns>	
		public virtual IDbCommand GetUpdateCommand(Query query, IEnumerable<KeyValuePair<string,IQueryValue>> data) {
			var cmd = GetCommand();
			var dbSqlBuilder = GetSqlBuilder(cmd);

			// prepare fields Part
			var setExpression = new StringBuilder();

			foreach (var setField in data) {
				if (setExpression.Length>0)
					setExpression.Append(',');

				setExpression.Append( dbSqlBuilder.BuildValue(new QField(setField.Key)) );
				setExpression.Append('=');
				setExpression.Append( dbSqlBuilder.BuildValue(setField.Value) );
			}

			// prepare WHERE part
			string whereExpression = dbSqlBuilder.BuildExpression( query.Condition );
			
			var updateTpl = new StringTemplate(UpdateTemplate);
			SetCommandText(cmd, updateTpl.FormatTemplate( (varName) => {
				switch (varName) {
					case "table": return new StringTemplate.TokenResult(query.Table);
					case "set": return new StringTemplate.TokenResult(setExpression.ToString());
					case "where": return new StringTemplate.TokenResult(whereExpression);
				}
				return StringTemplate.TokenResult.NotDefined;
			}) );
			
			return cmd;
		}

		/// <summary>
		/// Gets the automatically generated <see cref="IDbCommand"/> object to insert new record.
		/// </summary>
		/// <param name="tableName">table name</param>
		/// <param name="data">new record data</param>
		/// <returns>insert SQL command</returns>	
		public virtual IDbCommand GetInsertCommand(string tableName, IEnumerable<KeyValuePair<string,IQueryValue>> data) {
			var cmd = GetCommand();
			var dbSqlBuilder = GetSqlBuilder(cmd);
			
			// Prepare fields part
			var columns = new StringBuilder();
			var values = new StringBuilder();
			foreach (var setField in data) {
				if (columns.Length>0)
					columns.Append(',');
				columns.Append(setField.Key);

				if (values.Length>0)
					values.Append(',');
				values.Append(dbSqlBuilder.BuildValue(setField.Value));
			}

			var insertTpl = new StringTemplate(InsertTemplate);
			SetCommandText(cmd, insertTpl.FormatTemplate( (varName) => {
				switch (varName) {
					case "table": return new StringTemplate.TokenResult(tableName);
					case "columns": return new StringTemplate.TokenResult(columns.ToString());
					case "values": return new StringTemplate.TokenResult(values);
				}
				return StringTemplate.TokenResult.NotDefined;
			}) );
			
			return cmd;
		}

		

	}
}
