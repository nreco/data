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

namespace NReco.Data {

	public class QRawSqlNode : QNode {

		/// <summary>
		/// Nodes collection
		/// </summary>
		public override IList<QNode> Nodes { get { return new QNode[0]; } }

		public string SqlText => rawSql.SqlText;

		QRawSql rawSql;

		/// <summary>
		/// Initializes a new instance of the QRawSql with specfield SQL.
		/// </summary>
		/// <param name="sqlText">Raw SQL</param>
		public QRawSqlNode(string sqlText) {
			rawSql = new QRawSql(sqlText);
		}

		/// <summary>
		/// Initializes a new instance of the query node with specfield SQL template and arguments.
		/// </summary>
		/// <param name="sqlTemplate">SQL template that can be resolved with <code>String.Format</code></param>
		/// <param name="args">arguments that should be used to get final SQL text</param>
		public QRawSqlNode(string sqlTemplate, object[] args) {
			rawSql = new QRawSql(sqlTemplate, args);
		}

		public string GetSqlText(Func<object, string> resolveArgValue) => rawSql.GetSqlText(resolveArgValue);

	}

	
}
