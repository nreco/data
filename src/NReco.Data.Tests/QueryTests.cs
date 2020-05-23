using System;
using System.ComponentModel;
using Xunit;
using NReco.Data;

namespace NReco.Data.Tests
{

	public class QueryTests
	{
		[Fact]
		public void QSort() {

			QSort fld = (QSort)"name";
			Assert.Equal( fld.Field.Name, "name");
			Assert.Equal( fld.SortDirection, ListSortDirection.Ascending);
			
			fld = (QSort)"email desc ";
			Assert.Equal( fld.Field.Name, "email");
			Assert.Equal( fld.SortDirection, ListSortDirection.Descending);
			
			fld = (QSort)"email  desc ";
			Assert.Equal( fld.Field.Name, "email");
			Assert.Equal( fld.SortDirection, ListSortDirection.Descending);

			fld = (QSort)"  email  desc ";
			Assert.Equal( fld.Field.Name, "email");
			Assert.Equal( fld.SortDirection, ListSortDirection.Descending);			
			
			fld = (QSort)"position asc";
			Assert.Equal( fld.Field.Name, "position");
			Assert.Equal( fld.SortDirection, ListSortDirection.Ascending);
		}

		[Fact]
		public void QField() {
			var f = new QField("simple");
			Assert.Equal("simple", f.Name);
			Assert.Null(f.Expression);
			Assert.Null(f.Prefix);

			f = new QField("t.field");
			Assert.Equal("field", f.Name);
			Assert.Null(f.Expression);
			Assert.Equal("t", f.Prefix);

			f = new QField("dbo.t.field");
			Assert.Equal("field", f.Name);
			Assert.Null(f.Expression);
			Assert.Equal("dbo.t", f.Prefix);

			f = new QField("sum(field)");
			Assert.Equal("sum(field)", f.Name);
			Assert.Equal("sum(field)", f.Expression);
			Assert.Null(f.Prefix);

			f = new QField("sum(t.field)");
			Assert.Equal("sum(t.field)", f.Name);
			Assert.Equal("sum(t.field)", f.Expression);
			Assert.Null(f.Prefix);

			f = new QField("sum(field) as  fld_test");
			Assert.Equal("fld_test", f.Name);
			Assert.Equal("sum(field)", f.Expression);
			Assert.Null(f.Prefix);

			f = new QField("CAST(field as nvarchar)");
			Assert.Equal("CAST(field as nvarchar)", f.Name);
			Assert.Equal("CAST(field as nvarchar)", f.Expression);
			Assert.Null(f.Prefix);
		}

		[Fact]
		public void QAggregateField() {
			var f = new QAggregateField("amount_sum", "sum", (QField)"amount");
			Assert.Equal("sum(amount)", f.Expression);
			Assert.Equal("amount_sum", f.Name);
		}

		[Fact]
		public void SetVars() {
			var qVar1 = new QVar("$var1");
			var qVar2 = new QVar("$var2");
			var q = new Query("test",
					(QField)"name" == qVar1 &
					(
						(QConst)1 != (QConst)2
						|
						new Query("test2", (QField)"id" > qVar2 )
					)
				);

			q.SetVars( (v) => {
				switch (v.Name) {
					case "$var1": v.Set("John"); break;
					case "$var2": v.Set(2); break;
				}
			});
			Assert.Equal("John", qVar1.Value);
			Assert.Equal(2, qVar2.Value);

			q.SetVars( (v) => v.Unset() );

			Assert.Throws<InvalidOperationException>( () => { var v = qVar1.Value; } );
			Assert.Throws<InvalidOperationException>( () => { var v = qVar2.Value; } );
		}
	}
}
