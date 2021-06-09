using System.IO;

namespace MTSR
{
	public class InputDirectory : InputReader<string>
	{
		public InputDirectory(string text) : base(text) { }
		protected override string Hint => $"Please specify path to existing and accessible folder...";
		protected override bool TryConvert(string input, out string converted) => Directory.Exists(converted = input);
	}
}
