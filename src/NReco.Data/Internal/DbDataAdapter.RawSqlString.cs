#region License
/*
 * NReco Data library (http://www.nrecosite.com/)
 * Copyright 2016-2017 Vitaliy Fedorchenko
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
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;


namespace NReco.Data {
	
	public partial class DbDataAdapter {

		/// <summary>
		/// A string representing a raw SQL query. 
		/// This type enables overload resolution between the regular and interpolated <see cref="Select(FormattableString)"/> overloads.
		/// </summary>
		public struct RawSqlString {
			internal string Format;
			public RawSqlString(string s) {
				Format = s;
			}
			public static implicit operator RawSqlString(string value) {
				return new RawSqlString(value);
			}

#if NET_STANDARD
			public static implicit operator RawSqlString(FormattableString value) {
				// implicit cast from FormattableString is never called because compiler chooses Select(FormattableString) overload
				throw new InvalidOperationException();
			}
#endif

		}

	}
}
