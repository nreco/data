using System;

using Xunit;
using NReco.Data;

namespace NReco.Data.Tests
{

	public class QueryTests
	{
		[Fact]
		public void QSortField() {

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
