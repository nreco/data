using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

using NReco.Data;

namespace SqliteDemo.WebApi.Controllers
{
    [Route("api/db")]
    public class DataApiController : Controller
    {
		DbDataAdapter DbAdapter;

		Dictionary<string,string> allowedTableToIdName;

		public DataApiController(DbDataAdapter dbAdapter) {
			DbAdapter = dbAdapter;

			allowedTableToIdName = new Dictionary<string, string>() {
				{"Categories", "CategoryID"},
				{"Customers", "CustomerID"},
				{"Orders", "OrderID"},
				{"Products", "ProductID"}
			};
		}

		void CheckTable(string table) {
			if (!allowedTableToIdName.ContainsKey(table))
				throw new NotSupportedException($"Queries to table {table} are not allowed");
		}
		Query GetQueryByPk(string table, object idValue) {
			var pkFldName = allowedTableToIdName[table];
			var q = new Query(table, (QField)pkFldName == new QConst(idValue) );
			return q;
		}

        // GET api/db/rows?relex=Products[*;ProductID asc]
        [HttpGet("rows")]
		[HttpPost("rows")]
        public Task<List<Dictionary<string,object>>> Get(string relex) {
            var relexParser = new NReco.Data.Relex.RelexParser();
			var q = relexParser.Parse(relex);
			CheckTable(q.Table.Name);
			return DbAdapter.Select(q).ToDictionaryListAsync();
        }

        // GET api/db/Products/1
        [HttpGet("{table}/{id}")]
        public Task<Dictionary<string,object>> Get(string table, string id)
        {
			CheckTable(table);
			var q = GetQueryByPk(table, id);
            return DbAdapter.Select(q).ToDictionaryAsync<Dictionary<string,object>>();
        }

        // POST api/db/Products
        [HttpPost("{table}")]
        public bool Post(string table, [FromBody]IDictionary<string,object> values) {
			CheckTable(table);
			return DbAdapter.Insert(table, values)>0;
		}

        // PUT api/db/Products/1 + serialized json object in body
        [HttpPut("{table}/{id}")]
        public bool Put(string table, string id, [FromBody]IDictionary<string,object> values) {
			CheckTable(table);
			var q = GetQueryByPk(table, id);
			return DbAdapter.Update(q, values)>0;
        }

        // DELETE api/db/Products/1
        [HttpDelete("{table}/{id}")]
        public bool Delete(string table, string id) {
			CheckTable(table);
			var q = GetQueryByPk(table, id);
			return DbAdapter.Delete(q)>0;
        }
    }
}
