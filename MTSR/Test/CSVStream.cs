using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace MTSR
{
	public class CSVStream : IEnumerable<TimeSeriesTuple>
	{
		private readonly string filePath;
		private readonly char separator;
		private readonly int timeIndex, valueIndex;

		public CSVStream(string filePath, char separator, int timeIndex, int valueIndex)
		{
			this.filePath = filePath;
			this.separator = separator;
			this.timeIndex = timeIndex;
			this.valueIndex = valueIndex;
		}

		private IEnumerable<TimeSeriesTuple> Tuples
		{
			get
			{
				using (StreamReader reader = new StreamReader(filePath))
				{
					string line;
					while ((line = reader.ReadLine()) != null)
					{
						var words = line.Split(separator);
						if (timeIndex < words.Length && valueIndex < words.Length)
						{
							var tuple = new TimeSeriesTuple(words[timeIndex].ToTimestamp(), words[valueIndex].ToValue());
							if (tuple.IsValid)
							{
								yield return tuple;
							}
						}
					}
				}
			}
		}

		public IEnumerator<TimeSeriesTuple> GetEnumerator() => Tuples.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => Tuples.GetEnumerator();
	}
}
