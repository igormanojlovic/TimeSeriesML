using System;

namespace MTSR
{
	public class SqlDB : IDisposable
	{
		public SqlDB(string name, bool recreate)
		{
			CreateDatabase(name, recreate);
			Connection = new SqlDBConnection(name);
		}

		public SqlDBConnection Connection { get; }

		private void CreateDatabase(string name, bool recreate)
		{
			using (var connection = new SqlDBConnection("master"))
			{
				connection.RunScript($"{nameof(MTSR)}.SQL.{nameof(SqlDB)}.sql");
				using (var cmd = connection.CreateProcedureCommand(nameof(CreateDatabase)))
				{
					cmd.AddParameter(nameof(name), name);
					cmd.AddParameter(nameof(recreate), recreate);
					cmd.ExecuteNonQuery();
				}
			}
		}

		public override string ToString() => Connection.ToString();
		public void Dispose() => Connection.Dispose();
	}
}
