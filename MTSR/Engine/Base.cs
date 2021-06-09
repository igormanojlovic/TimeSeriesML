using System.Collections;
using System.Collections.Generic;

namespace MTSR
{
	public enum ValueType { Average, StandardDeviation }
	public enum RepresentationType { HMLPSA, MLPSA, PSA }

	public abstract class ValueTypes
	{
		static ValueTypes() => All = typeof(ValueType).GetValues<ValueType>();
		public static IEnumerable<ValueType> All { get; private set; }
	}

	public class CalculatedValue
	{
		public CalculatedValue(ValueType type, double value)
		{
			Type = type;
			Value = value;
		}

		public ValueType Type { get; }
		public double Value { get; }
	}

	public abstract class Representation : IEnumerable<CalculatedValue>
	{
		protected abstract IEnumerable<CalculatedValue> Values { get; }
		public IEnumerator<CalculatedValue> GetEnumerator() => Values.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => Values.GetEnumerator();
	}

	public static class Representations
	{
		static Representations() => All = typeof(RepresentationType).GetValues<RepresentationType>();
		public static IEnumerable<RepresentationType> All { get; private set; }

		public static IMTSR ToInstance(this RepresentationType representation, ITSDB db)
		{
			switch (representation)
			{
				case RepresentationType.MLPSA: return new MLPSA(db);
				case RepresentationType.PSA: return new PSA(db);
				default: return new HMLPSA(db);
			}
		}

		public static string GetFullName(this RepresentationType representation)
		{
			switch (representation)
			{
				case RepresentationType.MLPSA: return MLPSA.FullName;
				case RepresentationType.PSA: return PSA.FullName;
				default: return HMLPSA.FullName;
			}
		}
	}
}
