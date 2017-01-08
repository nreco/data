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
	
	internal interface IDataReaderResult<T> {
		T Result { get; }
		void Init(IDataReader rdr);
		void Read(IDataReader rdr);
	}

	internal class SingleDataReaderResult<T> : IDataReaderResult<T> {
		public T Result { get; private set; }

		Func<IDataReader,T> Convert;

		internal SingleDataReaderResult(Func<IDataReader,T> convert) {
			Convert = convert;
			Result = default(T);
		}

		public void Init(IDataReader rdr) { }

		public void Read(IDataReader rdr) {
			Result = Convert(rdr);
		}
	}

	internal class ListDataReaderResult<T> : IDataReaderResult<List<T>> {
		public List<T> Result { get; private set; }

		Func<IDataReader,T> Convert;

		internal ListDataReaderResult(Func<IDataReader,T> convert) {
			Convert = convert;
			Result = new List<T>();
		}

		public void Init(IDataReader rdr) { }

		public void Read(IDataReader rdr) {
			Result.Add( Convert(rdr) );
		}
	}

	internal class RecordSetDataReaderResult : IDataReaderResult<RecordSet> {
		public RecordSet Result { get; private set; }
		
		internal RecordSetDataReaderResult() {
			Result = null;
		}

		public void Init(IDataReader rdr) {
			if (Result==null) {
				Result = DataHelper.GetRecordSetByReader(rdr);
			}			
		}

		public void Read(IDataReader rdr) {
			var rowValues = new object[rdr.FieldCount];
			rdr.GetValues(rowValues);
			Result.Add(rowValues).AcceptChanges();
		}				
	}

}
