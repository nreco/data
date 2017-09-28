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
using System.Threading.Tasks;

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

		#if !NET_STANDARD1
		public override System.Data.DataTable GetSchemaTable() {
			throw new NotImplementedException("Currently RecordSetReader does not implement GetSchemaTable. If you need it please add an issue here: https://github.com/nreco/data/issues");
		}
		#endif

	}

}
