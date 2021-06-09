namespace MTSR
{
	public class InputIndex : InputReader<int>
	{
		public InputIndex(string text) : base(text) { }
		protected override string Hint => $"Please specify integer equal or greater than 0...";
		protected override bool TryConvert(string input, out int converted) => int.TryParse(input, out converted) && converted >= 0;
	}
}
