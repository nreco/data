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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;

namespace NReco.Data {
		
	/// <summary>
	/// Represents context for custom <see cref="DataReaderResult"/> data mapping to POCO models.
	/// </summary>
	public interface IDataReaderMapperContext {

		/// <summary>
		/// Data reader with current record's data.
		/// </summary>
		IDataReader DataReader { get; }

		/// <summary>
		/// Target POCO model type.
		/// </summary>
		Type ObjectType { get; }
			
		/// <summary>
		/// Performs default data mapping to specified object (data annotations are used if present).
		/// </summary>
		void MapTo(object o);

		/// <summary>
		/// Creates model of specified type and performs default mapping to this object.
		/// </summary>
		object MapTo(Type t);  
	}

	internal sealed class DataReaderMapperContext : IDataReaderMapperContext {
			
		public IDataReader DataReader { get; private set; }
			
		public Type ObjectType { get; private set; }

		DataMapper Mapper;

		internal DataReaderMapperContext(DataMapper mapper, IDataReader rdr, Type toType) {
			Mapper = mapper;
			DataReader = rdr;
			ObjectType =toType;
		}

		public void MapTo(object o) {
			var t = o.GetType();
			var schema = Mapper.GetSchema(t);
			Mapper.MapTo(DataReader, o, t, schema);
		}

		public object MapTo(Type t) {
			return Mapper.MapTo(DataReader, t);
		}

	}

}
