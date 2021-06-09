using System;

namespace MTSR
{
	public class LinearFunction
	{
		private readonly double slope, intercept;
		private readonly long xOffset;

		public LinearFunction(TimeSeriesTuple xy1, TimeSeriesTuple xy2)
			: this(0, xy1.Value, xy2.Timestamp.Ticks - xy1.Timestamp.Ticks, xy2.Value, xy1.Timestamp.Ticks)
		{ }

		public LinearFunction(double x1, double y1, double x2, double y2) : this(x1, y1, x2, y2, 0) { }

		private LinearFunction(double x1, double y1, double x2, double y2, long xOffset)
		{
			X1 = x1;
			Y1 = y1;
			X2 = x2;
			Y2 = y2;
			this.xOffset = xOffset;
			slope = (y2 - y1) / (x2 - x1);
			intercept = y1 - slope * x1;
		}

		public double X1 { get; }
		public double Y1 { get; }
		public double X2 { get; }
		public double Y2 { get; }

		public double ToX(DateTime timestamp) => timestamp.Ticks - xOffset;
		public double Integral(double x) => slope * x.Square() / 2 + intercept * x;
		public double Integral(double xFrom, double xTo) => Integral(xTo) - Integral(xFrom);
	}
}
