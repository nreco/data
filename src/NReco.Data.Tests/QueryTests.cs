using System;

using Xunit;
using NReco.Data;

namespace NReco.Data.Tests
{

	public class QueryTests
	{
		[Fact]
		public void test_QSortField() {

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
	}
}
