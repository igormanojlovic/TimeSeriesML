using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;

namespace CSV2DB
{
	public class Importer
	{
		#region Private Methods

		private IEnumerable<string> GetColumns(int columnFrom, int columnTo)
		{
			for (int i = columnFrom; i <= columnTo; i++)
			{
				yield return $"[c{i}]";
			}
		}

		private IEnumerable<string> GetColumnDefinitions(int columnFrom, int columnTo)
			=> GetColumns(columnFrom, columnTo).Select(c => $"{c} VARCHAR(MAX)");

		private string GetColumnNames(int columnsCount)
			=> string.Join(",", GetColumns(0, columnsCount - 1));

		private IEnumerable<SqlParameter> GetColumnParams(List<string> columnValues)
		{
			int i = 0;
			foreach (string column in GetColumns(0, columnValues.Count - 1))
			{
				yield return new SqlParameter($"@c{i}", columnValues[i]);
				i++;
			}
		}

		private string GetColumnParamNames(List<string> columnValues)
			=> string.Join(",", GetColumnParams(columnValues).Select(p => p.ParameterName));

		private string GetCreateCMD(string table, int columnsCount)
			=> $"CREATE TABLE {table} ({string.Join(",", GetColumnDefinitions(0, columnsCount - 1))});";

		private string GetAlterCMD(string table, int oldColumnsCount, int newColumnsCount)
			=> string.Join("", GetColumnDefinitions(oldColumnsCount, newColumnsCount - 1).Select(c => $"ALTER TABLE {table} ADD {c};"));

		private string GetInsertCMD(string table, List<string> columnValues)
			=> $"INSERT {table} ({GetColumnNames(columnValues.Count)}) VALUES ({GetColumnParamNames(columnValues)});";

		#endregion

		#region Public Methods

		public void Import(Params p)
		{
			int columnsCount = 0;
			using (DBConnection connection = new DBConnection(p.SqlInstance, p.SqlDatabase))
			{
				foreach (string file in Directory.GetFiles(p.DataFolder, "*", SearchOption.AllDirectories))
				{
					Console.Write($"Importing {file}...");
					using (StreamReader reader = new StreamReader(file))
					{
						int lineNumber = 0;
						while (reader.ReadLine() != null)
						{
							lineNumber++;
						}

						Console.WriteLine($"(file has {lineNumber} lines)...");
					}

					using (StreamReader reader = new StreamReader(file))
					{
						string line;
						int lineNumber = 0;
						while ((line = reader.ReadLine()) != null)
						{
							lineNumber++;
							if (lineNumber < p.LineFrom)
							{
								continue;
							}
							if (lineNumber > p.LineTo)
							{
								break;
							}

							string[] words = line.Split(p.CSVSeparator);
							var columnValues = new List<string>(words.Length + 2);
							columnValues.Add(file);
							columnValues.Add(lineNumber.ToString());
							columnValues.AddRange(words);

							while (true)
							{
								try
								{
									using (DBCommand cmd = connection.CreateCommand())
									{
										if (columnsCount == 0)
										{
											cmd.CommandText = GetCreateCMD(p.SqlTable, columnsCount = columnValues.Count);
											cmd.Execute();
										}
										else if (columnsCount < columnValues.Count)
										{
											cmd.CommandText = GetAlterCMD(p.SqlTable, columnsCount, columnsCount = columnValues.Count);
											cmd.Execute();
										}

										cmd.CommandText = GetInsertCMD(p.SqlTable, columnValues);
										foreach (SqlParameter sp in GetColumnParams(columnValues))
										{
											cmd.AddParameter(sp.ParameterName, sp.Value);
										}

										cmd.Execute();
									}

									break;
								}
								catch (Exception e)
								{
									Console.WriteLine($"Failed to import line {lineNumber} from {file}: {e.Message}");
									Console.WriteLine("Press any key to retry...");
									Console.ReadKey();
								}
							}
						}
					}
				}
			}

			Console.WriteLine($"finished.");
		}

		#endregion
	}
}
