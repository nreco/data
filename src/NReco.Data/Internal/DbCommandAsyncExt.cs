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
using System.Threading;
using System.Threading.Tasks;
using System.Data;
using System.Data.Common;

namespace NReco.Data {
	
	internal static class DbCommandAsyncExt {
		
		internal static Task<int> ExecuteNonQueryAsync(this IDbCommand cmd, CancellationToken cancel) {
			if (cmd is DbCommand) {
				return ((DbCommand)cmd).ExecuteNonQueryAsync(cancel);
			} else {
				return Task.FromResult<int>( cmd.ExecuteNonQuery() );
			}
		}

		internal static Task<object> ExecuteScalarAsync(this IDbCommand cmd, CancellationToken cancel) {
			if (cmd is DbCommand) {
				return ((DbCommand)cmd).ExecuteScalarAsync(cancel);
			} else {
				return Task.FromResult<object>( cmd.ExecuteScalar() );
			}
		}

	}



}
