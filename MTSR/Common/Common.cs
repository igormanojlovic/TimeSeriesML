using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;

namespace MTSR
{
	[SuppressUnmanagedCodeSecurity]
	internal static class SafeNativeMethods
	{
		[DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
		public static extern int StrCmpLogicalW(string psz1, string psz2);
	}

	public sealed class NaturalStringComparer : IComparer<string>
	{
		public int Compare(string a, string b)
		{
			return SafeNativeMethods.StrCmpLogicalW(a, b);
		}
	}

	public static class Common
	{
		#region Private Methods

		private static readonly DateTime UnixStartTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
		private static DateTime ConvertUnix(double msec) => UnixStartTime + TimeSpan.FromMilliseconds(msec);
		private static int GCD(int a, int b)
		{
			int remainder;
			while (b != 0)
			{
				remainder = a % b;
				a = b;
				b = remainder;
			}

			return a;
		}

		#endregion

		#region Public Methods

		public static IEnumerable<TEnum> GetValues<TEnum>(this Type type) where TEnum : struct
			=> Enum.GetValues(typeof(TEnum)).Cast<TEnum>().ToArray();

		public static string GetText(this Assembly assembly, string filePath)
		{
			using (StreamReader reader = new StreamReader(assembly.GetManifestResourceStream(filePath)))
			{
				return reader.ReadToEnd();
			}
		}

		public static IEnumerable<string> OrderNaturally(this IEnumerable<string> strings)
			=> strings.OrderBy(s => s, new NaturalStringComparer());

		public static int GCD(this IEnumerable<int> values)
		{
			int? gcd = null;
			foreach (int value in values)
			{
				gcd = gcd.HasValue ? GCD(gcd.Value, value) : value;
			}

			return gcd ?? 0;
		}

		public static DateTime ToTimestamp(this object timestamp)
		{
			if (timestamp is DateTime)
			{
				return (DateTime)timestamp;
			}
			if (timestamp is double)
			{
				return ConvertUnix((double)timestamp);
			}
			if (timestamp is string)
			{
				var s = (string)timestamp;
				if (DateTime.TryParse(s, out DateTime t))
				{
					return t;
				}
				if (double.TryParse(s, out double u))
				{
					return ConvertUnix(u);
				}
			}

			try
			{
				return Convert.ToDateTime(timestamp);
			}
			catch
			{
				return DateTime.MinValue;
			}
		}

		public static double ToValue(this object value)
		{
			if (value is double)
			{
				return (double)value;
			}
			if (value is string)
			{
				if (double.TryParse((string)value, out double d))
				{
					return d;
				}
			}

			try
			{
				return Convert.ToDouble(value);
			}
			catch
			{
				return double.NaN;
			}
		}

		public static bool IsValid(this DateTime timestamp) => timestamp > DateTime.MinValue && timestamp < DateTime.MaxValue;
		public static bool IsValid(this double value) => !double.IsNaN(value) && !double.IsInfinity(value);
		public static double Square(this double x) => x * x;

		public static void ForEach<T>(this IEnumerable<T> items, Action<T> action)
		{
			foreach (T item in items)
			{
				action(item);
			}
		}

		#endregion
	}
}
