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
using System.Text;
using System.Data.Common;
using System.Data;
using System.Collections;
using System.Threading.Tasks;
using System.Threading;

namespace NReco.Data {
	internal class OffsetCountDataReaderWrapper : DbDataReader {

		IDataReader Rdr;
		int Offset;
		int Count;

		internal OffsetCountDataReaderWrapper(IDataReader rdr, int offset, int count) {
			Rdr = rdr;
			Offset = offset;
			Count = count;
		}

		public override object this[int ordinal] => Rdr[ordinal];

		public override object this[string name] => Rdr[name];

		public override int FieldCount => Rdr.FieldCount;
		public override int Depth => Rdr.Depth;

		public override bool HasRows {
			get {
				if (Rdr is DbDataReader dbRdr)
					return dbRdr.HasRows;
				throw new NotImplementedException();
			}
		}

		public override bool IsClosed => Rdr.IsClosed;

		public override int RecordsAffected => Rdr.RecordsAffected;

		public override bool GetBoolean(int ordinal) {
			return Rdr.GetBoolean(ordinal);
		}

		public override byte GetByte(int ordinal) {
			return Rdr.GetByte(ordinal);
		}

		public override long GetBytes(int ordinal, long dataOffset, byte[] buffer, int bufferOffset, int length) {
			return Rdr.GetBytes(ordinal, dataOffset, buffer, bufferOffset, length);
		}

		public override char GetChar(int ordinal) {
			return Rdr.GetChar(ordinal);
		}

		public override long GetChars(int ordinal, long dataOffset, char[] buffer, int bufferOffset, int length) {
			return Rdr.GetChars(ordinal, dataOffset, buffer, bufferOffset, length);
		}

		public override string GetDataTypeName(int ordinal) {
			return Rdr.GetDataTypeName(ordinal);
		}

		public override DateTime GetDateTime(int ordinal) {
			return Rdr.GetDateTime(ordinal);
		}

		public override decimal GetDecimal(int ordinal) {
			return Rdr.GetDecimal(ordinal);
		}

		public override double GetDouble(int ordinal) {
			return Rdr.GetDouble(ordinal);
		}

		public override IEnumerator GetEnumerator() {
			return new DbEnumerator(this, false);
		}

		public override Type GetFieldType(int ordinal) => Rdr.GetFieldType(ordinal);

		public override float GetFloat(int ordinal) => Rdr.GetFloat(ordinal);

		public override Guid GetGuid(int ordinal) => Rdr.GetGuid(ordinal);

		public override short GetInt16(int ordinal) => Rdr.GetInt16(ordinal);

		public override int GetInt32(int ordinal) => Rdr.GetInt32(ordinal);

		public override long GetInt64(int ordinal) => Rdr.GetInt64(ordinal);

		public override string GetName(int ordinal) => Rdr.GetName(ordinal);

		public override int GetOrdinal(string name) => Rdr.GetOrdinal(name);

		public override string GetString(int ordinal) => Rdr.GetString(ordinal);

		public override object GetValue(int ordinal) => Rdr.GetValue(ordinal);

		public override int GetValues(object[] values) => Rdr.GetValues(values);

		public override bool IsDBNull(int ordinal) => Rdr.IsDBNull(ordinal);

		public override bool NextResult() => Rdr.NextResult();

		public override DataTable GetSchemaTable() => Rdr.GetSchemaTable();

		public override void Close() => Rdr.Close();

		public override bool Read() {
			while (Offset > 0) {
				if (!Rdr.Read()) {
					Offset = 0;
					return false;
				}
				Offset--;
			}

			var res = Rdr.Read();
			if (res)
				Count--;
			return res && Count >= 0;
		}

		async Task<bool> ReadWithOffsetCountAsync(DbDataReader dbRdr, CancellationToken cancellationToken) {
			while (Offset > 0) {
				if (!await dbRdr.ReadAsync(cancellationToken)) {
					Offset = 0;
					return false;
				}
				Offset--;
			}
			var res = await dbRdr.ReadAsync(cancellationToken);
			if (res)
				Count--;
			return res && Count >= 0;
		}

		public override Task<bool> ReadAsync(CancellationToken cancellationToken) {
			if (Rdr is DbDataReader dbRdr) {
				return ReadWithOffsetCountAsync(dbRdr, cancellationToken);
			} else {
				// use default impl that uses sync "Read" where offset/count are handled
				return base.ReadAsync(cancellationToken);
			}
		}

	}
}


