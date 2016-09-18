using System;
using System.Collections.Generic;

using Xunit;
using NReco.Data;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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

			// columns collection
			Assert.Equal(3, rs.Columns.Count);
			Assert.Equal(1, rs.Columns.GetOrdinal("name"));
			Assert.Equal("age", rs.Columns[2].Name);
			Assert.Equal("name", rs.Columns["name"].Name);

			Assert.Throws<ArgumentException>( () => {
				rs.Add( new object[2]);
			});
			Assert.Throws<ArgumentException>( () => {
				rs.Add( new object[4]);
			});

			Action<int,string,int> addRow = (id, name, age) => {
				var r = rs.Add();
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

			// add dictionary
			rs.Add(new Dictionary<string,object>() {
				{"name", "Gomer"}
			});
			Assert.Equal(4, rs.Count);

			// typed accessor
			Assert.Equal( "Gomer", rs[3].Field<string>("name") );
			Assert.Equal( null, rs[3].Field<int?>("age") );
			Assert.Equal( 45, rs[2].Field<int?>("age") );

			rs[0]["name"] = "Bart";
			Assert.Equal( RecordSet.RowState.Modified, rs[0].State & RecordSet.RowState.Modified );
			rs[0].AcceptChanges();
			Assert.Equal( RecordSet.RowState.Unchanged, rs[0].State );
			Assert.Equal( "Bart", rs[0]["name"] );


			Assert.Throws<ArgumentException>( () => {
				var t = rs[0]["test"];
			});
		}

		[Fact]
		public void RecordSetReader() {
			var testRS = new RecordSet(new [] {
					new RecordSet.Column("id", typeof(int)),
					new RecordSet.Column("name", typeof(string)),
					new RecordSet.Column("amount", typeof(decimal)),
					new RecordSet.Column("added_date", typeof(DateTime))			
			});
			for (int i=0; i<100; i++) {
				testRS.Add( new object[] {
					i, "Name"+i.ToString(), i%20, new DateTime(2000, 1, 1).AddMonths(i)
				});
			}

			var rdr = new RecordSetReader(testRS);

			Assert.True( rdr.HasRows );
			Assert.Throws<InvalidOperationException>( () => { var o = rdr[0]; });
			
			Assert.True( rdr.Read() );
			Assert.Equal( 4, rdr.FieldCount );
			Assert.Equal("id", rdr.GetName(0));
			Assert.Equal(2, rdr.GetOrdinal("amount"));
			Assert.Equal("added_date", rdr.GetName(3));
			Assert.Throws<IndexOutOfRangeException>( () => { var o = rdr.GetName(4); });

			Assert.Equal(0, rdr.GetInt32(0) );
			Assert.Equal(0, rdr[0]);
			Assert.Equal("Name0", rdr[1]);
			Assert.Equal(0, rdr[2]);
			Assert.Equal(1, rdr.GetDateTime(3).Month);

			int cnt = 1;
			while (rdr.Read()) {
				Assert.Equal(cnt, rdr[0] );
				cnt++;
			}
			Assert.Throws<InvalidOperationException>( () => { var o = rdr[0]; });

			rdr.Dispose();
			Assert.Throws<InvalidOperationException>( () => { var o = rdr.FieldCount; });
			Assert.Throws<InvalidOperationException>( () => { var o = rdr.GetOrdinal("id"); });

			Assert.Equal(100, cnt);

			// read RS from RecordSetReader
			var testRSCopy = RecordSet.FromReader( new RecordSetReader(testRS) );
			Assert.Equal( testRS.Count, testRSCopy.Count );
			Assert.Equal( testRS.Columns.Count, testRSCopy.Columns.Count );

			// read into initialized RecordSet
			var newRS = new RecordSet(
				new[] {
					new RecordSet.Column("id", typeof(int)),
					new RecordSet.Column("name", typeof(string))
				}
			);
			newRS.Load( new RecordSetReader(testRS) );
			Assert.Equal(testRS.Count, newRS.Count);
			Assert.Equal("Name99", newRS[99].Field<string>("name"));
		}

		[Fact]
		public void RecordSet_FromModel() {
			
			var rs1 = RecordSet.FromModel<PersonModel>();

			Assert.Equal( 5, rs1.Columns.Count );
			Assert.Equal( 1, rs1.PrimaryKey.Length );
			Assert.Equal("id", rs1.PrimaryKey[0].Name );
			Assert.True(rs1.PrimaryKey[0].AutoIncrement );
			Assert.True(rs1.PrimaryKey[0].ReadOnly );   
			
			Assert.Equal("first_name", rs1.Columns[1].Name );
			
			var rs2 = RecordSet.FromModel( new PersonModel() { Id = 9, FirstName = "John" }, RecordSet.RowState.Modified );
			Assert.Equal(1, rs2.Count);
			Assert.Equal(9, rs2[0].Field<int>("id") );
			Assert.Equal("John", rs2[0].Field<string>("first_name") );
			Assert.Equal(RecordSet.RowState.Modified, rs2[0].State );
		}


		[Table("persons")]
		public class PersonModel {
			
			[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
			[Key]
			[Column("id")]
			public int? Id { get; set; }
			
			[Column("first_name")]
			public string FirstName { get; set; }

			[Column("last_name")]
			public string LastName { get; set; }
			
			[Column("birthday")]
			public DateTime? BirthDay { get; set; }

			// not annotated field
			public bool Active;

			[NotMapped]
			public string Name { 
				get { return $"{FirstName} {LastName}"; }
			}
		}


	}
}
