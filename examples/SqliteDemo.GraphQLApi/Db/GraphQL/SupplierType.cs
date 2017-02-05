using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


using GraphQL.Types;
using SqliteDemo.GraphQLApi.Db.Models;

namespace SqliteDemo.GraphQLApi.Db.GraphQL {
	public class SupplierType : ObjectGraphType<Supplier> {
		public SupplierType() {
			Field(x => x.SupplierID).Description("The Id of the Supplier.");
			Field(x => x.CompanyName, nullable: true).Description("The title of the Supplier.");
			Field(x => x.ContactName, nullable: true).Description("The contact of the Supplier.");
		}
	}
}