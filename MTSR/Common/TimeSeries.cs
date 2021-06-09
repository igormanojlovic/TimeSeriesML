using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MTSR
{
	public class TimeSeries : IEnumerable<TimeSeriesTuple>
	{
		public TimeSeriesTuple First { get; private set; }
		public TimeSeriesTuple Last { get; private set; }
		public long Count { get; private set; }

		private IEnumerable<TimeSeriesTuple> Tuples
		{
			get
			{
				var t = First;
				while (t != null)
				{
					yield return t;
					t = t.Next;
				}
			}
		}

		public void Add(TimeSeriesTuple tuple)
		{
			if (First == null)
			{
				Last = First = tuple;
			}
			else
			{
				Last = Last.Next = tuple;
			}

			Count++;
		}

		public IEnumerable<TimeSeriesTuple> GetTuples(TimeInterval interval)
			=> Tuples.Where(t => t.Timestamp >= interval.From && t.Timestamp < interval.To);

		public IEnumerator<TimeSeriesTuple> GetEnumerator() => Tuples.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => Tuples.GetEnumerator();
		public void Clear()
		{
			Last = First = null;
			Count = 0;
		}
	}
}
