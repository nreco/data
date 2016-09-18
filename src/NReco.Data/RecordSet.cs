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
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Data;

namespace NReco.Data {
	
	/// <summary>
	/// Represents a set of in-memory data records with the same schema.
	/// </summary>
	public sealed class RecordSet : ICollection<RecordSet.Row> {
		
		/// <summary>
		/// Gets the columns of this <see cref="RecordSet"/>.
		/// </summary>
		public ColumnCollection Columns { 
			get {
				return columns;
			}
		}
		ColumnCollection columns = null;
		
		
		/// <summary>
		/// Gets or sets an array of columns that function as primary keys for this <see cref="RecordSet"/>.
		/// </summary>
		public Column[] PrimaryKey { get; set; }

		/// <summary>
		/// Gets the total number of <see cref="RecordSet.Rows"/> in this <see cref="RecordSet"/>.
		/// </summary>
		public int Count {
			get { return Rows.Count; }
		}

		List<Row> Rows;

		/// <summary>
		/// Initializes a new instance of <see cref="RecordSet"/> with specified list of <see cref="RecordSet.Column"/>.
		/// </summary>
		/// <param name="columns">columns array</param>
		public RecordSet(Column[] columns) : this(columns, 1) {
		}

		/// <summary>
		/// Initializes a new instance of <see cref="RecordSet"/> with specified list of <see cref="RecordSet.Column"/> and capacity.
		/// </summary>
		/// <param name="columns">columns array</param>
		/// <param name="rowsCapacity">initial rows list capacity</param>
		public RecordSet(Column[] columns, int rowsCapacity) {
			Rows = new List<Row>(rowsCapacity);
			this.columns = new ColumnCollection(columns);
		}

		/// <summary>
		/// Creates a new row.
		/// </summary>
		/// <returns>new row instance</returns>
		public Row Add() {
			return Add( (object[])null);
		}

		/// <summary>
		/// Creates a row using specified values and adds it to the <see cref="RecordSet"/>.
		/// </summary>
		/// <param name="values">The array of values that are used to create the new row.</param>
		/// <returns>new row instance</returns>
		public Row Add(object[] values) {
			if (values==null)
				values = new object[columns.Columns.Length];
			if (values.Length!=columns.Columns.Length)
				throw new ArgumentException("Values array does not match number of columns in the RecordSet.");
			var r = new Row(this,values);
			Rows.Add(r);
			return r;
		}

		/// <summary>
		/// Creates a row using specified column -> value dictionary and adds it to the <see cref="RecordSet"/>.
		/// </summary>
		/// <param name="values">Dictionary with column -> value pairs.</param>
		/// <returns>new row instance</returns>
		public Row Add(IDictionary<string,object> values) {
			var r = new Row(this, new object[columns.Columns.Length]);
			object v;
			for (int i=0; i<columns.Columns.Length; i++) {
				if (values.TryGetValue(columns.Columns[i].Name, out v)) {
					r[i] = v;
				}
			}
			Rows.Add(r);
			return r;
		}

		/// <summary>
		/// Commits all the changes made to this <see cref="RecordSet"/> since the last time <see cref="AcceptChanges"/> was called.
		/// </summary>
		public void AcceptChanges() {
			Rows.RemoveAll( (r) => {
				var del = (r.State&RowState.Deleted)==RowState.Deleted;
				if (del) {
					r.Detach();
				}
				return r.State==RowState.Detached;
			} );
			foreach (var r in Rows)
				r.AcceptChanges();
		}

		public void SetPrimaryKey(params string[] columnNames) {
			var pkCols = new Column[columnNames.Length];
			for (int i=0; i<columnNames.Length; i++) {
				pkCols[i] = Columns[columnNames[i]];
			}
			PrimaryKey = pkCols;
		}

		/// <summary>
		/// Clears the collection of all rows.
		/// </summary>
		public void Clear() {
			foreach (var r in Rows)
				r.Detach();
			Rows.Clear();
		}

		/// <summary>
		/// Determines whether the specified row is present in this <see cref="RecordSet"/>.
		/// </summary>
		/// <param name="row"></param>
		/// <returns></returns>
		public bool Contains(Row row) {
			return Rows.Contains(row);
		}

