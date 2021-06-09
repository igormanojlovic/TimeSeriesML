using System.Data;
using System.Data.SqlClient;

namespace MTSR
{
	public static class SqlDBExtensions
	{
		private static string CreateParamName(string paramName) => $"@{paramName}";

		public static void AddParameter(this SqlCommand cmd, string name, object value)
			=> cmd.Parameters.AddWithValue(CreateParamName(name), value);

		public static void AddParameter(this SqlCommand cmd, string name, DataTable values)
			=> cmd.Parameters.Add(new SqlParameter(CreateParamName(name), SqlDbType.Structured)
			{ Value = values, TypeName = values.TableName });
	}
}
