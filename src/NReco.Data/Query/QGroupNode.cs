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

namespace NReco.Data
{
	
	/// <summary>
	/// Represents group of nodes combined with logical OR/AND operator
	/// </summary>
	public class QGroupNode : QNode {
		
		private List<QNode> _Nodes;
	
		/// <summary>
		/// List of group child nodes
		/// </summary>
		public override IList<QNode> Nodes {
			get {
				return _Nodes;
			}
		}
		
		/// <summary>
		/// Logical operator type (<see cref="NReco.Data.QGroupType"/>)
		/// </summary>
		public QGroupType GroupType { get; set; }
		
		/// <summary>
		/// Initializes a new instance of the QueryGroupNode with specified logical operator
		/// </summary>
		/// <param name="type">group logical operator (<see cref="NReco.Data.QGroupType"/>)</param>
		public QGroupNode(QGroupType type) {
			GroupType = type;
			_Nodes = new List<QNode>();
		}
		
		/// <summary>
		/// Initializes a new instance of the QueryGroupNode that copies specified QueryGroupNode
		/// </summary>
		/// <param name="likeGroup">QueryGroupNode to copy from</param>
		public QGroupNode(QGroupNode likeGroup)
			: this(likeGroup.GroupType) { 
			Name = likeGroup.Name;
			_Nodes.AddRange(likeGroup.Nodes);
		}
		

		/// <summary>
		/// OR operator
		/// </summary>
		public static QGroupNode operator | (QGroupNode node1, QNode node2) {
			
			if ( node1.GroupType==QGroupType.Or) {
				node1.Nodes.Add( node2 );
				return node1;
			}
			QGroupNode res = new QGroupNode(QGroupType.Or);
			res.Nodes.Add(node1);
			res.Nodes.Add(node2);
			return res;
		}

		/// <summary>
		/// AND operator
		/// </summary>
		public static QGroupNode operator & (QGroupNode node1, QNode node2) {
			if ( node1.GroupType==QGroupType.And) {
				node1.Nodes.Add( node2 );
				return node1;
			}
			QGroupNode res = new QGroupNode(QGroupType.And);
			res.Nodes.Add(node1);
			res.Nodes.Add(node2);
			return res;
		}

		/// <summary>
		/// Compose AND group node with specified child nodes
		/// </summary>
		/// <param name="nodes">child nodes</param>
		/// <returns>QueryGroupNode of AND type</returns>
		public static QGroupNode And(params QNode[] nodes) {
			var andGrp = new QGroupNode(QGroupType.And);
			andGrp._Nodes.AddRange(nodes);
			return andGrp;
		}

		/// <summary>
		/// Compose OR group node with specified child nodes
		/// </summary>
		/// <param name="nodes">child nodes</param>
		/// <returns>QueryGroupNode of OR type</returns>
		public static QGroupNode Or(params QNode[] nodes) {
			var orGrp = new QGroupNode(QGroupType.Or);
			orGrp._Nodes.AddRange(nodes);
			return orGrp;
		}

	}

	/// <summary>
	/// Describes the group node types
	/// </summary>
	public enum QGroupType {
		/// <summary>
		/// Logical OR group type
		/// </summary>
		Or, 

		/// <summary>
		/// Logical AND group type
		/// </summary>
		And
	}	


	
}
