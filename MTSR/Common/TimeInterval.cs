using System;

namespace MTSR
{
	public class TimeInterval
	{
		public TimeInterval() : this(DateTime.UtcNow, DateTime.UtcNow) { }
		public TimeInterval(DateTime from, DateTime to)
		{
			From = from;
			To = to;
		}

		public DateTime From { get; set; }
		public DateTime To { get; set; }
		public TimeSpan Span => To - From;
		public TimeInterval Over(TimeInterval interval) => Over(interval.From, interval.To);
		public TimeInterval Over(DateTime from, DateTime to) => new TimeInterval(from > From ? from : From, to < To ? to : To);
	}
}
