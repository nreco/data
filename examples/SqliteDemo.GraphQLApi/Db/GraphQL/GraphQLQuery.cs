using GraphQL.Types;

using SqliteDemo.GraphQLApi.Db.Models;
using SqliteDemo.GraphQLApi.Db.Interfaces;
using SqliteDemo.GraphQLApi.Db.Repositories;

namespace SqliteDemo.GraphQLApi.Db.GraphQL {
	public class GraphQLQuery : ObjectGraphType<object> {
		public GraphQLQuery(IDataRepository data) {
			Name = "Query";

			Field<SupplierType>(
				"supplier",
				arguments: new QueryArguments(
					new QueryArgument<NonNullGraphType<IntGraphType>> { Name = "id", Description = "id of the supplier" }
				),
				resolve: context => data.GetSupplierById(context.GetArgument<int>("id"))
			);
		}
	}
}
