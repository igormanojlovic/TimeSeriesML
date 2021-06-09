using System;
using System.Collections.Generic;

namespace MTSR
{
	/// <summary>One piece of Piecewise-Linear-Function-based Piecewise Statistical Approximation</summary>
	public class PLFPSA : Representation
	{
		private readonly double duration, average, standardDeviation;

		public PLFPSA() { }

		public PLFPSA(LinearFunction f, TimeInterval interval)
		{
			var xFrom = f.ToX(interval.From);
			var xTo = f.ToX(interval.To);

			duration = xTo - xFrom;
			average = f.Integral(xFrom, xTo) / duration;
			var totalVariance = new LinearFunction(f.X1, (f.Y1 - average).Square(), f.X2, (f.Y2 - average).Square()).Integral(xFrom, xTo);
			standardDeviation = Math.Sqrt(totalVariance / duration);
		}

		public PLFPSA(PLFPSA a, PLFPSA b)
		{
			duration = a.duration + b.duration;
			average = (a.duration * a.average + b.duration * b.average) / duration;
			var totalVariance = a.duration * (a.standardDeviation.Square() + (a.average - average).Square()) +
								b.duration * (b.standardDeviation.Square() + (b.average - average).Square());
			standardDeviation = Math.Sqrt(totalVariance / duration);
		}

		protected override IEnumerable<CalculatedValue> Values
		{
			get
			{
				yield return new CalculatedValue(ValueType.Average, average);
				yield return new CalculatedValue(ValueType.StandardDeviation, standardDeviation);
			}
		}
	}
}
