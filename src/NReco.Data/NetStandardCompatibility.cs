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
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NReco.Data {
	
#if NET_STANDARD
	internal static class NetStandardCompatibility {
		internal static PropertyInfo[] GetProperties(this Type t) {
			return t.GetTypeInfo().GetProperties();
		} 
		internal static FieldInfo[] GetFields(this Type t) {
			return t.GetTypeInfo().GetFields();
		} 
		internal static PropertyInfo GetProperty(this Type t, string propName) {
			return t.GetTypeInfo().GetProperty(propName);
		}
		internal static bool IsAssignableFrom(this Type t, Type fromType) {
			return t.GetTypeInfo().IsAssignableFrom(fromType.GetTypeInfo());
		}
		internal static bool _IsValueType(this Type t) {
			return t.GetTypeInfo().IsValueType;
		}
		internal static bool _IsEnum(this Type t) {
			return t.GetTypeInfo().IsEnum;
		}
	}
#else
	internal static class Net40Compatibility {
		internal static bool _IsValueType(this Type t) {
			return t.IsValueType;
		}
		internal static bool _IsEnum(this Type t) {
			return t.IsEnum;
		}
	}
#endif

}
