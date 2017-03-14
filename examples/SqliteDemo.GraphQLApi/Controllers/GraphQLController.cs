using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.Text;

using Microsoft.AspNetCore.Mvc;

using SqliteDemo.GraphQLApi.Db.GraphQL;

using GraphQL;
using GraphQL.Http;
using GraphQL.Instrumentation;
using GraphQL.Execution;
using GraphQL.Types;
using GraphQL.Validation;
using GraphQL.Validation.Complexity;

namespace SqliteDemo.GraphQLApi.Controllers {
	[Route("api/[controller]")]
	public class GraphQLController : Controller {
		Schema graphQLSchema;

		public GraphQLController(Schema schema) {
			graphQLSchema = schema;
		}

		[HttpGet("")]
		public async Task<string> Get() {
			return await Get("{ Customers_list { CustomerID CompanyName } }");
		}

		[HttpGet("{query}")]
		public async Task<string> Get(string query) {
			//query = @"{ Customers(CustomerID: ""ALFKI"") { CustomerID CompanyName } }";
			//query = @"{ Customers_list { CustomerID CompanyName } }";

			var result = await new DocumentExecuter().ExecuteAsync(
				new ExecutionOptions() {
					Schema = graphQLSchema,
					Query = query
				}
			).ConfigureAwait(false);

			var json = new DocumentWriter(indent: true).Write(result.Data);
			return json;
		}

		// POST api/values
		[HttpPost]
		public void Post([FromBody]string value) {
		}

		// PUT api/values/5
		[HttpPut("{id}")]
		public void Put(int id, [FromBody]string value) {
		}

		// DELETE api/values/5
		[HttpDelete("{id}")]
		public void Delete(int id) {
		}
	}
}