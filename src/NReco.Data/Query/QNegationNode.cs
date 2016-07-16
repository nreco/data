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

#if !NET_STANDARD
using System.Runtime.Serialization;
#endif

namespace NReco.Data {
	
	/// <summary>
	/// Represents logical negation operator
	/// </summary>
	//[Serializable]
	public class QNegationNode : QNode {
		
		private QNode[] SingleNodeList;
	
		public override IList<QNode> Nodes {
			get { return SingleNodeList; } 
		}
		
		/// <summary>
		/// Initializes a new instance of the QueryNegationNode that wraps specified node  
		/// </summary>
		/// <param name="node">condition node to negate</param>
		public QNegationNode(QNode node) {
			SingleNodeList = new QNode[] { node };
		}

		public QNegationNode(QNegationNode copyNode) {
			SingleNodeList = new QNode[] { copyNode.Nodes[0] };
			Name = copyNode.Name;
		}

		
	}


	
}
