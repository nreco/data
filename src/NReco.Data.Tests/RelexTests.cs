using System;
using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.IO;
using System.Xml;
using System.Data;
using System.Data.SqlClient;

using NReco.Data.Relex;

using Xunit;

namespace NReco.Data.Tests
{

	public class RelexTests
	{
		string[] oldRelExSamples = new string[] {
			"expenses(expense_report_id = 5)[id, unit_uid, money_equivalent]",
			"tokens(id in custompage_to_token(left_uid=5 or left_uid=6)[right_uid] and type=6)[value]",
			"users(id>1 or id<=1)[*]",
			"users( id=null and id!=null )[*]" // test for compatibility
		};

		string[] oldRelExCommandTexts = new string[] {
			@"SELECT id,unit_uid,money_equivalent FROM expenses WHERE expense_report_id=@p0",
			@"SELECT value FROM tokens WHERE (id IN (SELECT right_uid FROM custompage_to_token WHERE (left_uid=@p0) Or (left_uid=@p1))) And (type=@p2)",
			@"SELECT * FROM users WHERE (id>@p0) Or (id<=@p1)",
			@"SELECT * FROM users WHERE (id IS  NULL) And (id IS NOT NULL)"
		};


		string[] relExSamples = new string[] {
			"accounts(login = \"Mike\" or id<=parent_id)[*]",
			"accounts(login = \"vit\"\"alik\" or id<=5)[*]",
			"accounts(1=2)[max(id),min(id)]",
			"users(id=\"\")[*]",
			"users_view[count]",
			"users( (id>\"1\" and id<\"5\") or (name like \"AAA\" and age<\"25\") )[*]",
			"users( (<idGroup> id>\"1\" and id<\"5\") or age<\"25\":int32 )[*]",
			"users( id in \"1,2,3\":int32[] and id !in \"4,5\":int32[] and id!=0 )[*]",
			"users( id=null and id!=null )[count(*)]",
			
			"users( (id!=1 and id!=2) and id!=3)[name]",
			"users( (<grname> id!=1 and id!=2) and id!=3)[name]",
			"users( (id!=1 and id!=2) and (id!=3))[name]",
			"users( (id!=1 and id!=2) and (id!=3 and id!=4))[name]",
			"users( (id!=1 and id!=2) and (id!=3 and (id!=4)))[name]",
			"users( (id!=1 and id!=2) and (( (id!=3 and (id!=4))) ))[name]",
			"users( (id!=1 and id!=2) or (id!=3))[name]",
			"users( 1=1 )[name;name,login]",
			"users( 1=1 )[name;\"name desc\"]",
			"users( 1=1 )[name;name desc,id asc,time]",
			"users( \"id\":field = 5 )[*]",
			"users( \"[user id]\":sql = 1 )[name, \"[last name]\"]"
		};

		string[] relExCommandTexts = new string[] {
			@"SELECT * FROM accounts WHERE (login=@p0) Or (id<=parent_id)",
			@"SELECT * FROM accounts WHERE (login=@p0) Or (id<=@p1)",
			@"SELECT max(id),min(id) FROM accounts WHERE @p0=@p1",
			@"SELECT * FROM users WHERE id=@p0",
			@"SELECT count FROM users_view",
			@"SELECT * FROM users WHERE ((id>@p0) And (id<@p1)) Or ((name LIKE @p2) And (age<@p3))",
			@"SELECT * FROM users WHERE ((id>@p0) And (id<@p1)) Or (age<@p2)",
			@"SELECT * FROM users WHERE (id IN (@p0,@p1,@p2)) And (NOT (id IN (@p3,@p4))) And (id<>@p5)",
			@"SELECT count(*) FROM users WHERE (id IS  NULL) And (id IS NOT NULL)",
			
			@"SELECT name FROM users WHERE (id<>@p0) And (id<>@p1) And (id<>@p2)",
			@"SELECT name FROM users WHERE ((id<>@p0) And (id<>@p1)) And (id<>@p2)",
			@"SELECT name FROM users WHERE (id<>@p0) And (id<>@p1) And (id<>@p2)",
			@"SELECT name FROM users WHERE (id<>@p0) And (id<>@p1) And (id<>@p2) And (id<>@p3)",
			@"SELECT name FROM users WHERE (id<>@p0) And (id<>@p1) And (id<>@p2) And (id<>@p3)",
			@"SELECT name FROM users WHERE (id<>@p0) And (id<>@p1) And (id<>@p2) And (id<>@p3)",
			@"SELECT name FROM users WHERE ((id<>@p0) And (id<>@p1)) Or (id<>@p2)",
			@"SELECT name FROM users WHERE @p0=@p1 ORDER BY name asc,login asc",
			@"SELECT name FROM users WHERE @p0=@p1 ORDER BY name desc",
			@"SELECT name FROM users WHERE @p0=@p1 ORDER BY name desc,id asc,time asc",
			@"SELECT * FROM users WHERE id=@p0",
			@"SELECT name,[last name] FROM users WHERE [user id]=@p0",
	};
		
