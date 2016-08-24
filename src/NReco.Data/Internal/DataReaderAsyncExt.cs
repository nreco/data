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
	
	internal static class DataReaderAsyncExt {
		
		internal static Task<bool> ReadAsync(this IDataReader rdr, CancellationToken cancel) {
			if (rdr is DbDataReader) {
				return ((DbDataReader)rdr).ReadAsync(cancel);
			} else {
				return Task.FromResult(rdr.Read());
			}
		}

		internal static Task<int> GetValuesAsync(this IDataReader rdr, object[] values, CancellationToken cancel) {
			if (rdr is DbDataReader) {
				return ((DbDataReader)rdr).GetValuesAsync(values, cancel);
			} else {
				return Task.FromResult(rdr.GetValues(values));
			}
		}

	}



}
