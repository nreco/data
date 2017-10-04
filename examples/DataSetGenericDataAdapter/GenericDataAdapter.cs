using System;
using System.Collections.Generic;
using System.Text;
using System.Data.Common;
using System.Data;

using NReco.Data;

namespace DataSetGenericDataAdapter
{

	/// <summary>
	/// Generic implementation of <see cref="IDbDataAdapter"/>.
	/// </summary>
	/// <remarks>Note: this implementation ignores tables and columns mapping. You may enhance the code if you need this feature.</remarks>
    public class GenericDataAdapter : System.Data.Common.DbDataAdapter {

		IDbCommandBuilder CmdBuilder;
		DbConnection Conn;

		public GenericDataAdapter(IDbCommandBuilder cmdBuilder, DbCommand selectCmd) {
			CmdBuilder = cmdBuilder;
			SelectCommand = selectCmd;
		}

		public GenericDataAdapter(IDbCommandBuilder cmdBuilder, DbConnection conn) {
			CmdBuilder = cmdBuilder;
			Conn = conn;
		}

		IEnumerable<KeyValuePair<string,IQueryValue>> GetChangeset(DataTable t) {
			var res = new List<KeyValuePair<string, IQueryValue>>(t.Columns.Count);
			foreach (DataColumn col in t.Columns)
				if (!col.AutoIncrement && !col.ReadOnly) {
					res.Add(new KeyValuePair<string, IQueryValue>(col.ColumnName, new QVar(col.ColumnName).Set(null) ));
				}
			return res.ToArray();
		}

		const string OriginalSuffix = "__ORIGINAL";

		void InitDbCmd(DbCommand cmd, DataTable t) {
			foreach (DbParameter p in cmd.Parameters) {
				if (p.SourceColumn != null) {
					if (p.SourceColumn.EndsWith(OriginalSuffix)) {
						p.SourceColumn = p.SourceColumn.Substring(0, p.SourceColumn.Length - OriginalSuffix.Length);
						p.SourceVersion = DataRowVersion.Original;
					} else {
						p.SourceVersion = DataRowVersion.Current;
					}
					var col = t.Columns[p.SourceColumn];
					// you may use column metadata to initialize DbParameter in a special way if needed
				}
			}
			if (SelectCommand != null && SelectCommand.Connection != null) { 
				cmd.Connection = SelectCommand.Connection;
			} else {
				cmd.Connection = Conn;
			}
		}

		QNode ComposePkCondition(DataTable t) {
			var pkCondition = new QGroupNode(QGroupType.And);
			foreach (DataColumn col in t.PrimaryKey) {
				pkCondition.Nodes.Add(
					(QField)col.ColumnName == new QVar(col.ColumnName+OriginalSuffix).Set(null) );
			}
			return pkCondition;
		}

		protected override int Update(DataRow[] dataRows, DataTableMapping tableMapping) {
			// generate commands by first row table schema
			if (dataRows.Length>0) {
				var tbl = dataRows[0].Table;
				var changeset = GetChangeset(tbl);
				InsertCommand = (DbCommand)CmdBuilder.GetInsertCommand(tbl.TableName, changeset);
				InitDbCmd(InsertCommand, tbl);

				if (tbl.PrimaryKey!=null && tbl.PrimaryKey.Length>0) {
					var pkQuery = new Query(tbl.TableName, ComposePkCondition(tbl) );
					UpdateCommand = (DbCommand)CmdBuilder.GetUpdateCommand(pkQuery, changeset);
					InitDbCmd(UpdateCommand, tbl);

					DeleteCommand = (DbCommand)CmdBuilder.GetDeleteCommand(pkQuery);
					InitDbCmd(DeleteCommand, tbl);
				}
			}
			

			return base.Update(dataRows, tableMapping);
		}


	}

}
