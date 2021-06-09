using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Sql;
using System.Data.SqlClient;

namespace CSV2DB
{
	public class DBConnection : IDisposable
	{
		#region Fields

		private readonly static HashSet<ConnectionState> Closed = new HashSet<ConnectionState>()
			{ ConnectionState.Broken, ConnectionState.Closed };
		private SqlConnection connection;
		private bool disposed = false;

		#endregion

		#region Constructors

		public DBConnection(string instance, string database)
		{
			connection = new SqlConnection(CreateBuilder(instance, database).ConnectionString);
			CheckConnection();
		}

		#endregion

		#region Properties

		public static string DefaultInstance
		{
			get
			{
				var table = SqlDataSourceEnumerator.Instance.GetDataSources();
				foreach (DataRow row in table.Rows)
				{
					return $@".\{row["InstanceName"]}";
				}

				throw new Exception("SQL Server instance not found. Please make sure that SQL Server and SQL Server Browser services are started and that at least one SQL Server instance is available.");
			}
		}

		#endregion

		#region Methods

		private SqlConnectionStringBuilder CreateBuilder(string instance, string database)
			=> new SqlConnectionStringBuilder
			{
				DataSource = instance.Contains(@"\") ? instance : $@".\{instance}",
				InitialCatalog = database,
				IntegratedSecurity = true,
				ConnectTimeout = 60
			};

		private void CheckConnection()
		{
			if (!Closed.Contains(connection.State)) return;
			var connectionString = connection.ConnectionString;
			try { connection.Dispose(); } catch (Exception) { }
			connection = new SqlConnection(connectionString);
			connection.Open();
		}

		public DBCommand CreateCommand()
		{
			CheckConnection();
			var command = connection.CreateCommand();
			command.CommandTimeout = 3600;
			return new DBCommand(command);
		}

		public void Dispose()
		{
			if (disposed)
			{
				return;
			}

			connection.Dispose();
			disposed = true;
		}

		#endregion
	}
}
