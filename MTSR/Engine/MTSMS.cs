using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace MTSR
{
	/// <summary>Multiresolution Time Series Management System</summary>
	public abstract class MTSMS<TRepresentation> : IMTSR where TRepresentation : Representation
	{
		private class Buffer
		{
			private readonly Dictionary<Resolution, TimeSeries> collection;
			public Buffer(Resolution[] resolutions) => collection = resolutions.ToDictionary(r => r, r => new TimeSeries());
			public IEnumerable<Resolution> Resolutions => collection.Keys;
			public TimeSeries GetTimeSeries(Resolution resolution) => collection[resolution];
		}

		private readonly ITSDB db;
		private readonly ConcurrentDictionary<int, Buffer> buffers = new ConcurrentDictionary<int, Buffer>();

		protected MTSMS(ITSDB db) => this.db = db;

		protected abstract TRepresentation Aggregate(TimeSeries timeSeries, TimeInterval interval);

		public void Process(int id, TimeSeriesTuple tuple)
		{
			if (!buffers.TryGetValue(id, out Buffer buffer))
			{
				buffers.TryAdd(id, buffer = new Buffer(db.Resolutions));
			}

			var writer = db.CreateWriter();
			foreach (var resolution in buffer.Resolutions)
			{
				var timeseries = buffer.GetTimeSeries(resolution);
				if (timeseries.Count == 0)
				{
					timeseries.Add(tuple);
					continue;
				}

				var last = timeseries.Last;
				timeseries.Add(tuple);

				var interval = resolution.GetInterval(last.Timestamp);
				if (tuple.Timestamp < interval.To)
				{
					continue;
				}

				bool trimmed = false;
				var targetInterval = resolution.GetInterval(tuple.Timestamp);
				while (interval.From < targetInterval.From)
				{
					var representation = Aggregate(timeseries, interval);
					writer.Write(id, resolution, interval.From, representation);
					interval = resolution.GetInterval(interval.To);
					if (!trimmed)
					{
						timeseries.Clear();
						timeseries.Add(last);
						timeseries.Add(tuple);
						trimmed = true;
					}
				}
			}

			writer.Flush();
		}
	}

	/// <summary>(Multiresolution) Piecewise Statistical Approximation</summary>
	public class PSA : MTSMS<DTPSA>
	{
		public const string FullName = "Piecewise Statistical Approximation";
		public PSA(ITSDB db) : base(db) { }
		protected override DTPSA Aggregate(TimeSeries timeseries, TimeInterval interval) => new DTPSA(timeseries.GetTuples(interval).Select(t => t.Value));
	}

	/// <summary>Multiresolution Linear-function-based Piecewise Statistical Approximation</summary>
	public class MLPSA : MTSMS<PLFPSA>
	{
		public const string FullName = "Multiresolution (pieceiwse-linear-function-based) Piecewise Statistical Approximation";
		public MLPSA(ITSDB db) : base(db) { }
		protected override PLFPSA Aggregate(TimeSeries timeseries, TimeInterval interval)
		{
			var result = new PLFPSA();

			TimeSeriesTuple prev = null;
			foreach (var next in timeseries)
			{
				if (prev != null)
				{
					var nextResult = new PLFPSA(new LinearFunction(prev, next), interval.Over(prev.Timestamp, next.Timestamp));
					result = new PLFPSA(result, nextResult);
				}

				prev = next;
			}

			return result;
		}
	}
}
