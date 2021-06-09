using System;
using System.Data;
using System.Data.Sql;
using System.Data.SqlClient;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace MTSR
{
	public class SqlDBConnection : IDisposable
	{
		#region Fields

		private static string defaultInstance = null;
		private readonly SqlConnectionStringBuilder builder;
		private readonly object connectionSync = new object();
		private SqlConnection connection;

		#endregion

		#region Constructors

		public SqlDBConnection(string database)
		{
			builder = new SqlConnectionStringBuilder
			{
				DataSource = $@".\{DefaultInstance}",
				InitialCatalog = database,
				IntegratedSecurity = true,
				ConnectTimeout = 10
			};
		}

		#endregion

		#region Properties

		private SqlConnection Connection
		{
			get
			{
				if (connection == null || connection.State == ConnectionState.Broken || connection.State == ConnectionState.Closed)
				{
					lock (connectionSync)
					{
						if (connection == null || connection.State == ConnectionState.Broken || connection.State == ConnectionState.Closed)
						{
							TryDisposeConnection();
							connection = new SqlConnection(builder.ConnectionString);
							connection.Open();
						}
					}
				}

				return connection;
			}
		}

		private static string DefaultInstance
		{
			get
			{
				if (defaultInstance != null)
				{
					return defaultInstance;
				}

				var table = SqlDataSourceEnumerator.Instance.GetDataSources();
				foreach (DataRow row in table.Rows)
				{
					return defaultInstance = row["InstanceName"].ToString();
				}

				throw new Exception("SQL Server instance not found. Please make sure that SQL Server and SQL Server Browser services are started and that at least one SQL Server instance is available.");
			}
		}

		public string Server => DefaultInstance;
		public string Database => Connection.Database;

		#endregion

		#region Private Methods

		private string GetText(string filePath)
		{
			var splitByBatch = new Regex(@"^\s*?GO\s*?$", RegexOptions.Multiline | RegexOptions.CultureInvariant | RegexOptions.Compiled);
			var script = Assembly.GetExecutingAssembly().GetText(filePath);
			var cmd = new StringBuilder(script.Length);
			foreach (string batch in splitByBatch.Split(script))
			{
				cmd.AppendFormat("EXECUTE sp_executesql @statement = N'{0}'", batch.Replace("'", "''"));
				cmd.AppendLine();
			}

			return cmd.ToString();
		}

		private SqlCommand CreateCommand(CommandType type, string text)
		{
			SqlCommand cmd = Connection.CreateCommand();
			cmd.CommandTimeout = 3600;
			cmd.CommandType = type;
			cmd.CommandText = text;
			return cmd;
		}

		private void TryDisposeConnection()
		{
			try { connection?.Dispose(); } catch { }
		}

		#endregion

		#region Public Methods

		public SqlCommand CreateTextCommand(string text) => CreateCommand(CommandType.Text, text);
		public SqlCommand CreateProcedureCommand(string text) => CreateCommand(CommandType.StoredProcedure, text);
		public int RunScript(string filePath) => Run(GetText(filePath));
		public int Run(string text)
		{
			using (var cmd = CreateTextCommand(text))
			{
				return cmd.ExecuteNonQuery();
			}
		}

		public override string ToString() => builder.InitialCatalog;
		public void Dispose() => TryDisposeConnection();

		#endregion
	}
}
