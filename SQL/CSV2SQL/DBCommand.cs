using System;
using System.Data.SqlClient;

namespace CSV2DB
{
	public class DBCommand : IDisposable
	{
		#region Fields

		private readonly SqlCommand command;
		private bool disposed = false;

		#endregion

		#region Constructors

		public DBCommand(SqlCommand command)
			=> this.command = command ?? throw new ArgumentNullException("command");

		#endregion

		#region Properties

		public string CommandText
		{
			get { return command.CommandText; }
			set { command.CommandText = value; }
		}

		#endregion

		#region Private Methods

		private void Execute(Action action, int retryCount)
		{
			if (retryCount <= 0)
			{
				action.Invoke();
			}

			while (retryCount > 0)
			{
				try
				{
					action.Invoke();
					return;
				}
				catch (Exception)
				{
					retryCount--;
					if (retryCount == 0)
					{
						throw;
					}
				}
			}

			throw new Exception("Failed to execute DB command.");
		}

		#endregion

		#region Public Methods

		public void AddParameter(string name, object value) => command.Parameters.AddWithValue(name, value);

		public void Execute() => Execute(delegate { command.ExecuteNonQuery(); }, 3);

		public void Dispose()
		{
			if (disposed)
			{
				return;
			}

			command.Dispose();
			disposed = true;
		}

		#endregion
	}
}
