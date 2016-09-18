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
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using System.Data;
using System.Data.Common;
using System.Reflection;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;

namespace NReco.Data {
	
	internal class DataMapper {
		
		internal readonly static DataMapper Instance = new DataMapper();

		IDictionary<Type,PocoModelSchema> SchemaCache;

		internal DataMapper() {
			SchemaCache = new ConcurrentDictionary<Type, PocoModelSchema>();
		}

		PocoModelSchema InferSchema(Type t) {
			var keyCols = new List<ColumnMapping>();
			var cols = new List<ColumnMapping>();
			foreach (var prop in t.GetProperties()) {
				var metadata = CheckSchemaAttributes(prop.GetCustomAttributes());
				if (metadata.Item3) // not mapped
					continue; 
				var colMapping = new ColumnMapping( 
					metadata.Item1 ?? prop.Name, t, prop.Name, prop.PropertyType, 
					prop.CanRead, prop.CanWrite,
					metadata.Item4, metadata.Item5, metadata.Item2);
				if (metadata.Item2) // is key
					keyCols.Add(colMapping);
				cols.Add(colMapping);
			}
			foreach (var fld in t.GetFields()) {
				var metadata = CheckSchemaAttributes(fld.GetCustomAttributes());
				if (metadata.Item3) // not mapped
					continue; 
				var colMapping = new ColumnMapping( 
					metadata.Item1 ?? fld.Name, t, fld.Name, fld.FieldType, 
					true, true,
					metadata.Item4, metadata.Item5, metadata.Item2);
				if (metadata.Item2) // is key
					keyCols.Add(colMapping);
				cols.Add(colMapping);
			}
			var typeAttrs = t.GetCustomAttributes(true);
			var tableAttr = typeAttrs.Where(a=>a is TableAttribute).Select(a=>(TableAttribute)a).FirstOrDefault();
			return new PocoModelSchema( ( tableAttr?.Name ) ?? t.Name, cols.ToArray(), keyCols.ToArray() );
		}

		internal PocoModelSchema GetSchema(Type t) {
			PocoModelSchema schema = null;
			if (!SchemaCache.TryGetValue(t, out schema)) {
				schema = InferSchema(t);
				SchemaCache[t] = schema;
			}
			return schema;
		}

		Tuple<string,bool,bool,bool,bool> CheckSchemaAttributes(IEnumerable<Attribute> attrs) {
			bool isNotMapped = false;
			bool isKey = false;
			bool isDbGenerated = false;
			bool isIdentity = false;
			string colName = null;
			foreach (var attr in attrs) {
				if (attr is NotMappedAttribute) {
					isNotMapped = true;
					break;
				} else if (attr is KeyAttribute) {
					isKey = true;
				} else if (attr is ColumnAttribute) {
					var colAttr = (ColumnAttribute)attr;
					colName = colAttr.Name;
				} else if (attr is DatabaseGeneratedAttribute) {
					var dbGenAttr = ((DatabaseGeneratedAttribute)attr);
					isDbGenerated = true;
					if (dbGenAttr.DatabaseGeneratedOption==DatabaseGeneratedOption.Identity)
						isIdentity = true;
				}
			}
			return new Tuple<string,bool,bool,bool,bool>(colName, isKey, isNotMapped, isDbGenerated, isIdentity);
		}

		internal void MapTo(IDataRecord record, object o) {
			if (o==null)
				return;
			var type = o.GetType();
			var schema = GetSchema(type);

			for (int i = 0; i < record.FieldCount; i++) {
				var fieldName = record.GetName(i);
				var colMapping = schema.GetColumnMapping(fieldName);
				if (colMapping==null || colMapping.SetVal==null)
					continue;

				var fieldValue = record.GetValue(i);
				
				if (DataHelper.IsNullOrDBNull(fieldValue)) {
					fieldValue = null;
					if (Nullable.GetUnderlyingType(colMapping.ValueType) == null && colMapping.ValueType._IsValueType() )
						fieldValue = Activator.CreateInstance(colMapping.ValueType); // slow: TBD faster way to get default(T)
				}
				colMapping.SetValue(o, fieldValue);
			}
		}

		internal class PocoModelSchema {
			
			internal readonly string TableName;

			internal readonly ColumnMapping[] Key;

			internal readonly ColumnMapping[] Columns;

			Dictionary<string,ColumnMapping> ColNameMap;

			internal PocoModelSchema(string tableName, ColumnMapping[] cols, ColumnMapping[] key) {
				TableName = tableName;
				Columns = cols;
				Key = key;
				ColNameMap = new Dictionary<string, ColumnMapping>(Columns.Length);
				for (int i=0; i<Columns.Length; i++) {
					ColNameMap[Columns[i].ColumnName] = Columns[i];
				}
			}

			internal ColumnMapping GetColumnMapping(string colName) {
				ColumnMapping colMapping = null;
				ColNameMap.TryGetValue(colName, out colMapping);
				return colMapping;
			}
		}

		internal class ColumnMapping {
			internal readonly string ColumnName;
			internal readonly Type ValueType;

			internal readonly Func<object,object> GetVal;
			internal readonly Action<object,object> SetVal;

			internal readonly bool IsReadOnly;

			internal readonly bool IsIdentity;

			internal readonly bool IsKey;

			internal ColumnMapping(
					string colName, Type t, 
					string propOrFieldName, Type propOrFieldType, 
					bool canRead, bool canWrite,
					bool isReadOnly, bool isIdentity, bool isKey) {
				ColumnName = colName;
				ValueType = propOrFieldType;
				IsReadOnly = isReadOnly;
				IsIdentity = isIdentity;
				IsKey = isKey;

				// compose get
				if (canRead) {
					var getParamObj = Expression.Parameter(typeof(object));
					var getterExpr = Expression.Lambda<Func<object,object>>(
							Expression.Convert(
								Expression.PropertyOrField( Expression.Convert(getParamObj,t), propOrFieldName ),
								typeof(object)
							),
							getParamObj
						);
					GetVal = getterExpr.Compile();
				}

				// compose set
				if (canWrite) {
					var setParamObj = Expression.Parameter(typeof(object));
					var setParamVal = Expression.Parameter(typeof(object));
					var setterExpr = Expression.Lambda<Action<object,object>>(
						Expression.Assign( 
							Expression.PropertyOrField( Expression.Convert(setParamObj,t) , propOrFieldName ), 
							Expression.Convert(setParamVal, propOrFieldType)
						), setParamObj, setParamVal
					);
					SetVal = setterExpr.Compile();
				}
			}

			internal bool SetValue(object obj, object val) {
				if (SetVal==null)
					return false;
				var valType = ValueType;
				if (Nullable.GetUnderlyingType(valType) != null)
					valType = Nullable.GetUnderlyingType(valType);
				if (valType._IsEnum()) {
					val = Enum.Parse(valType, val.ToString(), true); 
				}

				SetVal(obj, Convert.ChangeType(val, valType ) );
				return true;			
			}

		}


	}
}
