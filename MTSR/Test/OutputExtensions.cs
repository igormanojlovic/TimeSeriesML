using System;

namespace MTSR
{
	public static class OutputExtensions
	{
		public static string ToText(this double number) => number.ToString("0.00");
		public static string ToText(this TimeSpan span)
		{
			if (span.TotalMilliseconds < 1000)
			{
				return $"{span.TotalMilliseconds.ToText()} milliseconds";
			}
			if (span.TotalSeconds < 60)
			{
				return $"{span.TotalSeconds.ToText()} seconds";
			}
			if (span.TotalMinutes < 60)
			{
				return $"{span.TotalMinutes.ToText()} minutes";
			}
			if (span.TotalHours < 24)
			{
				return $"{span.TotalHours.ToText()} hours";
			}

			return $"{span.TotalDays.ToText()} days";
		}
	}
}