		[Fact]
		public void test_Parse() {
			var relExParser = new RelexParser();
			
			// generate SQL by query
			var dbFactory = new DbFactory( SqlClientFactory.Instance );
			var cmdGenerator = new DbCommandBuilder(dbFactory);

			for (int i=0; i<oldRelExSamples.Length; i++) {
				string relEx = oldRelExSamples[i];
				Query q = relExParser.Parse(relEx);
				IDbCommand cmd = cmdGenerator.GetSelectCommand( q );

				Assert.Equal(oldRelExCommandTexts[i], cmd.CommandText.Trim() );
			}
			
			for (int i=0; i<relExSamples.Length; i++) {
				string relEx = relExSamples[i];
				Query q = relExParser.Parse(relEx);
				IDbCommand cmd = cmdGenerator.GetSelectCommand( q );
				
				Assert.Equal( relExCommandTexts[i], cmd.CommandText.Trim() );
			}

			// test for named nodes
			string relexWithNamedNodes = @"users( (<idGroup> id=null and id!=null) and (<ageGroup> age>5 or age<2) and (<emptyGroup>) )[count(*)]";
			Query qWithGroups = relExParser.Parse(relexWithNamedNodes);
			Assert.NotEqual(null, FindNodeByName(qWithGroups.Condition, "idGroup") );
			Assert.NotEqual(null, FindNodeByName(qWithGroups.Condition, "ageGroup") );
			Assert.NotEqual(null, FindNodeByName(qWithGroups.Condition, "emptyGroup") );
		
			// just a parse test for real complex relex
			string sss = "sourcename( ( ((\"False\"=\"True\") or (\"False\"=\"True\")) and \"contact-of\" in agent_to_agent_role( left_uid in agent_accounts(agent_accounts.id=\"\")[agent_id] and (right_uid=agent_institutions.id) )[role_uid] ) or ( ((agent_institutions.id in events( events.id in event_assignments( person_id in agent_accounts (agent_accounts.id=\"\")[agent_id] )[event_id] )[client_institution_id]) or (agent_institutions.id in events( events.id in event_assignments( person_id in agent_accounts (agent_accounts.id=\"\")[agent_id] )[event_id] )[supplier_institution_id])) and (\"False\"=\"True\") ) or ( (agent_institutions.id in agent_to_agent_role( (left_uid in agent_to_agent_role( left_uid in agent_accounts(agent_accounts.id=\"\")[agent_id] and role_uid='contact-of' )[right_uid]) and role_uid='supplier-of')[right_uid] ) or (agent_institutions.id in events( events.supplier_institution_id in agent_to_agent_role( (agent_to_agent_role.role_uid='contact-of') and (agent_to_agent_role.left_uid in agent_accounts(agent_accounts.id=\"\")[agent_id]) )[agent_to_agent_role.right_uid] )[events.client_institution_id]) or (agent_institutions.id in events( events.client_institution_id in agent_to_agent_role( (agent_to_agent_role.role_uid='contact-of') and (agent_to_agent_role.left_uid in agent_accounts(agent_accounts.id=\"\")[agent_id]) )[agent_to_agent_role.right_uid] )[events.supplier_institution_id]) ) or (\"False\"=\"True\") or ( (\"False\"=\"True\") and (agent_institutions.id in agent_to_agent_role( role_uid='supplier-of' and right_uid = \"\" )[left_uid]) ) or (\"False\"=\"True\") )[*]";
			relExParser.Parse(sss);


			var complexSortAndFields = "users.u( u.id=1 )[\"name+','\";id asc,id desc,\"sum(id)\"]";
			var complexQ = relExParser.Parse(complexSortAndFields);
			Assert.Equal(1, complexQ.Fields.Length);
			Assert.Equal(3, complexQ.Sort.Length);

			Assert.Throws<RelexParseException>(() => {
				relExParser.Parse("users[id");
			});
		}

