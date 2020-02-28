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
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Data;

namespace NReco.Data {
	
	/// <summary>
	/// The <see cref="RecordSetReader"/> obtains the contents of one <see cref="RecordSet"/> as form of read-only, forward-only result set. 
	/// </summary>
	public class RecordSetReader : DbDataReader {

		RecordSet RS;
		bool isOpen = true;
		RecordSet.Row currentRow = null;
		int currentRowIdx = -1;
		bool EOR = false;

		public RecordSetReader(RecordSet rs) {
			RS = rs;
		}

		void EnsureOpen() {
			if (!isOpen)
				throw new InvalidOperationException("Data reader is closed.");
		}

		void EnsureRow() {
			EnsureOpen();
			if (currentRow==null) {
				throw new InvalidOperationException("Invalid data reader.");
			}
		}

		public override object this[string name] {
			get {
				EnsureRow();
				return currentRow[name];
			}
		}

		public override object this[int ordinal] {
			get {
				EnsureRow();
				if (currentRow==null)
					throw new InvalidOperationException("Invalid data reader.");
				return currentRow[ordinal];
			}
		}

		public override int Depth {
			get {
				EnsureOpen();
				return 0;
			}
		}

		public override int FieldCount {
			get {
				EnsureOpen();
				return RS.Columns.Count;
			}
		}

		public override bool HasRows {
			get {
				EnsureOpen();
				return RS.Count>0;
			}
		}

		public override bool IsClosed {
			get {
				return !isOpen;
			}
		}

		public override int RecordsAffected {
			get {
				EnsureOpen();
				return 0;
			}
		}

