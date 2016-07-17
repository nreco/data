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
using System.Linq;

#if !NET_STANDARD
using System.Runtime.Serialization;
#endif

namespace NReco.Data
{

	/// <summary>
	/// Represents abstract data query structure.
	/// </summary>
	//[Serializable]
	public class Query : QNode, IQueryValue
	{
		private QSort[] _Sort = null;
		private QField[] _Fields = null;
		private int _RecordOffset = 0;
		private int _RecordCount = Int32.MaxValue;
		private QTable _Table = null;
		
		/// <summary>
		/// Query conditions tree. Can be null.
		/// </summary>
		public QNode Condition { get; set; }

		/// <summary>
		/// List of child nodes
		/// </summary>
		public override IList<QNode> Nodes {
			get { return new QNode[] { Condition }; }
		}

		/// <summary>
		/// List of sort fields. Can be null.
		/// </summary>
		public QSort[] Sort {
			get { return _Sort; } 
			set { _Sort = value; }
		}
		
		/// <summary>
		/// List of fields to load. Null means all available fields.
		/// </summary>
		public QField[] Fields {
			get { return _Fields; }
			set { _Fields = value; }
		}
		
		/// <summary>
		/// Get or set starting record to load
		/// </summary>
		public int RecordOffset {
			get { return _RecordOffset; }
			set { _RecordOffset = value; }
		}
		
		/// <summary>
		/// Get or set max records count to load
		/// </summary>
		public int RecordCount {
			get { return _RecordCount; }
			set { _RecordCount = value; }
		}
		
		/// <summary>
		/// Get or set target source name of this query
		/// </summary>
		public QTable Table { 
			get { return _Table; }
			set { _Table = value; }
		}
		
		/// <summary>
		/// Get or set query extended properties. 
		/// </summary>
		/// <remarks>Extended properties may be used for passing custom query parameters.</remarks>
		public IDictionary<string,object> ExtendedProperties { get; set; }


		/// <summary>
		/// Initializes a new instance of the Query with specified table
		/// </summary>
		/// <param name="table">target table</param>
		public Query(QTable table) {
			_Table = table;
		}

		/// <summary>
		/// Initializes a new instance of the Query with specified table and condition
		/// </summary>
		/// <param name="table">target table</param>
		/// <param name="condition">conditions root node</param>
		public Query(QTable table, QNode condition) {
			_Table = table;
			Condition = condition;
		}

		/// <summary>
		/// Initializes a new instance of the Query with identical options of specified query
		/// </summary>
		/// <param name="q">query with options to copy</param>
		public Query(Query q) {
			_Table = q.Table;
			_Sort = q.Sort;
			_RecordOffset = q.RecordOffset;
			_RecordCount = q.RecordCount;
			Condition = q.Condition;
			_Fields = q.Fields;
			if (q.ExtendedProperties!=null)
				ExtendedProperties = new Dictionary<string,object>( q.ExtendedProperties );
		}
		
		/// <summary>
		/// Set query sort by specified list of QSort
		/// </summary>
		/// <param name="sortFields"></param>
		public Query OrderBy(params QSort[] sortFields) {
			if (sortFields != null && sortFields.Length > 0) {
				_Sort = sortFields;
			} else {
				_Sort = null;
			}
			return this;
		}

		/// <summary>
		/// Set query fields by specified list of field names
		/// </summary>
		/// <param name="fields">list of field names</param>
		public Query Select(params QField[] fields) {
			if (fields != null && fields.Length > 0) {
				_Fields = fields;
			} else {
				_Fields = null;
			}
			return this;
		}

		/// <summary>
		/// Finds all QVar constants ("name":var in relex) and passes them to specified set handler.
		/// </summary>
		/// <param name="setVar">handler for <see cref="QVar"/> constants.</param>
		/// <example>
		/// The following code unsets all query variables:
		/// <code>
		/// </code>
		/// </example>
		public void SetVars(Action<QVar> setVar) {
			SetVarsInternal(Condition, setVar);
		}

		private void SetVarsInternal(QNode node, Action<QVar> setVar) {
			if (node is QConditionNode) {
				var cndNode = (QConditionNode)node;
				if (cndNode.LValue is QVar)
					setVar( (QVar) cndNode.LValue);
				if (cndNode.RValue is QVar)
					setVar( (QVar) cndNode.RValue);
			}
			if (node != null)
				foreach (var cNode in node.Nodes)
					SetVarsInternal(cNode, setVar);
		}

		/// <summary>
		/// Returns a string that represents current query in relex format
		/// </summary>
		/// <returns>relex string</returns>
		public override string ToString() {
			return (new NReco.Data.Relex.RelexBuilder()).BuildRelex(this);
		}
	
	}
}
