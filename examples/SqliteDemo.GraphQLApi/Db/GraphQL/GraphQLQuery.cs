using System.Collections.Generic;

using GraphQL.Types;

using SqliteDemo.GraphQLApi.Db.Models;
using GraphQL.Resolvers;

using NReco.Data;

namespace SqliteDemo.GraphQLApi.Db.GraphQL {
	public class GraphQLQuery : ObjectGraphType<object> {
		private IDatabaseMetadata _dbMetadata;
		private DbDataAdapter _dbAdapter;

		public GraphQLQuery(DbDataAdapter data, IDatabaseMetadata dbMetadata) {
			_dbMetadata = dbMetadata;
			_dbAdapter = data;

			Name = "Query";

			foreach (var metaTable in _dbMetadata.GetMetadataTables()) {
				var tableType = new TableType(metaTable);
				this.AddField(new FieldType() {
					Name = metaTable.TableName,
					Type = tableType.GetType(),
					ResolvedType = tableType,
					Resolver = new MyFieldResolver(metaTable, _dbAdapter),
					Arguments = new QueryArguments(
						tableType.TableArgs
					)
				});
				//lets add key to get list of current table
				var listType = new ListGraphType(tableType);
				this.AddField(new FieldType {
					Name = $"{metaTable.TableName}_list",
					Type = listType.GetType(),
					ResolvedType = listType,
					Resolver = new MyFieldResolver(metaTable, _dbAdapter),
					Arguments = new QueryArguments(
						tableType.TableArgs
					)
				});
			}
		}
	}

	public class MyFieldResolver : IFieldResolver {
		private TableMetadata _tableMetadata;
		private DbDataAdapter _dbAdapter;

		public MyFieldResolver(TableMetadata tableMetadata, DbDataAdapter data) {
			_tableMetadata = tableMetadata;
			_dbAdapter = data;
		}

		public object Resolve(ResolveFieldContext context) {
			var query = new Query(
				$"'{_tableMetadata.TableName}'"
			);
			ApplyArguments(query, context.Arguments);
			if (context.FieldName.Contains("_list")) {
				return _dbAdapter.Select(query).ToDictionaryList();
			} else {
				return _dbAdapter.Select(query).ToDictionary();
			}
		}

		private void ApplyArguments(Query q, IDictionary<string, object> args) {
			var grpAnd = QGroupNode.And();
			var applyConditions = false;
			foreach (var arg in args) {
				if (arg.Value != null) {
					grpAnd.Nodes.Add(
						(QField)arg.Key == new QConst(arg.Value)
					);
					applyConditions = true;
				}
			}

			if (applyConditions)
				q.Condition = grpAnd;
		}
	}
}
