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
	
	//[Serializable]
	public class QRawSqlNode : QNode {

		/// <summary>
		/// Nodes collection
		/// </summary>
		public override IList<QNode> Nodes { get { return new QNode[0]; } }
		
		public string SqlText {
			get; private set;
		}
	
		public QRawSqlNode(string sqlText) {
			SqlText = sqlText;
		}

	}


	
}
