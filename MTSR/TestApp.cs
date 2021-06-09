using System;

namespace MTSR
{
	public class TestApp
	{
		public static void Main(string[] args)
		{
			while (true)
			{
				try
				{
					Console.WriteLine($"Please provide requested input or press Enter for more details...");
					var test = new InputEnum<TestType>("Test", TestType.Speed).Read().ToInstance();
					test.Run();
					Console.WriteLine("Test finished.");
				}
				catch (Exception e)
				{
					Console.WriteLine($"Unexpected error: {e}");
				}

				Console.WriteLine("Press any key to try again...");
				Console.ReadKey();
			}
		}
	}
}
