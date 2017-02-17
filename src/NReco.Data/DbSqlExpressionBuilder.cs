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
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace NReco.Data
{
	/// <summary>
	/// Generic implementation of DB-specific SQL expression builder.
	/// </summary>
	public class DbSqlExpressionBuilder : SqlExpressionBuilder
	{

		protected IDbCommand Command { get; private set; }
		protected IDbFactory DbFactory { get; private set; }

		public Func<Query,string> BuildSubquery { get; set; }

		public Func<string,string> FormatIdentifier { get; set; }

		public DbSqlExpressionBuilder(IDbCommand cmd, IDbFactory dbFactory) {
			Command = cmd;
			DbFactory = dbFactory;
		}
		
		protected override string BuildConditionLValue(QConditionNode node) {
			var lValue = base.BuildConditionLValue(node);
			return (node.LValue is Query) ?	"("+lValue+")" : lValue;
		}

		protected override string BuildConditionRValue(QConditionNode node) {
			var rValue = base.BuildConditionRValue(node);
			return (node.RValue is Query && ((node.Condition & Conditions.In) != Conditions.In)) ?
				"(" + rValue + ")" : rValue;
		}

		public override string BuildValue(IQueryValue v) {
			if (v is Query) {
				// refactoring is needed for subqueries handling. TBD: find better solution without 'buildSubquery' delegate.
				if (BuildSubquery==null)
					throw new NotImplementedException("Subqueries are not supported in this context");
				return BuildSubquery( (Query)v );
			}
			return base.BuildValue(v);
		}

		protected override string BuildValue(QConst value) {
			object constValue = value.Value;
			// all constants are passed as parameters						
			var cmdParam = DbFactory.AddCommandParameter(Command,constValue);
			if (value is QVar) {
				cmdParam.Parameter.SourceColumn = ((QVar)value).Name;
			}
			return cmdParam.Placeholder;
		}
		
		protected override string BuildValue(string str) {
			return DbFactory.AddCommandParameter(Command,str).Placeholder;
		}
		
		protected override string BuildIdentifier(string name) {
			return FormatIdentifier!=null ? FormatIdentifier(name) : name;
		}



	}
}
