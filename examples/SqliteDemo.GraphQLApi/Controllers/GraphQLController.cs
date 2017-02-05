using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.Text;

using Microsoft.AspNetCore.Mvc;

using SqliteDemo.GraphQLApi.Db.GraphQL;
using SqliteDemo.GraphQLApi.Db.Repositories;
using SqliteDemo.GraphQLApi.Db.Interfaces;

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
		IDataRepository db;
		Schema graphQLSchema;

		public GraphQLController(DataRepository dataRepository, Schema schema) {
			db = dataRepository;
			graphQLSchema = schema;
		}

		// GET api/values
		[HttpGet]
		public IEnumerable<string> Get() {
			return new string[] { "value1", "value2" };
		}

		// GET api/article/5
		[HttpGet("{query}")]
		public async Task<string> GraphQL(string query) {
			var result = await new DocumentExecuter().ExecuteAsync(_ => {
				_.Schema = graphQLSchema;
				_.Query = query;
				/*@"
               query { supplier(id: 2) { supplierID companyName } }

              ";*/
			}).ConfigureAwait(false);

			var json = new DocumentWriter(indent: true).Write(result);
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