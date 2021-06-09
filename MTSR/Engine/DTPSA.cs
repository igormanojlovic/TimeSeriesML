using System;
using System.Collections.Generic;

namespace MTSR
{
	/// <summary>One piece of Discrete-Time-based Piecewise Statistical Approximation</summary>
	public class DTPSA : Representation
	{
		private readonly double average, standardDeviation;

		public DTPSA(IEnumerable<double> values)
		{
			double sum = 0;
			double count = 0;
			foreach (var value in values)
			{
				sum += value;
				count++;
			}

			if (count == 0)
			{
				return;
			}

			average = sum / count;

			if (count == 1)
			{
				return;
			}

			double deviation2 = 0;
			double deviation3 = 0;
			foreach (var value in values)
			{
				var deviation = value - average;
				deviation2 += deviation.Square();
				deviation3 += deviation2 * deviation;
			}

			standardDeviation = Math.Sqrt(deviation2 / (count - 1));
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