		bool ICollection<Row>.IsReadOnly {
			get { return false; }
		}

		void ICollection<Row>.Add(Row r) {
			throw new NotSupportedException();
		}

		/// <summary>
		/// Gets the row at the specified index.
		/// </summary>
		/// <param name="rowIndex">The zero-based index of the row to return.</param>
		public Row this[int rowIndex] {
			get { return Rows[rowIndex]; }
		}

		/// <summary>
		/// Removes the row from this <see cref="RecordSet"/>.
		/// </summary>
		/// <param name="r">row to remove</param>
		/// <returns>true if row was successfully removed from the <see cref="RecordSet"/>; otherwise, false.</returns>
		public bool Remove(Row r) {
			if (Rows.Remove(r)) {
				r.Detach();
				return true;
			}
			return false;
		}

		void ICollection<Row>.CopyTo(Row[] array, int arrayIndex) {
			Rows.CopyTo(array, arrayIndex);
		}

		/// <summary>
		/// Returns an enumerator that iterates through the rows in this <see cref="RecordSet"/>.
		/// </summary>
		public IEnumerator<Row> GetEnumerator() {
			return Rows.GetEnumerator();
		}

		/// <summary>
		/// Returns an enumerator that iterates through the rows in this <see cref="RecordSet"/>.
		/// </summary>
		IEnumerator IEnumerable.GetEnumerator() {
			return Rows.GetEnumerator();
		}

		/// <summary>
		/// Fills a <see cref="RecordSet"/> with values from a data source using the supplied <see cref="IDataReader"/>.
		/// </summary>
		/// <param name="rdr">An <see cref="IDataReader"/> that provides a result set.</param>
		/// <returns>Number of loaded rows.</returns>
		public int Load(IDataReader rdr) {
			int processed = 0;
			while (rdr.Read()) {
				Tuple<int,int>[] rdrToRsColIdx = null;
				if (rdrToRsColIdx==null) {
					var rdrToRsColIdxList = new List<Tuple<int, int>>();
					for (int i=0; i<rdr.FieldCount; i++) {
						var colName = rdr.GetName(i);
						if (this.Columns.Contains(colName))
							rdrToRsColIdxList.Add( new Tuple<int, int>(i, this.Columns.GetOrdinal(colName) ) );
					}
					rdrToRsColIdx = rdrToRsColIdxList.ToArray();
				}

				processed++;

				var r = this.Add();
				for (int i=0; i<rdrToRsColIdx.Length; i++) {
					var t = rdrToRsColIdx[i];
					r[ t.Item2 ] = rdr.GetValue(t.Item1);
				}
				r.AcceptChanges();

			}
			return processed;
		}

		/// <summary>
		/// Creates a <see cref="RecordSet"/> from a data source using the supplied <see cref="IDataReader"/>.
		/// </summary>
		/// <param name="rdr">An <see cref="IDataReader"/> that provides a result set.</param>
		/// <returns><see cref="RecordSet"/> with schema inferred by reader and populated with incoming data.</returns>
		public static RecordSet FromReader(IDataReader rdr) {
			RecordSet rs = null;
			while (rdr.Read()) {
				if (rs==null) {
					rs = DataHelper.GetRecordSetByReader(rdr);
				}
				// just copy values
				var rowValues = new object[rdr.FieldCount];
				rdr.GetValues(rowValues);
				rs.Add(rowValues).AcceptChanges();
			}
			return rs;
		}

		/// <summary>
		/// Creates an empty <see cref="RecordSet"/> with schema inferred from the annotated object model.
		/// </summary>
		/// <typeparam name="T">annotated model type</typeparam>
		/// <returns>empty <see cref="RecordSet"/></returns>
		public static RecordSet FromModel<T>() {
			return RecordSet.FromModel<T>(null, RowState.Added);
		}

		/// <summary>
		/// Creates a <see cref="RecordSet"/> with schema and row inferred from the annotated object model.
		/// </summary>
		/// <typeparam name="T">annotated model type</typeparam>
		/// <param name="model">model instance</param>
		/// <param name="rowState">intial state of row created by model</param>
		/// <returns><see cref="RecordSet"/> with one row</returns>
		public static RecordSet FromModel<T>(T model, RowState rowState) {
			return RecordSet.FromModel<T>(new[] {model}, rowState);
		}

