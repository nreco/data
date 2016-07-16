using System;
using System.ComponentModel;
using System.Collections.Generic;

using Xunit;

namespace NReco.Data.Tests
{

	public class StringTemplateTests
	{
		[Fact]
		public void FormatTemplate() {
			var testStr = "TEST @SqlOrderBy[order by {0};order by u.id desc] TEST";
			var strTpl = new StringTemplate(testStr);
			strTpl.ReplaceMissedTokens = false;
			Assert.Equal(testStr, strTpl.FormatTemplate(new Dictionary<string, object>()));
			strTpl.ReplaceMissedTokens = true;
			Assert.Equal("TEST order by u.id desc TEST", strTpl.FormatTemplate(new Dictionary<string, object>()));

			Assert.Equal("TEST order by name TEST", strTpl.FormatTemplate(
				new Dictionary<string, object>() {
					{"SqlOrderBy", "name"}
				} ));


			Assert.Equal("1+2",
				new StringTemplate("@A[{0}+@B]",2).FormatTemplate(new Dictionary<string, object>() {
					{"A", 1}, {"B", 2}
				})
			);

			Assert.Equal("No replace: @Test",
				new StringTemplate("No replace: @@Test").FormatTemplate(new Dictionary<string, object>() {
					{"Test", "bla"}
				})
			);

			Assert.Equal(
				"and 1=2",
				new StringTemplate(
					"@class_id[and id in metadata_property_to_class(class_id=\"class_id\":var)[property_id]];and 1=2]").FormatTemplate(
					new Dictionary<string, object>() {
						{"class_id", ""}
					}
				)
			);

			Assert.Equal(
				"zzz@WAW;[]",
				new StringTemplate(
					"zzz@A[@WAW;;[]]]").FormatTemplate(
					new Dictionary<string, object>() {
						{"A", "1"}
					}
				)
			);
			Assert.Equal(
				"zzz [] ",
				new StringTemplate(
					"zzz@A[\\;; [\\] ]").FormatTemplate(
					new Dictionary<string, object>() {
						{"A", ""}
					}
				)
			);

		}
	}
}
