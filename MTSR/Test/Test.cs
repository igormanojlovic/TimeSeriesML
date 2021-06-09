using System;
using System.Diagnostics;

namespace MTSR
{
	public enum TestType
	{
		Speed, Quality
	}

	public static class TestTypeExtensions
	{
		public static Test ToInstance(this TestType t)
		{
			switch (t)
			{
				case TestType.Quality: return new QualityTest();
				default: return new SpeedTest();
			}
		}
	}

	public abstract class Test
	{
		public abstract void Run();
		protected void Log(object text) => Console.WriteLine($"[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}] {text}");
		protected TimeSpan Run(Action action)
		{
			do
			{
				try
				{
					Stopwatch watch = Stopwatch.StartNew();
					action.Invoke();
					return watch.Elapsed;
				}
				catch (Exception e)
				{
					Console.WriteLine(e);
					Console.WriteLine("Press Esc to skip or any other key to retry...");
					if (Console.ReadKey().Key == ConsoleKey.Escape)
					{
						return new TimeSpan(0);
					}

					Console.WriteLine("Retrying...");
				}
			} while (true);
		}
	}
}
