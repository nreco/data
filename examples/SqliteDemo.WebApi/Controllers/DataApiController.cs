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
        public async Task<List<Dictionary<string,object>>> Get(string relex) {
            var relexParser = new NReco.Data.Relex.RelexParser();
			var q = relexParser.Parse(relex);
			CheckTable(q.Table.Name);
			return await DbAdapter.Select(q).ToDictionaryListAsync().ConfigureAwait(false);
        }

        // GET api/db/Products/1
        [HttpGet("{table}/{id}")]
        public async Task<Dictionary<string,object>> Get(string table, string id)
        {
			CheckTable(table);
			var q = GetQueryByPk(table, id);
            return await DbAdapter.Select(q).ToDictionaryAsync().ConfigureAwait(false);
        }

        // POST api/db/Products
        [HttpPost("{table}")]
        public async Task<bool> Post(string table, [FromBody]IDictionary<string,object> values) {
			CheckTable(table);
			return await DbAdapter.InsertAsync(table, values).ConfigureAwait(false)>0;
		}

        // PUT api/db/Products/1 + serialized json object in body
        [HttpPut("{table}/{id}")]
        public async Task<bool> Put(string table, string id, [FromBody]IDictionary<string,object> values) {
			CheckTable(table);
			var q = GetQueryByPk(table, id);
			return await DbAdapter.UpdateAsync(q, values).ConfigureAwait(false)>0;
        }

        // DELETE api/db/Products/1
        [HttpDelete("{table}/{id}")]
        public async Task<bool> Delete(string table, string id) {
			CheckTable(table);
			var q = GetQueryByPk(table, id);
			return await DbAdapter.DeleteAsync(q).ConfigureAwait(false)>0;
        }
    }
}
