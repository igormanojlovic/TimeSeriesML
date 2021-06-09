using System;
using System.Linq;

namespace MTSR
{
	public abstract class InputReader
	{
		protected const string Default = ".";
	}

	public abstract class InputReader<TResult> : InputReader
	{
		private readonly string text;
		protected InputReader(string text)
		{
			int maxLength = 50;
			int spacesCount = text.Length >= maxLength ? 0 : maxLength - text.Length;
			this.text = $"{text}{string.Concat(Enumerable.Repeat(" ", spacesCount))}: ";
		}

		protected abstract string Hint { get; }
		protected abstract bool TryConvert(string input, out TResult converted);
		public TResult Read()
		{
			while (true)
			{
				Console.Write(text);
				var input = Console.ReadLine().Trim();
				if (TryConvert(input, out TResult converted))
				{
					if (input.Equals(Default))
					{
						Console.WriteLine($"{text}{converted}");
					}

					return converted;
				}

				Console.WriteLine(Hint);
			}
		}
	}
}
