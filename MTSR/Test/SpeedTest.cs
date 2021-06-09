using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MTSR
{
	public class SpeedTest : Test
	{
		#region Param

		public class Param
		{
			public RepresentationType Representation { get; set; } = RepresentationType.HMLPSA;
			public int[] TimeSeries { get; set; } = new int[0];
			public TimeInterval TimeSeriesRange { get; } = new TimeInterval();
			public Resolution[] Resolutions { get; set; } = new Resolution[0];
			public string OutputFolder { get; set; }
			public string OutputFile => Path.Combine(OutputFolder, $"{Representation}-{string.Join("-", Resolutions.Select(s => s.ToString()))}.csv");
			public IMTSR CreateModel() => Representation.ToInstance(new PITSDB(TimeSeries, Resolutions));
		}

		#endregion

		#region Private Methods

		private bool Clean()
		{
			Console.Write("Press enter to remove existing points from PI server or any other key to continue...");
			bool remove = Console.ReadKey().Key == ConsoleKey.Enter;
			Console.WriteLine();
			if (!remove)
			{
				return false;
			}

			using (var db = new PIConnection())
			{
				int deleted = db.ClearTimeSeries();
				if (deleted == 0)
				{
					return false;
				}

				Console.WriteLine($"Successfully deleted {deleted} time series from PI server but for the action to take effect on licence limits you must restart the system.");
				Console.Write("Press esc to exit or any other key to continue...");
				return Console.ReadKey().Key == ConsoleKey.Escape;
			}
		}

		private Param Setup()
		{
			var param = new Param();
			param.Representation = new InputRepresentation().Read();
			param.TimeSeriesRange.From = new InputTime("Simulation start time", DateTime.UtcNow.Date.AddDays(-1), "yesterday in UTC").Read();
			param.TimeSeriesRange.To = new InputTime("Simulation end time", param.TimeSeriesRange.From.AddDays(1), "one day after start time in UTC").Read();
			param.Resolutions = new InputResolutions("Time resolutions (e.g. 1,2,3,...minutes)", new Resolution(15)).Read();
			param.TimeSeries = Enumerable.Range(0, new InputCount("Time series count", 10000 / param.Resolutions.Length / Representations.All.Count(), "trial PI licence limit divided by the number of resolutions and representation value types.").Read()).ToArray();
			param.OutputFolder = Path.Combine(new InputDirectory("Output folder for exporting results").Read(), "Speed");
			return param;
		}

		private void Start(Param param)
		{
			Log("Initializing model...");
			IMTSR model = param.CreateModel();
			Directory.CreateDirectory(param.OutputFolder);
			Log("Initialization finished...");
			Log("Speed test started...");

			int prevHour = 0;
			int prevSecond = 0;
			int valuesPerSecond = 0;
			var random = new Random();
			TimeSpan duration = TimeSpan.Zero;
			using (StreamWriter writer = new StreamWriter(param.OutputFile, true))
			{
				while (duration < param.TimeSeriesRange.Span)
				{
					var timestamp = param.TimeSeriesRange.From + duration;
					var tuples = param.TimeSeries.Select(ts => new TimeSeriesTuple(timestamp, random.NextDouble() * 1000)).ToArray();

					duration += Run(delegate
					{
						Parallel.ForEach(param.TimeSeries, id =>
						{
							model.Process(id, tuples[id]);
						});
					});

					while (prevSecond < (int)duration.TotalSeconds)
					{
						writer.WriteLine(valuesPerSecond);
						Run(delegate { writer.Flush(); });
						valuesPerSecond = 0;
						prevSecond++;
					}

					if ((int)duration.TotalHours > prevHour)
					{
						Log($"Processed {duration.ToText()} of data...");
						prevHour = (int)duration.TotalHours;
					}

					valuesPerSecond += param.TimeSeries.Length;
					prevSecond = (int)duration.TotalSeconds;
				}
			}
		}

		#endregion

		#region Public Methods

		public override void Run()
		{
			if (!Clean())
			{
				Start(Setup());
			}
		}

		#endregion
	}
}