		/// <summary>
		/// Creates a <see cref="RecordSet"/> with schema and rows inferred from the annotated object models.
		/// </summary>
		/// <typeparam name="T">annotated model type</typeparam>
		/// <param name="models">sequence of models</param>
		/// <param name="rowState">intial state of rows created by models</param>
		/// <returns><see cref="RecordSet"/> with rows</returns>
		public static RecordSet FromModel<T>(IEnumerable<T> models, RowState rowState) {
			var schema = DataMapper.Instance.GetSchema(typeof(T));
			if (schema.Columns.Length==0)
				throw new ArgumentException($"Model of type {typeof(T).Name} has no columns");
			var rsCols = new Column[schema.Columns.Length];
			var pkCols = new List<Column>(schema.Key.Length);
			for (int i=0; i<rsCols.Length; i++) {
				var modelCol = schema.Columns[i];
				rsCols[i] = new Column(modelCol.ColumnName, modelCol.ValueType) {
					AutoIncrement = modelCol.IsIdentity,
					ReadOnly = modelCol.IsReadOnly
				};
				if (modelCol.IsKey)
					pkCols.Add(rsCols[i]);
			}
			var rs = new RecordSet(rsCols);
			rs.PrimaryKey = pkCols.ToArray();
			if (models!=null) {
				foreach (var dto in models) {
					var rowData = new object[schema.Columns.Length];
					for (int i=0; i<schema.Columns.Length; i++) {
						var modelCol = schema.Columns[i];
						if (modelCol.GetVal!=null)
							rowData[i] = modelCol.GetVal(dto);
					}
					rs.Add(rowData).rowState = rowState;
				}
			}
			return rs;
		}

		/// <summary>
		/// Asynchronously creates a <see cref="RecordSet"/> from a data source using the supplied <see cref="IDataReader"/>.
		/// </summary>
		public static Task<RecordSet> FromReaderAsync(IDataReader rdr) {
			return FromReaderAsync(rdr, CancellationToken.None);
		}

		/// <summary>
		/// Asynchronously creates a <see cref="RecordSet"/> from a data source using the supplied <see cref="IDataReader"/>.
		/// </summary>
		public static async Task<RecordSet> FromReaderAsync(IDataReader rdr, CancellationToken cancel) {
			RecordSet rs = null;
			while (await rdr.ReadAsync(cancel).ConfigureAwait(false)) {
				if (rs==null) {
					rs = DataHelper.GetRecordSetByReader(rdr);
				}
				// just copy values
				var rowValues = new object[rdr.FieldCount];
				await rdr.GetValuesAsync(rowValues, cancel).ConfigureAwait(false);
				rs.Add(rowValues).AcceptChanges();
			}
			return rs;
		}

		/// <summary>
		/// Represents the schema of a column in a <see cref="RecordSet"/>.
		/// </summary>
		public sealed class Column {

			bool _AutoIncrement = false;
			
			/// <summary>
			/// Gets or sets the name of the column.
			/// </summary>
			public string Name { get; private set; }	
			
			/// <summary>
			/// Gets or sets the type of data stored in the column.
			/// </summary>
			public Type DataType { get; set; }

			/// <summary>
			/// Gets or sets a value that indicates whether null values are allowed in this column.
			/// </summary>
			public bool AllowDBNull { get; set; } = true;

			/// <summary>
			/// Gets or sets a value that indicates whether the column automatically increments the value of the column for new rows.
			/// </summary>
			public bool AutoIncrement { 
				get { return _AutoIncrement; } 
				set { _AutoIncrement = value; if (value) ReadOnly = true; } 
			}

			/// <summary>
			/// Gets or sets a value that indicates whether the column allows for changes when committed to data source.
			/// </summary>
			public bool ReadOnly { get; set; } = false;

			public Column(string name) {
				Name = name;
			}

			public Column(string name, Type dataType) {
				Name = name;
				DataType = dataType;
			}

