using RDotNet;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace MTSR
{
	public static class R
	{
		private static readonly Lazy<REngine> engine = new Lazy<REngine>(() => GetEngine());

		private static REngine GetEngine()
		{
			string rHome = Directory.GetDirectories(@"C:\Program Files\R\").OrderNaturally().Last();
			string rBin = Path.Combine(rHome, @"bin\x64");
			string rDll = Path.Combine(rBin, @"R.dll");
			REngine.SetEnvironmentVariables(rBin, rHome);
			return REngine.GetInstance(rDll, true, new StartupParameter()
			{
				RHome = rHome,
				Interactive = true,
				Quiet = true,
			});
		}

		public static DataFrame Run(string cmd) => engine.Value.Evaluate(cmd).AsDataFrame();
		public static DataFrame RunScript(string filePath) => Run(Assembly.GetExecutingAssembly().GetText(filePath));
		public static string ToRPath(string path) => path.Replace("\\", "\\\\");
	}
}
