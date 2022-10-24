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
using System.ComponentModel;

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
		public IDbFactory DbFactory {  get; private set; }

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
		/// <item>@&lt;extendedPropertyKey&gt; (value from Query.ExtendedProperties dictionary)</item>
		/// </list>
		/// @record* variables are useful for database-specific paging optimizations, for example:
		/// <code>
		/// // MS SQL TOP syntax
		/// DbCommandBuilder cmdBuilder;
		/// cmdBuilder = "SELECT @recordtop[TOP {0}] @columns FROM @table @where[ WHERE {0}] @groupby[ GROUP BY {0}] @orderby[ ORDER BY {0}]";
		/// </code>
		/// <code>
		/// // PostgreSql LIMIT and OFFSET syntax
		/// DbCommandBuilder cmdBuilder;
		/// cmdBuilder = "SELECT @columns FROM @table @where[ WHERE {0}] @groupby[ GROUP BY {0}] @orderby[ ORDER BY {0}] @recordcount[LIMIT {0}] @recordoffset[OFFSET {0}]";
		/// </code>
		/// Note that if offset is applied on DB level and <see cref="DbDataAdapter.ApplyOffset"/> should be false. 
		/// </remarks>
		public string SelectTemplate { get; set; } = "SELECT @columns FROM @table@where[ WHERE {0}]@groupby[ GROUP BY {0}]@orderby[ ORDER BY {0}]";

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
		/// Gets or sets view name -> <see cref="DbDataView"/> dictionary.
		/// </summary>
		public IDictionary<string,DbDataView> Views { get; set; }

		Func<string, StringTemplate> _createStringTemplate;

		/// <summary>
		/// Initializes a new instance of the DbCommandBuilder.
		/// </summary>
		/// <param name="dbFactory">DB provider-specific factory implementation</param>
		public DbCommandBuilder(IDbFactory dbFactory) {
			DbFactory = dbFactory;
			Views = new Dictionary<string,DbDataView>();
		}

		/// <summary>
		/// Initializes a new instance of the DbCommandBuilder with the specified StringTemplate factory method.
		/// </summary>
		/// <param name="dbFactory">DB provider-specific factory implementation</param>
		/// <param name="createStringTemplate">StringTemplate factory method</param>
		public DbCommandBuilder(IDbFactory dbFactory, Func<string, StringTemplate> createStringTemplate) {
			DbFactory = dbFactory;
			Views = new Dictionary<string, DbDataView>();
			_createStringTemplate = createStringTemplate;
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

		DbDataView defaultSelectView = null;

		internal string BuildSelectInternal(Query query, ISqlExpressionBuilder sqlBuilder, bool isSubquery) {
			DbDataView view = null;
			if (Views==null || !Views.TryGetValue(query.Table.Name, out view)) {
				// default template
				if (defaultSelectView!=null && defaultSelectView.SelectTemplate == SelectTemplate)
					view = defaultSelectView;
				else
					view = defaultSelectView = new DbDataView(SelectTemplate, CreateStringTemplate);
			}
			return view.FormatSelectSql(query,sqlBuilder,isSubquery);
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
			var tblName = dbSqlBuilder.BuildTableName( new QTable(query.Table.Name, null) );

			var deleteTpl = CreateStringTemplate(DeleteTemplate);
			SetCommandText(cmd, deleteTpl.FormatTemplate( (varName) => {
				switch (varName) {
					case "table": return new StringTemplate.TokenResult(tblName);
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
			var tblName = dbSqlBuilder.BuildTableName( new QTable(query.Table.Name, null) );

			var updateTpl = CreateStringTemplate(UpdateTemplate);
			SetCommandText(cmd, updateTpl.FormatTemplate( (varName) => {
				switch (varName) {
					case "table": return new StringTemplate.TokenResult(tblName);
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
				columns.Append( dbSqlBuilder.BuildValue( (QField)setField.Key) );

				if (values.Length>0)
					values.Append(',');
				values.Append(dbSqlBuilder.BuildValue(setField.Value));
			}

			var tblName = dbSqlBuilder.BuildTableName( new QTable(tableName, null) );
			var colStr = columns.ToString();
			var valStr = values.ToString();

			var insertTpl = CreateStringTemplate(InsertTemplate);
			SetCommandText(cmd, insertTpl.FormatTemplate( (varName) => {
				switch (varName) {
					case "table": return new StringTemplate.TokenResult(tblName);
					case "columns": return new StringTemplate.TokenResult(colStr);
					case "values": return new StringTemplate.TokenResult(valStr);
				}
				return StringTemplate.TokenResult.NotDefined;
			}) );
			
			return cmd;
		}

		StringTemplate CreateStringTemplate(string tpl) {
			if (_createStringTemplate != null)
				return _createStringTemplate(tpl);
			return new StringTemplate(tpl);
		}

	}
}
