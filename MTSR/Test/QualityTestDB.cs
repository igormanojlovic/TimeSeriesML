using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;

namespace MTSR
{
	public class QualityTestDB : SqlTSDB
	{
		public QualityTestDB(RepresentationType representation, Resolution[] resolutions, bool recreateTable)
			: base(nameof(QualityTestDB), representation, resolutions, recreateTable)
			=> Connection.RunScript($"{nameof(MTSR)}.Test.{nameof(QualityTestDB)}.sql");

		private DataTable GetDates(IEnumerable<DateTime> dates)
		{
			var table = new DataTable("Dates");
			table.Columns.Add("date", typeof(DateTime));
			foreach (var date in dates)
			{
				var row = table.NewRow();
				row["date"] = date;
				table.Rows.Add(row);
			}

			return table;
		}

		public void SetTSDates(int id, HashSet<DateTime> dates)
		{
			using (var cmd = Connection.CreateProcedureCommand(nameof(SetTSDates)))
			{
				cmd.AddParameter(nameof(id), id);
				cmd.AddParameter(nameof(dates), GetDates(dates));
				cmd.ExecuteNonQuery();
			}
		}

		public void ExportConcatenatedANDLPs(string folder, Resolution resolution, IEnumerable<DateTime> holidays)
		{
			string table = GetTable(resolution);

			string[] valueNames;
			var period2day2time2id2values = new SortedList<int, SortedList<int, SortedList<int, SortedList<int, double[]>>>>();
			using (var cmd = Connection.CreateProcedureCommand("GetANDLPs"))
			{
				cmd.AddParameter(nameof(table), table);
				cmd.AddParameter(nameof(resolution), resolution.Step);
				cmd.AddParameter(nameof(holidays), GetDates(holidays));
				using (var reader = cmd.ExecuteReader())
				{
					var valueTypes = Enumerable.Range(4, reader.FieldCount - 4).ToArray();
					valueNames = valueTypes.Select(i => reader.GetName(i)).ToArray();
					while (reader.Read())
					{
						var id = reader.GetInt32(0);
						var period = reader.GetInt32(1);
						var day = reader.GetInt32(2);
						var time = reader.GetInt32(3);
						var values = valueTypes.Select(i => reader.IsDBNull(i) ? 0 : reader.GetDouble(i)).ToArray();
						if (!period2day2time2id2values.TryGetValue(period, out SortedList<int, SortedList<int, SortedList<int, double[]>>> day2time2id2values))
						{
							period2day2time2id2values.Add(period, day2time2id2values = new SortedList<int, SortedList<int, SortedList<int, double[]>>>());
						}
						if (!day2time2id2values.TryGetValue(day, out SortedList<int, SortedList<int, double[]>> time2id2values))
						{
							day2time2id2values.Add(day, time2id2values = new SortedList<int, SortedList<int, double[]>>());
						}
						if (!time2id2values.TryGetValue(time, out SortedList<int, double[]> id2values))
						{
							time2id2values.Add(time, id2values = new SortedList<int, double[]>());
						}

						id2values[id] = values;
					}
				}
			}

			if (period2day2time2id2values.Count == 0)
			{
				return;
			}

			var names = new List<string>();
			var id2concatenated = new SortedList<int, List<double>>();
			for (int valueType = 0; valueType < valueNames.Length; valueType++)
			{
				foreach (var period in period2day2time2id2values)
				{
					foreach (var day in period.Value)
					{
						foreach (var time in day.Value)
						{
							names.Add($"{valueNames[valueType]}_P{period.Key}_D{day.Key}_T{time.Key}");
							foreach (var id in time.Value)
							{
								if (!id2concatenated.TryGetValue(id.Key, out List<double> values))
								{
									id2concatenated.Add(id.Key, values = new List<double>());
								}

								values.Add(id.Value[valueType]);
							}
						}
					}
				}
			}

			var separator = "|";
			StringBuilder b = new StringBuilder();
			b.AppendLine(string.Join(separator, names));
			foreach (var id in id2concatenated)
			{
				b.Append(id.Key);
				b.Append(separator);
				b.AppendLine(string.Join(separator, id.Value));
			}

			string file = $"{table}.csv";
			File.WriteAllText(Path.Combine(folder, file), b.ToString());
		}
	}
}
