using System;

namespace MTSR
{
	public class InputTime : InputReader<DateTime>
	{
		private readonly DateTime defaultValue;
		private readonly string description;
		public InputTime(string text, DateTime defaultValue, string description) : base(text)
		{
			this.defaultValue = defaultValue;
			this.description = description;
		}

		protected override string Hint => $"Please specify time in 'yyyy-MM-dd HH:mm:ss' format or type '{Default}' to use {description} by default ({defaultValue})...";
		protected override bool TryConvert(string input, out DateTime converted)
		{
			if (input.Equals(Default))
			{
				converted = defaultValue.Date;
				return true;
			}
			if (!DateTime.TryParse(input, out converted))
			{
				return false;
			}

			converted = new DateTime(converted.Year, converted.Month, converted.Day, converted.Hour, converted.Minute, converted.Second, DateTimeKind.Utc);
			return true;
		}
	}
}