		[Fact]
		public void test_RelexParseSpeed() {
			var relExParser = new RelexParser();

			var stopwatch = new System.Diagnostics.Stopwatch();
			stopwatch.Start();
			var iterations = 1000;
			for (int iter = 0; iter < iterations; iter++) {
				for (int i = 0; i < relExSamples.Length; i++) {
					string relEx = relExSamples[i];
					Query q = relExParser.Parse(relEx);
				}
			}
			stopwatch.Stop();
			Console.WriteLine("Speedtest for relex parse ({1} times): {0}", stopwatch.Elapsed, iterations * relExSamples.Length); 

		}

		[Fact]
		public void test_RelexVar() {
			var relExParser = new RelexParser();
			var cnd = relExParser.ParseCondition("\"hey:%{0}\":var = \"hey\" ");
			Assert.True(cnd is QConditionNode);
			Assert.True(((QConditionNode)cnd).LValue is QVar);
			((QVar)((QConditionNode)cnd).LValue).Set("yeh");

			Assert.Equal("\"%yeh\"=\"hey\"", new RelexBuilder().BuildRelex(cnd) );
		}

		protected QNode FindNodeByName(QNode node, string name) {
			if (node.Name!=null && node.Name==name)
				return node;
			if (node.Nodes!=null)
				foreach (QNode cNode in node.Nodes) {
					QNode r = FindNodeByName(cNode, name);
					if (r!=null) return r;
				}

			return null;
		}

		
		[Fact]
		public void test_GetLexem() {
			TestRelExQueryParser relExParser = new TestRelExQueryParser();
			
			string expression = relExSamples[0];
			int startIdx = 0;
			int endIdx = 0;
			RelexParser.LexemType lexemType;

			var testLexemTypeSeq = new string[] {
				"Name", "accounts",
				"Delimiter", "(",
				"Name", "login",
				"Delimiter", "=",
				"QuotedConstant", "\"Mike\"",
				"Name", "or",
				"Name", "id",
				"Delimiter", "<",
				"Delimiter", "=",
				"Name", "parent_id",
				"Delimiter", ")",
				"Delimiter", "[",
				"Delimiter", "*",
				"Delimiter", "]"
			};

			int idx = 0;
			while ( (lexemType=relExParser.TestGetLexemType(expression,startIdx, out endIdx))!=RelexParser.LexemType.Stop) {
				var testLexemType = testLexemTypeSeq[idx*2];
				var testLexemVal = testLexemTypeSeq[idx*2+1];
				Assert.Equal(testLexemType, lexemType.ToString() );
				Assert.Equal(testLexemVal, expression.Substring(startIdx, endIdx-startIdx).Trim() );
				startIdx = endIdx;

				idx++;
			}
			
		}
		
		
		public class TestRelExQueryParser : RelexParser {
			
			public RelexParser.LexemType TestGetLexemType(string s, int startIdx, out int endIdx) {
				return GetLexemType(s, startIdx, out endIdx);
			}
		
		}
		
		
		
		
		
		
	}
}
