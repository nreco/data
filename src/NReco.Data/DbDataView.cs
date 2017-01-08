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
using System.ComponentModel;

namespace NReco.Data {
	
	/// <summary>
	/// Represents application-level read-only data view (complex query that can be queries a table).
	/// </summary>
	public class DbDataView {
		
		/// <summary>
		/// Gets select template string for <see cref="StringTemplate"/>.
		/// </summary>
		public string SelectTemplate { get; private set; }

		/// <summary>
		/// Query fields mapping to SQL expressions (optional).
		/// </summary>
		/// <remarks>
		/// Field mappings are useful for defining SQL-calculated columns, or resolving ambigious columns names:
		/// <code>
		/// var dbView = new DbDataView(
		///		@"SELECT @columns FROM persons p
		///		  LEFT JOIN countries c ON (c.id=p.country_id)
		///		@where[ WHERE {0}] @orderby[ ORDER BY {0}]") {
		///		FieldMapping = new Dictionary&lt;string,string&gt;() {
		///			// just id is ambigious
		///			{"id", "p.id"},  
		///			// SQL expression for calculated "expired" field
		///			{"expired", "CASE WHEN DATEDIFF(dd, p.added_date, NOW() )>30 THEN 1 ELSE 0 END" } 
		///     }
		/// } );
		/// </code>
		/// </remarks>
		public IDictionary<string,string> FieldMapping { get; set; }

		public DbDataView(string selectTemplate) {
			SelectTemplate = selectTemplate;
		}

		/// <summary>
		/// Generates SELECT SQL statement by given <see cref="Query"/> and <see cref="ISqlExpressionBuilder"/>.
		/// </summary>
		/// <param name="query">formal query structure</param>
		/// <param name="sqlBuilder">SQL builder component</param>
		/// <param name="isSubquery">subquery flag (true if this is sub-query select)</param>
		/// <returns>SQL select command text</returns>
		public virtual string FormatSelectSql(Query query, ISqlExpressionBuilder sqlBuilder, bool isSubquery) {
			var isCount = IsCountQuery(query);
			string columns = BuildSelectColumns(query, sqlBuilder);
			string orderBy = BuildOrderBy(query, sqlBuilder);
			string whereExpression = BuildWhere(query, sqlBuilder);
			var tblName = sqlBuilder.BuildTableName(query.Table);

			var selectTpl = new StringTemplate(SelectTemplate);
			return selectTpl.FormatTemplate( (varName) => {
				switch (varName) {
					case "table": return new StringTemplate.TokenResult( tblName );
					case "where": return new StringTemplate.TokenResult( whereExpression );
					case "orderby": return isCount ? StringTemplate.TokenResult.NotApplicable : new StringTemplate.TokenResult( orderBy );
					case "columns": return new StringTemplate.TokenResult( columns );
					case "recordoffset": return new StringTemplate.TokenResult( query.RecordOffset );
					case "recordcount": return query.RecordCount<Int32.MaxValue ? new StringTemplate.TokenResult( query.RecordCount ) : StringTemplate.TokenResult.NotDefined;
					case "recordtop": return query.RecordCount<Int32.MaxValue ? new StringTemplate.TokenResult( query.RecordOffset+query.RecordCount ) : StringTemplate.TokenResult.NotDefined;
				}
				if (query.ExtendedProperties!=null && query.ExtendedProperties.ContainsKey(varName))
					return new StringTemplate.TokenResult(query.ExtendedProperties[varName]);
				return StringTemplate.TokenResult.NotDefined;
			});			
		} 

		string BuildWhere(Query query, ISqlExpressionBuilder sqlBuilder) {
			var condition = FieldMapping==null ? query.Condition : DataHelper.MapQValue(query.Condition, ApplyFieldMapping);
			return sqlBuilder.BuildExpression(condition);
		}

		IQueryValue ApplyFieldMapping(IQueryValue qValue) {
			if (qValue is QField) {
				var qFld = (QField)qValue;
				if (FieldMapping.ContainsKey(qFld.Name)) {
					return new QField(qFld.Name, FieldMapping[qFld.Name]);
				}
			}
			return qValue;
		}

		string ApplyFieldMapping(string field) {
			if (FieldMapping!=null && FieldMapping.ContainsKey(field))
				return FieldMapping[field];
			return field;
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
				return ApplyFieldMapping("*");

			var columns = new StringBuilder();
			foreach (var qField in query.Fields) {
				var f = qField;
				if (FieldMapping!=null && FieldMapping.ContainsKey(f.Name))
					f = new QField(f.Name, FieldMapping[f.Name]);	

				var fld = sqlBuilder.BuildValue((IQueryValue)f);
				if (fld!=f.Name && f.Expression!=null) { // use "as" only for expression-fields
					// special handling for 'count(*)' mapping
					if (f.Expression.ToLower()=="count(*)")
						fld = ApplyFieldMapping("count(*)");
					fld = String.IsNullOrEmpty(f.Name) ? fld : String.Format("{0} as {1}", fld, sqlBuilder.BuildValue((QField)f.Name) );
				}
				if (columns.Length>0)
					columns.Append(',');
				columns.Append(fld);
			}
			return columns.ToString();
		}

		bool IsCountQuery(Query q) {
			if (q.Fields==null || q.Fields.Length!=1 || q.Fields[0].Expression==null)
				return false;
			var exprLower = q.Fields[0].Expression.Trim().ToLower();
			return exprLower.StartsWith("count(") && exprLower.EndsWith(")");
		}


	}
}
