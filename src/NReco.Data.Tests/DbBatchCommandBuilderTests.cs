using System;
using System.Data;
using System.Data.Common;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Data.SqlClient;

using Xunit;
using NReco.Data;

namespace NReco.Data.Tests {

	public class DbBatchCommandBuilderTests {


		[Fact]
		public void BatchInsertCommand() {
			var dbFactory = new DbFactory( SqlClientFactory.Instance );
			var cmdGenerator = new DbBatchCommandBuilder(dbFactory);
			
			Assert.Throws<InvalidOperationException>( () => { cmdGenerator.EndBatch(); });

			cmdGenerator.BeginBatch();

			cmdGenerator.GetInsertCommand( "test", new { FieldA = "A", FieldB = 0 } );
			cmdGenerator.GetInsertCommand( "test", new { FieldC = "C", FieldD = 1 } );
			
			var batchCmd = cmdGenerator.EndBatch();

			Assert.Equal(
				@"INSERT INTO test (FieldA,FieldB) VALUES (@p0,@p1);INSERT INTO test (FieldC,FieldD) VALUES (@p2,@p3)",
				batchCmd.CommandText);
			Assert.Equal(4, batchCmd.Parameters.Count);
			
		}
		
		
	}
}
