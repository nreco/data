using System;
using System.ComponentModel;
using System.Collections.Generic;

using Xunit;

namespace NReco.Data.Tests
{

	public class StringTemplateTests {
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
				}));


			Assert.Equal("1+2",
				new StringTemplate("@A[{0}+@B]", 2).FormatTemplate(new Dictionary<string, object>() {
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
				"{ $and: [ {\"_id\":{$exists:true}},  { \"borough\" : \"1\" },   { \"cuisine\" : { $regex : \"2\" } },   ] }",
				new StringTemplate(
					"{ $and: [ {\"_id\":{$exists:true}}, @borough[ {{ \"borough\" : {0} }}, ] @cuisine[ {{ \"cuisine\" : {{ $regex : {0} }} }}, ]  ] }").FormatTemplate(
					new Dictionary<string, object>() {
						{"borough", "\"1\""},
						{"cuisine", "\"2\""}
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

			// placeholder syntax @(name)
			Assert.Equal(
				"@(AAA",
				new StringTemplate(
					"@(AAA").FormatTemplate(
					new Dictionary<string, object>() { { "AAA", "BBB" } }
				)
			);
			Assert.Equal(
				"BBB",
				new StringTemplate(
					"@(AAA)").FormatTemplate(
					new Dictionary<string, object>() { { "AAA", "BBB" } }
				)
			);
			Assert.Equal(
				"AAA_BBB",
				new StringTemplate(
					"@(123)_@aaa").FormatTemplate(
					new Dictionary<string, object>() {
						{"123", "AAA"},
						{"aaa", "BBB"}
					}
				)
			);
			Assert.Equal(
				" _B_",
				new StringTemplate(
					" @(A)[_{0}_]").FormatTemplate(
					new Dictionary<string, object>() {
						{"A", "B"}
					}
				)
			);

		}

		[Fact]
		public void NestedTokens() {
			Assert.Equal("1+(2) ",
				(new StringTemplate("@A[{0}+@B[({0})] ]") {
					ReplaceNestedTokens = true
				}).FormatTemplate(new Dictionary<string, object>() {
					{"A", 1}, {"B", 2}
				})
			);
			Assert.Equal("2",
				(new StringTemplate("@A[@B]") {
					ReplaceNestedTokens = true
				}).FormatTemplate(new Dictionary<string, object>() {
					{"A", 1}, {"B", 2}
				})
			);
			Assert.Equal("@1",
				(new StringTemplate("@A[@{0}]") {
					ReplaceNestedTokens = true
				}).FormatTemplate(new Dictionary<string, object>() {
					{"A", 1}, {"B", 2}
				})
			);
			Assert.Equal("3+2+1",
				(new StringTemplate("@A[@B[@C+{0}]+{0}]") {
					ReplaceNestedTokens = true
				}).FormatTemplate(new Dictionary<string, object>() {
					{"A", 1}, {"B", 2}, {"C", 3}
				})
			);
			Assert.Equal("2]",
				(new StringTemplate(@"@A[@B[1;2\]]]") {
					ReplaceNestedTokens = true
				}).FormatTemplate(new Dictionary<string, object>() {
					{"A", 1}, {"B", null}
				})
			);
			Assert.Equal("2]",
				(new StringTemplate(@"@A[@B[1;2]\]]") { // escaped \] in nested
					ReplaceNestedTokens = true
				}).FormatTemplate(new Dictionary<string, object>() {
					{"A", 1}, {"B", null}
				})
			);
			Assert.Equal("2]",
				(new StringTemplate(@"@A[@B[1;2]]]]") {  // double ]] in outer
					ReplaceNestedTokens = true
				}).FormatTemplate(new Dictionary<string, object>() {
					{"A", 1}, {"B", null}
				})
			);
			Assert.Equal("2-]]",
				(new StringTemplate(@"@A[@B[1;2]]-]]") {  // external ]] remains ]]
					ReplaceNestedTokens = true
				}).FormatTemplate(new Dictionary<string, object>() {
					{"A", 1}, {"B", null}
				})
			);
			Assert.Equal("2[\\]",
				(new StringTemplate(@"@A[@B\[\\\]]") {  // backslash escaped 
					ReplaceNestedTokens = true
				}).FormatTemplate(new Dictionary<string, object>() {
					{"A", 1}, {"B", 2}
				})
			);

			Assert.Equal("{ 2 }",
				(new StringTemplate(@"@A[@B[{{ {0} }}]]") {  // escaped { inside nested
					ReplaceNestedTokens = true
				}).FormatTemplate(new Dictionary<string, object>() {
					{"A", 1}, {"B", 2}
				})
			);

			Assert.Equal("{0}",
				(new StringTemplate(@"@A[@B[\{0\}]]") {  // escaped { inside nested
					ReplaceNestedTokens = true
				}).FormatTemplate(new Dictionary<string, object>() {
					{"A", 1}, {"B", 2}
				})
			);
			Assert.Equal("{[{}]}",
				(new StringTemplate(@"@A[@B[\{@C[\[\{\}\]]\}]]") {  // escaped { inside nested
					ReplaceNestedTokens = true
				}).FormatTemplate(new Dictionary<string, object>() {
					{"A", 1}, {"B", 2}, {"C", 3}
				})
			);
		}
	}
}
