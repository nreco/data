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

		public string SelectTemplate { get; private set; }

		public IDictionary<string,string> FieldMapping { get; set; }

		public DbDataView(string selectTemplate) {
			SelectTemplate = selectTemplate;
		}

		public virtual string FormatSelectSql(Query query, ISqlExpressionBuilder sqlBuilder, bool isSubquery) {
			var isCount = IsCountQuery(query);
			string columns = BuildSelectColumns(query, sqlBuilder);
			string orderBy = BuildOrderBy(query, sqlBuilder);
			string whereExpression = BuildWhere(query, sqlBuilder);
			
			var selectTpl = new StringTemplate(SelectTemplate);
			return selectTpl.FormatTemplate( (varName) => {
				switch (varName) {
					case "table": return new StringTemplate.TokenResult( GetSelectTableName(query.Table) );
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
			foreach (var qField in query.Fields) {
				var f = qField;
				if (FieldMapping!=null && FieldMapping.ContainsKey(f.Name))
					f = new QField(f.Name, FieldMapping[f.Name]);	

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

		bool IsCountQuery(Query q) {
			if (q.Fields==null || q.Fields.Length!=1 || q.Fields[0].Expression==null)
				return false;
			var exprLower = q.Fields[0].Expression.Trim().ToLower();
			return exprLower.StartsWith("count(") && exprLower.EndsWith(")");
		}


	}
}