		#if NET_STANDARD
		protected override void Dispose(bool disposing) {
		#else
		public override void Close() {
		#endif
			if (IsClosed)
				return;
			isOpen = false;
			RS = null;
		}

		public override bool GetBoolean(int ordinal) {
			return (bool)this[ordinal];
		}

		public override byte GetByte(int ordinal) {
			return (byte)this[ordinal];
		}

		long GetArray<T>(int ordinal, long dataOffset, T[] buffer, int bufferOffset, int length) {
			var arr = (T[])this[ordinal];
			var copyCnt = Math.Min(arr.Length - (int)dataOffset, length);
			if (copyCnt>0) {
				Array.Copy(arr, (int)dataOffset, buffer, bufferOffset, copyCnt);
			}
			return copyCnt;			
		}

		public override long GetBytes(int ordinal, long dataOffset, byte[] buffer, int bufferOffset, int length) {
			return GetArray<byte>(ordinal, dataOffset, buffer, bufferOffset, length);
		}

		public override char GetChar(int ordinal) {
			return (char)this[ordinal];
		}

		public override long GetChars(int ordinal, long dataOffset, char[] buffer, int bufferOffset, int length) {
			return GetArray<char>(ordinal, dataOffset, buffer, bufferOffset, length);
		}

		public override DateTime GetDateTime(int ordinal) {
			return (DateTime)this[ordinal];
		}

		public override decimal GetDecimal(int ordinal) {
			return (decimal)this[ordinal];
		}

		public override double GetDouble(int ordinal) {
			return (double)this[ordinal];
		}

		public override IEnumerator GetEnumerator() {
			EnsureOpen();
			return new DbEnumerator(this, false);
		}

		public override string GetDataTypeName(int ordinal) {
			return GetFieldType(ordinal).Name;
		}

		public override Type GetFieldType(int ordinal) {
			EnsureOpen();
			return RS.Columns[ordinal].DataType;
		}

		public override float GetFloat(int ordinal) {
			return (float)this[ordinal];
		}

		public override Guid GetGuid(int ordinal) {
			return (Guid)this[ordinal];
		}

		public override short GetInt16(int ordinal) {
			return (short)this[ordinal];
		}

		public override int GetInt32(int ordinal) {
			return (int)this[ordinal];
		}

		public override long GetInt64(int ordinal) {
			return (long)this[ordinal];
		}

		public override string GetName(int ordinal) {
			return RS.Columns[ordinal].Name;
		}

		public override int GetOrdinal(string name) {
			EnsureOpen();
			return RS.Columns.GetOrdinal(name);
		}

		public override string GetString(int ordinal) {
			return (string)this[ordinal];
		}

		public override object GetValue(int ordinal) {
			return this[ordinal];
		}

		public override int GetValues(object[] values) {
			if (values==null)
				throw new ArgumentNullException("values");
			EnsureRow();
			if (values.Length<RS.Columns.Count)
				throw new ArgumentException("Target values array is too small.");
			currentRow.ItemArray.CopyTo(values, 0);
			return RS.Columns.Count;
		}

		public override bool IsDBNull(int ordinal) {
			var v = this[ordinal];
			return v==null || DBNull.Value.Equals(v);
		}

		public override bool NextResult() {
			// current impl doesn't support multiple result sets
			return false; 
		}

		public override bool Read() {
			EnsureOpen();
			if (EOR)
				return false;
			currentRowIdx++;
			if (currentRowIdx>=RS.Count) {
				EOR = true;
				currentRow = null;
				return false;
			}
			currentRow = RS[currentRowIdx];
			return true;
		}

		DataTable _SchemaTable = null;

		public override DataTable GetSchemaTable() {
			return _SchemaTable ?? (_SchemaTable = BuildSchemaTable());
		}

		internal DataTable BuildSchemaTable() {
			var schemaTable = new DataTable("SchemaTable");

			var ColumnName = new DataColumn(SchemaTableColumn.ColumnName, typeof(string));
			var ColumnOrdinal = new DataColumn(SchemaTableColumn.ColumnOrdinal, typeof(int));
			var ColumnSize = new DataColumn(SchemaTableColumn.ColumnSize, typeof(int));
			var NumericPrecision = new DataColumn(SchemaTableColumn.NumericPrecision, typeof(short));
			var NumericScale = new DataColumn(SchemaTableColumn.NumericScale, typeof(short));

			var DataType = new DataColumn(SchemaTableColumn.DataType, typeof(Type));
			var DataTypeName = new DataColumn("DataTypeName", typeof(string));

			var IsLong = new DataColumn(SchemaTableColumn.IsLong, typeof(bool));
			var AllowDBNull = new DataColumn(SchemaTableColumn.AllowDBNull, typeof(bool));

			var IsUnique = new DataColumn(SchemaTableColumn.IsUnique, typeof(bool));
			var IsKey = new DataColumn(SchemaTableColumn.IsKey, typeof(bool));
			var IsAutoIncrement = new DataColumn(SchemaTableOptionalColumn.IsAutoIncrement, typeof(bool));

			var BaseCatalogName = new DataColumn(SchemaTableOptionalColumn.BaseCatalogName, typeof(string));
			var BaseSchemaName = new DataColumn(SchemaTableColumn.BaseSchemaName, typeof(string));
			var BaseTableName = new DataColumn(SchemaTableColumn.BaseTableName, typeof(string));
			var BaseColumnName = new DataColumn(SchemaTableColumn.BaseColumnName, typeof(string));

			var BaseServerName = new DataColumn(SchemaTableOptionalColumn.BaseServerName, typeof(string));
			var IsAliased = new DataColumn(SchemaTableColumn.IsAliased, typeof(bool));
			var IsExpression = new DataColumn(SchemaTableColumn.IsExpression, typeof(bool));

			var columns = schemaTable.Columns;

			columns.Add(ColumnName);
			columns.Add(ColumnOrdinal);
			columns.Add(ColumnSize);
			columns.Add(NumericPrecision);
			columns.Add(NumericScale);
			columns.Add(IsUnique);
			columns.Add(IsKey);
			columns.Add(BaseServerName);
			columns.Add(BaseCatalogName);
			columns.Add(BaseColumnName);
			columns.Add(BaseSchemaName);
			columns.Add(BaseTableName);
			columns.Add(DataType);
			columns.Add(DataTypeName);
			columns.Add(AllowDBNull);
			columns.Add(IsAliased);
			columns.Add(IsExpression);
			columns.Add(IsAutoIncrement);
			columns.Add(IsLong);

			for (int i = 0; i < RS.Columns.Count; i++) {
				var schemaRow = schemaTable.NewRow();

				schemaRow[ColumnName] = RS.Columns[i].Name;
				schemaRow[ColumnOrdinal] = i;
				schemaRow[ColumnSize] = DBNull.Value;
				schemaRow[NumericPrecision] = DBNull.Value;
				schemaRow[NumericScale] = DBNull.Value;
				schemaRow[BaseServerName] = DBNull.Value;
				schemaRow[BaseCatalogName] = DBNull.Value;
				schemaRow[BaseColumnName] = RS.Columns[i].Name;
				schemaRow[BaseSchemaName] = DBNull.Value;
				schemaRow[BaseTableName] = DBNull.Value;
				schemaRow[DataType] = RS.Columns[i].DataType;
				schemaRow[DataTypeName] = RS.Columns[i].DataType.Name;
				schemaRow[IsAliased] = false;
				schemaRow[IsExpression] = false;
				schemaRow[IsLong] = DBNull.Value;

				schemaRow[IsKey] = RS.PrimaryKey!=null ? Array.IndexOf( RS.PrimaryKey, RS.Columns[i])>=0 : false;
				schemaRow[AllowDBNull] = RS.Columns[i].AllowDBNull;
				schemaRow[IsAutoIncrement] = RS.Columns[i].AutoIncrement;

				schemaTable.Rows.Add(schemaRow);
			}
			return schemaTable;
		}

	}

}