			#if NET_STANDARD
			internal Column(System.Data.Common.DbColumn dbCol) {
				Name = dbCol.ColumnName;
				DataType = dbCol.DataType;
				AllowDBNull = dbCol.AllowDBNull.HasValue ? dbCol.AllowDBNull.Value : true;
				ReadOnly = dbCol.IsReadOnly.HasValue ? dbCol.IsReadOnly.Value : false;
				AutoIncrement = dbCol.AllowDBNull.HasValue ? dbCol.IsAutoIncrement.Value : false;
			}
			#endif
		}

		/// <summary>
		/// Represents a row of data in a <see cref="RecordSet"/>.
		/// </summary>
		public sealed class Row {
			
			/// <summary>
			/// Gets the current state of the row with regard to its relationship to the <see cref="RecordSet"/>.
			/// </summary>
			public RowState State { 
				get {
					return rowState;
				}
			}
			internal RowState rowState;
			
			RecordSet rs;
			object[] values;

			internal Row(RecordSet rs, object[] values) {
				this.rs = rs;
				this.values = values;
				rowState = RowState.Added;
			}

			/// <summary>
			/// Gets or sets all the values for this row through an array.
			/// </summary>
			public object[] ItemArray {
				get { return values; }
			}

			/// <summary>
			/// Deletes the <see cref="RecordSet.Row"/>.
			/// </summary>
			/// <remarks>
			/// If the <see cref="State"/> of the row is Added, the <see cref="State"/> becomes Detached and the row is removed from the table when you call <see cref="RecordSet.AcceptChanges"/>.
			/// The <see cref="State"/> becomes Deleted after you use the <see cref="Delete"/> method on an existing <see cref="Row"/>. 
			/// It remains Deleted until you call <see cref="RecordSet.AcceptChanges"/>.
			/// </remarks>
			public void Delete() {
				if ((rowState&RowState.Added)==RowState.Added) {
					Detach();
				} else {
					rowState = RowState.Deleted;
				}
			}

			/// <summary>
			/// Commits all the changes made to this row since the last time AcceptChanges was called.
			/// </summary>
			/// <remarks>
			/// If the <see cref="State"/> of the row was Added or Modified, the  <see cref="State"/> becomes Unchanged. 
			/// If the <see cref="State"/> was Deleted, the row is removed (<see cref="State"/> becomes Detached).
			/// </remarks>
			public void AcceptChanges() {
				if ((rowState&RowState.Deleted)==RowState.Deleted) {
					Detach();
				} else if (rowState!=RowState.Detached) { 
					rowState = RowState.Unchanged;
				}
			}

			internal void Detach() {
				rs = null;
				rowState = RowState.Detached;
			}

			void CheckIfDetached() {
				if (rowState==RowState.Detached)
					throw new InvalidOperationException();
			}

			object GetValue(int columnIndex) {
				return values[columnIndex];
			}
			void SetValue(int columnIndex, object val) {
				values[columnIndex] = val;
				rowState |= RowState.Modified;
			}

			/// <summary>
			/// Gets or sets the data stored in the column specified by index.
			/// </summary>
			/// <param name="column">column index</param>
			/// <returns>An object that contains column value for this row.</returns>
			public object this[int columnIndex] {
				get {
					CheckIfDetached();
					return GetValue(columnIndex);
				}
				set {
					CheckIfDetached();
					SetValue(columnIndex, value);
				}
			}

			/// <summary>
			/// Gets or sets the data stored in the specified <see cref="RecordSet.Column"/>.
			/// </summary>
			/// <param name="column">data column</param>
			/// <returns>An object that contains column value for this row.</returns>
			public object this[Column column] {
				get {
					CheckIfDetached();
					return GetValue(rs.columns.GetOrdinal(column));
				}
				set {
					CheckIfDetached();
					SetValue(rs.columns.GetOrdinal(column), value);
				}
			}

			/// <summary>
			/// Gets or sets the data stored in the column specified by name.
			/// </summary>
			/// <param name="columnName">The name of the column.</param>
			/// <returns>An object that contains column value for this row.</returns>
			public object this[string columnName] {
				get {
					CheckIfDetached();
					return GetValue(rs.columns.GetOrdinal(columnName));
				}
				set {
					CheckIfDetached();
					SetValue(rs.columns.GetOrdinal(columnName), value);
				}
			}

