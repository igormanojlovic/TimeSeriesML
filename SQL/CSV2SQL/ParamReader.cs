using System;
using System.Data;
using System.Data.Sql;
using System.IO;

namespace CSV2DB
{
	public class ParamReader
	{
		#region Private Methods

		private void PrintParam(string name) => Console.Write($"{name}: ");

		private void ReadDataFolder(Params result)
		{
			do
			{
				PrintParam("Folder");

				result.DataFolder = Console.ReadLine().Trim();
				if (Directory.Exists(result.DataFolder))
				{
					return;
				}

				Console.WriteLine("Folder does not exist.");
			} while (true);
		}

		private void ReadSeparator(Params result)
		{
			do
			{
				PrintParam("Separator");

				string input = Console.ReadLine();
				if (string.IsNullOrEmpty(input))
				{
					Console.WriteLine($"Separator is invalid.");
				}
				else
				{
					try
					{
						result.CSVSeparator = Convert.ToChar(input);
						return;
					}
					catch (Exception)
					{
						Console.WriteLine($"Separator is invalid.");
					}
				}
			} while (true);
		}

		private void ReadLineFromTo(Params result)
		{
			PrintParam("Lines (from-to or all='')");

			var words = Console.ReadLine().Trim().Split('-');
			if (words.Length < 2 || !int.TryParse(words[0], out int from) || !int.TryParse(words[1], out int to))
			{
				return;
			}

			result.LineFrom = from;
			result.LineTo = to;
		}

		private void ReadSqlInstance(Params result)
		{
			do
			{
				PrintParam("Server (default='')");

				result.SqlInstance = Console.ReadLine().Trim();
				try
				{
					if (string.IsNullOrEmpty(result.SqlInstance))
					{
						result.SqlInstance = DBConnection.DefaultInstance;
					}

					using (DBConnection connection = new DBConnection(result.SqlInstance, "master")) { }
					return;
				}
				catch (Exception)
				{
					Console.WriteLine("Server is unavailable.");
				}
			} while (true);
		}

		private void ReadSqlDatabase(Params result)
		{
			do
			{
				PrintParam("Database");

				result.SqlDatabase = Console.ReadLine().Trim();
				try
				{
					using (DBConnection connection = new DBConnection(result.SqlInstance, result.SqlDatabase)) { }
					return;
				}
				catch (Exception)
				{
					Console.WriteLine("Database is unavailable.");
				}
			} while (true);
		}

		private void ReadSqlTable(Params result)
		{
			PrintParam("Table");
			result.SqlTable = Console.ReadLine().Trim();
		}

		#endregion

		#region Public Methods

		public Params Read()
		{
			Params result = new Params();
			ReadDataFolder(result);
			ReadSeparator(result);
			ReadLineFromTo(result);
			ReadSqlInstance(result);
			ReadSqlDatabase(result);
			ReadSqlTable(result);
			return result;
		}

		#endregion
	}
}
