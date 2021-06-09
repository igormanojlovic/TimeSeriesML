using System;
using System.Linq;

namespace MTSR
{
	public class InputDates : InputReader<DateTime[]>
	{
		public InputDates(string text) : base(text) { }

		protected override string Hint => $"Please specify comma-separated dates in 'yyyy-MM-dd' format or type '{Default}' to skip...";
		protected override bool TryConvert(string input, out DateTime[] converted)
		{
			if (input.Equals(Default))
			{
				converted = new DateTime[0];
				return true;
			}

			try
			{
				converted = input.Split(',').Select(h => DateTime.Parse(h).Date).ToArray();
				return true;
			}
			catch (Exception)
			{
				converted = new DateTime[0];
				return false;
			}
		}
	}
}
