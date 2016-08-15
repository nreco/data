using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NReco.Data {
	
	/// <summary>
	/// Represents a set of in-memory data records with the same schema.
	/// </summary>
	public sealed class RecordSet : ICollection<RecordSet.Row> {
		
		/// <summary>
		/// Gets the columns of this <see cref="RecordSet"/>.
		/// </summary>
		public Column[] Columns { 
			get {
				return columns;
			}
		}
		Column[] columns;
		
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
		Dictionary<Column,int> ColToIdx;
		Dictionary<string,int> ColNameToIdx;

		public RecordSet(Column[] columns) : this(columns, 1) {
		}

		public RecordSet(Column[] columns, int rowsCapacity) {
			Rows = new List<Row>(rowsCapacity+1);
			this.columns = columns;
			ColToIdx = new Dictionary<Column, int>(columns.Length);
			ColNameToIdx = new Dictionary<string, int>(columns.Length);
			for (int i=0; i<columns.Length; i++) {
				ColToIdx[columns[i]] = i;
				ColNameToIdx[columns[i].Name] = i;
			}
		}

		internal int GetColumnIndex(Column c) {
			int idx;
			if (ColToIdx.TryGetValue(c, out idx))
				return idx;
			throw new InvalidOperationException();
		}

		internal int GetColumnIndex(string colName) {
			int idx;
			if (ColNameToIdx.TryGetValue(colName, out idx))
				return idx;
			throw new InvalidOperationException();
		}

		public Row Add(object[] rowValues) {
			if (rowValues.Length!=columns.Length)
				throw new InvalidOperationException();
			var r = new Row(this,rowValues);
			Rows.Add(r);
			return r;
		}

		public Row NewRow() {
			var r = new Row(this, new object[columns.Length]);
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
		/// Represents the schema of a column in a <see cref="RecordSet"/>.
		/// </summary>
		public sealed class Column {
			
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
			public bool AllowDBNull { get; set; }

			/// <summary>
			/// Gets or sets a value that indicates whether the column automatically increments the value of the column for new rows.
			/// </summary>
			public bool AutoIncrement { get; set; }

			public Column(string name) {
				Name = name;
			}

			public Column(string name, Type dataType) {
				Name = name;
				DataType = dataType;
			}
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
			RowState rowState;
			
			RecordSet rs;
			object[] values;

			internal Row(RecordSet rs, object[] values) {
				this.rs = rs;
				this.values = values;
				rowState = RowState.Added;
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
				if ((rowState&RowState.Deleted)==RowState.Deleted)
					Detach();
				else
					rowState = RowState.Unchanged;
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
					return GetValue(rs.GetColumnIndex(column));
				}
				set {
					CheckIfDetached();
					SetValue(rs.GetColumnIndex(column), value);
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
					return GetValue(rs.GetColumnIndex(columnName));
				}
				set {
					CheckIfDetached();
					SetValue(rs.GetColumnIndex(columnName), value);
				}
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

	}
}
