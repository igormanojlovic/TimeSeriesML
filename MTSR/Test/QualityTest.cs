using System;
using System.Collections.Generic;
using System.IO;

namespace MTSR
{
	public class QualityTest : Test
	{
		public override void Run()
		{
			var representation = new InputRepresentation().Read();
			var inputFolder = new InputDirectory("Input folder with one CSV file per time series").Read();
			var separator = new InputChar("Input CSV column separator").Read();
			var timeIndex = new InputIndex("Input Time column (zero-based index)").Read();
			var valueIndex = new InputIndex("Input Value column (zero-based index)").Read();
			var holidays = new InputDates("Holidays (e.g. 2019-12-31,2020-01-01,...)").Read();
			var outputFolder = Path.Combine(new InputDirectory("Output folder for exporting results").Read(), "ConcatenatedANDLPs");
			var resolutions = new InputResolutions("Time resolutions (e.g. 5,15,30,...minutes)", new Resolution(15)).Read();
			var kMin = new InputCount("Minimum number of clusters", 3, "without considering dataset size").Read();
			var kMax = new InputCount("Maximum number of clusters", 10, "without considering dataset size").Read();

			Console.Write("Press enter to create new time series representations or any other key to cluster the existing ones...");
			bool recreate = Console.ReadKey().Key == ConsoleKey.Enter;
			Console.WriteLine();

			bool export = true;
			if (!recreate)
			{
				Console.Write("Press enter to export the existing representations to CSV or any other key to cluster previously exported data...");
				export = Console.ReadKey().Key == ConsoleKey.Enter;
				Console.WriteLine();
			}

			if (recreate || export)
			{
				if (export)
				{
					Directory.CreateDirectory(outputFolder);
				}

				using (var db = new QualityTestDB(representation, resolutions, recreate))
				{
					if (recreate)
					{
						int id = 0;
						var model = representation.ToInstance(db);
						foreach (var ts in Directory.GetFiles(inputFolder, "*.csv").OrderNaturally())
						{
							id++;
							var dates = new HashSet<DateTime>();

							Log($"Processing {ts}");
							foreach (var tuple in new CSVStream(ts, separator, timeIndex, valueIndex))
							{
								model.Process(id, tuple);
								dates.Add(tuple.Timestamp.Date);
							}

							db.SetTSDates(id, dates);
						}
					}

					if (export)
					{
						foreach (var resolution in resolutions)
						{
							db.ExportConcatenatedANDLPs(outputFolder, resolution, holidays);
						}
					}
				}
			}

			Console.WriteLine();
			R.RunScript($"{nameof(MTSR)}.R.Source.R");
			foreach (var cvi in new string[2] { "DBI", "CHI" })
			{
				R.Run($"LPR('{R.ToRPath(outputFolder)}', {kMin}, {kMax}, 'TSGA', '{cvi}')");
			}
		}
	}
}
