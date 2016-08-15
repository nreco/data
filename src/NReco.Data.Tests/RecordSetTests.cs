using System;

using Xunit;
using NReco.Data;

namespace NReco.Data.Tests
{

	public class RecordSetTests
	{
		[Fact]
		public void CrudOperations() {
			
			var rs = new RecordSet(
				new[] {
					new RecordSet.Column("id", typeof(int)),
					new RecordSet.Column("name", typeof(string)),
					new RecordSet.Column("age", typeof(int))
				}
			);
			Assert.Throws<InvalidOperationException>( () => {
				rs.Add( new object[2]);
			});
			Assert.Throws<InvalidOperationException>( () => {
				rs.Add( new object[4]);
			});

			Action<int,string,int> addRow = (id, name, age) => {
				var r = rs.NewRow();
				r["id"] = id;
				r["name"] = name;
				r["age"] = age;
			};
			addRow(1, "John", 25);
			addRow(2, "Mary", 28);
			addRow(3, "Ray", 32);

			Assert.Equal(3, rs.Count);
			Assert.Equal( RecordSet.RowState.Added, rs[0].State & RecordSet.RowState.Added  );

			rs.AcceptChanges();
			Assert.Equal(3, rs.Count);
			Assert.Equal( RecordSet.RowState.Unchanged, rs[0].State );

			rs[2].Delete();
			Assert.Equal( RecordSet.RowState.Deleted, rs[2].State);
			rs.AcceptChanges();
			Assert.Equal(2, rs.Count);

			// add values array
			rs.Add(new object[] {4, "Adam", 45});
			Assert.Equal(3, rs.Count);

			rs[0]["name"] = "Bart";
			Assert.Equal( RecordSet.RowState.Modified, rs[0].State & RecordSet.RowState.Modified );
			rs[0].AcceptChanges();
			Assert.Equal( RecordSet.RowState.Unchanged, rs[0].State );
			Assert.Equal( "Bart", rs[0]["name"] );

			Assert.Throws<InvalidOperationException>( () => {
				var t = rs[0]["test"];
			});
		}

	}
}
