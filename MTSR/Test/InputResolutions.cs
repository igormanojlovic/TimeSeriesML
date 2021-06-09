using System;
using System.Linq;

namespace MTSR
{
	public class InputResolutions : InputReader<Resolution[]>
	{
		private readonly Resolution defaultValue;
		public InputResolutions(string text, Resolution defaultValue) : base(text) => this.defaultValue = defaultValue;

		protected override string Hint => $"Please specify resolution steps as comma-separated integers greater than 0 or type '{Default}' to use {defaultValue} by default...";
		protected override bool TryConvert(string input, out Resolution[] converted)
		{
			if (input.Equals(Default))
			{
				converted = new Resolution[1] { new Resolution(15) };
				return true;
			}

			try
			{
				converted = input.Split(',').Select(step => new Resolution(int.Parse(step))).ToHashSet().ToArray();
				return converted.Length > 0;
			}
			catch (Exception)
			{
				converted = new Resolution[0];
				return false;
			}
		}
	}
}
