using System;
using System.Linq;

namespace MTSR
{
	public class InputEnum<TEnum> : InputReader<TEnum> where TEnum : struct
	{
		private readonly TEnum defaultValue;
		private static string AvailableValues = string.Join("/", Enum.GetNames(typeof(TEnum)));
		public InputEnum(string text, TEnum defaultValue) : base($"{text} ({AvailableValues})") => this.defaultValue = defaultValue;
		protected override string Hint => $"Please choose one of available values ({AvailableValues}) or type '{Default}' to use {defaultValue} by default...";
		protected override bool TryConvert(string input, out TEnum converted)
		{
			if (input.Equals(Default))
			{
				converted = defaultValue;
				return true;
			}

			return Enum.TryParse(input, true, out converted);
		}
	}

	public class InputRepresentation : InputEnum<RepresentationType>
	{
		public InputRepresentation() : base(nameof(Representation), RepresentationType.HMLPSA) { }
		protected override string Hint => $"{base.Hint}\n{string.Join("\n", Representations.All.Select(r => $"- {r}: {r.GetFullName()}"))}";
	}
}
