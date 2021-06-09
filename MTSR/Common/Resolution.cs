using System;
using System.Collections.Generic;
using System.Linq;

namespace MTSR
{
	public class Resolution
	{
		private static readonly long TicksPerMinute = TimeSpan.FromMinutes(1).Ticks;

		public Resolution(int step)
		{
			Step = step;
			Span = TimeSpan.FromMinutes(Step);
		}

		public int Step { get; }
		public TimeSpan Span { get; }

		public TimeInterval GetInterval(DateTime time)
		{
			int stepsCount = time.Minute / Step;
			DateTime start = new DateTime(time.Year, time.Month, time.Day, time.Hour, Step * stepsCount, 0, time.Kind);
			DateTime end = new DateTime(start.Ticks + Step * TicksPerMinute);
			return new TimeInterval(start, end);
		}

		public IEnumerable<TimeInterval> GetIntervals(TimeInterval range)
		{
			DateTime from = range.From;
			while (from < range.To)
			{
				var interval = GetInterval(from);
				yield return interval;
				from = interval.To;
			}
		}

		public override int GetHashCode() => Step;
		public override bool Equals(object obj) => (obj as Resolution)?.Step == Step;
		public override string ToString() => $"{Step}min";
	}

	public static class ResolutionExtensions
	{
		public static Resolution Root(this IEnumerable<Resolution> resolutions) => new Resolution(resolutions.Select(r => r.Step).GCD());
		public static Resolution Root(this Resolution[] resolutions, out Dictionary<Resolution, Resolution[]> hierarchy)
		{
			var root = resolutions.Root();
			hierarchy = new Dictionary<Resolution, Resolution[]>();

			var descendants = resolutions.ToHashSet();
			descendants.Remove(root);
			foreach (var parent in resolutions.Where(r => !r.Equals(root)).OrderByDescending(r => r.Step))
			{
				var children = descendants.Where(d => d.Step > parent.Step && d.Step % parent.Step == 0).ToArray();
				if (children.Length > 0)
				{
					descendants.ExceptWith(hierarchy[parent] = children);
				}
			}

			hierarchy[root] = descendants.ToArray();
			return root;
		}

		public static IEnumerable<Resolution> GetChildren(this Dictionary<Resolution, Resolution[]> hierarchy, Resolution parent)
			=> hierarchy.TryGetValue(parent, out Resolution[] children) ? children : new Resolution[0];
	}
}
