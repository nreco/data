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
	/// Represents abstract query node that contains child nodes.
	/// </summary>
	//[Serializable]
	public abstract class QNode
	{
		public abstract IList<QNode> Nodes { get; }

		public string Name { get; set; }	
	
		internal QNode() {
		}

		/// <summary>
		/// OR operator
		/// </summary>
		public static QGroupNode operator | (QNode node1, QNode node2) {
			QGroupNode res = new QGroupNode(QGroupType.Or);
			res.Nodes.Add(node1);
			res.Nodes.Add(node2);
			return res;
		}

		/// <summary>
		/// AND operator
		/// </summary>
		public static QGroupNode operator & (QNode node1, QNode node2) {
			QGroupNode res = new QGroupNode(QGroupType.And);
			res.Nodes.Add(node1);
			res.Nodes.Add(node2);
			return res;
		}	


	}
}
