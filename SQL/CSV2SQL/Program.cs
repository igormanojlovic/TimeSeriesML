using System;

namespace CSV2DB
{
	class Program
	{
		static void Main(string[] args)
		{
			do
			{
				try
				{
					ParamReader reader = new ParamReader();
					Importer importer = new Importer();
					importer.Import(reader.Read());
				}
				catch (Exception e)
				{
					Console.WriteLine(e);
				}
			} while (true);
		}
	}
}
