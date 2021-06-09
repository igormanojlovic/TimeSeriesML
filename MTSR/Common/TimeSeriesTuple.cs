using System;

namespace MTSR
{
	public class TimeSeriesTuple
	{
		public TimeSeriesTuple(DateTime timestamp, double value)
		{
			Timestamp = timestamp;
			Value = value;
		}

		public DateTime Timestamp { get; }
		public double Value { get; set; }
		public bool IsValid => Timestamp.IsValid() && Value.IsValid();

		/// <summary>
		/// This property is used to efficiently construct TimeSeries collection
		/// based on assumption that one tuple can belong to only one time series.
		/// </summary>
		public TimeSeriesTuple Next { get; set; }
	}
}
