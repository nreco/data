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
			return new PocoModelSchema( ( tableAttr?.Name ) ?? t.Name, cols.ToArray(), keyCols.ToArray(), t );
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

		internal void MapTo(IDataRecord record, object o, Type type, PocoModelSchema schema) {
			for (int i = 0; i < record.FieldCount; i++) {
				var fieldName = record.GetName(i);
				var colMapping = schema.GetColumnMapping(fieldName);
				if (colMapping==null || colMapping.SetVal==null)
					continue;

				var fieldValue = record.GetValue(i);
				
				if (DataHelper.IsNullOrDBNull(fieldValue)) {
					fieldValue = null;
					if (Nullable.GetUnderlyingType(colMapping.ValueType) == null && colMapping.ValueType._IsValueType() )
						fieldValue = colMapping.DefaultValue;
				}
				colMapping.SetValue(o, fieldValue);
			}
		}

		internal T MapTo<T>(IDataRecord record) {
			var type = typeof(T);
			var schema = GetSchema(type);
			if (schema.CreateModel==null)
				throw new ArgumentException($"Type '{type.Name}' does not have a default constructor");	
			var o = schema.CreateModel();
			MapTo(record, o, type, schema);
			return (T)o;
		}

		internal class PocoModelSchema {
			
			internal readonly string TableName;

			internal readonly ColumnMapping[] Key;

			internal readonly ColumnMapping[] Columns;

			internal readonly Type ModelType;

			Dictionary<string,ColumnMapping> ColNameMap;

			internal Func<object> CreateModel;

			internal PocoModelSchema(string tableName, ColumnMapping[] cols, ColumnMapping[] key, Type modelType) {
				TableName = tableName;
				Columns = cols;
				Key = key;
				ColNameMap = new Dictionary<string, ColumnMapping>(Columns.Length);
				for (int i=0; i<Columns.Length; i++) {
					ColNameMap[Columns[i].ColumnName] = Columns[i];
				}
				ModelType = modelType;

				if (modelType.GetConstructor(Type.EmptyTypes)!=null) {
					var createExpr = Expression.Lambda<Func<object>>( Expression.New(modelType) );
					CreateModel = createExpr.Compile();
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

			internal object DefaultValue;

			readonly Type ConvertToType;
			readonly bool IsEnum;

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

				DefaultValue = null;
				if (ValueType._IsValueType())
					DefaultValue = Activator.CreateInstance(ValueType);

				ConvertToType = ValueType;
				if (Nullable.GetUnderlyingType(ValueType) != null)
					ConvertToType = Nullable.GetUnderlyingType(ValueType);
				IsEnum = ConvertToType._IsEnum();

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
				if (val!=null) {
					if (IsEnum) {
						val = Enum.Parse(ConvertToType, val.ToString(), true); 
					} else {
						val = Convert.ChangeType(val, ConvertToType );
					}
				}
				SetVal(obj, val);
				return true;			
			}

		}


	}
}
