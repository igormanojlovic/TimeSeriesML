namespace MTSR
{
	public class InputChar : InputReader<char>
	{
		public InputChar(string text) : base(text) { }
		protected override string Hint => $"Please specify one character...";
		protected override bool TryConvert(string input, out char converted) => char.TryParse(input, out converted);
	}
}