			/// <summary>
			/// Provides strongly-typed access to each of the column values in the specified row. The Field<T> method also supports nullable types. 
			/// </summary>
			/// <typeparam name="T">A generic parameter that specifies the return type of the column.</typeparam>
			/// <param name="columnName">The name of the column to return the value of.</param>
			/// <returns>The value, of type T. If value is null or DBNull, default(T) is returned.</returns>
			public T Field<T>(string columnName) {
				var v = this[columnName];
				var typeCode = Type.GetTypeCode(typeof(T));
				if (v!=null && !DBNull.Value.Equals(v)) {
					return (T)Convert.ChangeType( v, typeCode, System.Globalization.CultureInfo.InvariantCulture );
				}
				return default(T);
			}

		}

		/// <summary>
		/// Gets the state of a <see cref="RecordSet.Row"/> object.
		/// </summary>
		[Flags]
		public enum RowState {
			Detached = 1,
			Unchanged = 2,
			Added = 4,
			Deleted = 8,
			Modified = 16
		}

		/// <summary>
		/// Represents a collection of <see cref="RecordSet.Column"/> objects for a <see cref="RecordSet"/>.
		/// </summary>
		public sealed class ColumnCollection : 
			#if NET_STANDARD
			IReadOnlyList<Column>
			#else 
			ICollection<Column>
			#endif
		{
			
			internal Column[] Columns;
			Dictionary<Column,int> ColToIdx;
			Dictionary<string,int> ColNameToIdx;

			internal ColumnCollection(Column[] columns) {
				Columns = columns;
				ColToIdx = new Dictionary<Column, int>(columns.Length);
				ColNameToIdx = new Dictionary<string, int>(columns.Length);
				for (int i=0; i<columns.Length; i++) {
					ColToIdx[columns[i]] = i;
					ColNameToIdx[columns[i].Name] = i;
				}
			}

			internal int GetOrdinal(Column c) {
				int idx;
				if (ColToIdx.TryGetValue(c, out idx))
					return idx;
				throw new ArgumentException("Column with name '"+c.Name+"' does not exist.");
			}

			public int GetOrdinal(string columnName) {
				int idx;
				if (ColNameToIdx.TryGetValue(columnName, out idx))
					return idx;
				throw new ArgumentException("Column with name '"+columnName+"' does not exist.");
			}

			/// <summary>
			/// Gets the total number of elements in a collection.
			/// </summary>
			public int Count {
				get {
					return Columns.Length;
				}
			}

			/// <summary>
			/// Gets the <see cref="RecordSet.Column"/> from the collection at the specified index.
			/// </summary>
			public Column this[int ordinal] {
				get {
					return Columns[ordinal];
				}
			}

			/// <summary>
			/// Gets the <see cref="RecordSet.Column"/> from the collection with the specified name.
			/// </summary>
			public Column this[string columnName] {
				get {
					return Columns[GetOrdinal(columnName)];
				}
			}

			/// <summary>
			/// Checks whether the collection contains a column with the specified name.
			/// </summary>
			/// <param name="columnName">the name of the column.</param>
			/// <returns>true if a column exists with this name; otherwise, false.</returns>
			public bool Contains(string columnName) {
				return ColNameToIdx.ContainsKey(columnName);
			}

			public IEnumerator<Column> GetEnumerator() {
				return ((IEnumerable<Column>)Columns).GetEnumerator();
			}

			IEnumerator IEnumerable.GetEnumerator() {
				return Columns.GetEnumerator();
			}

			#if !NET_STANDARD
			bool ICollection<Column>.IsReadOnly {
				get {
					return true;
				}
			}

			void ICollection<Column>.Add(Column item) {
				throw new NotSupportedException("ColumnCollection is readonly");
			}

			void ICollection<Column>.Clear() {
				throw new NotSupportedException("ColumnCollection is readonly");
			}

			bool ICollection<Column>.Contains(Column item) {
				throw new NotImplementedException();
			}

			void ICollection<Column>.CopyTo(Column[] array, int arrayIndex) {
				throw new NotImplementedException();
			}

			bool ICollection<Column>.Remove(Column item) {
				throw new NotSupportedException("ColumnCollection is readonly");
			}
			#endif


		}

	}
}
