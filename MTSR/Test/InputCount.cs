namespace MTSR
{
	public class InputCount : InputReader<int>
	{
		private readonly int defaultValue;
		private readonly string description;
		public InputCount(string text, int defaultValue, string description) : base(text)
		{
			this.defaultValue = defaultValue;
			this.description = description;
		}

		protected override string Hint => $"Please specify integer greater than 0 or type '{Default}' to use {description} by default ({defaultValue})...";
		protected override bool TryConvert(string input, out int converted)
		{
			if (input.Equals(Default))
			{
				converted = defaultValue;
				return true;
			}

			return int.TryParse(input, out converted) && converted > 0;
		}
	}
}
