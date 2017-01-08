using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Common;
using System.Data;

namespace NReco.Data {
	
	/// <summary>
	/// The exception that is thrown when execution of <see cref="IDbCommand"/> is failed.
	/// </summary>
	public class ExecuteDbCommandException : Exception {
		
		/// <summary>
		/// Gets a command that generated the error. 
		/// </summary>
		public IDbCommand Command { get; private set; }

		public ExecuteDbCommandException(IDbCommand dbCmd, Exception innerException) 
			: base(innerException.Message, innerException) {
			Command = dbCmd;
		}

	}
}
