using System.Linq;

namespace MTSR
{
	public class SqlTSDB : SqlDB, ITSDB
	{
		private readonly RepresentationType representation;
		public SqlTSDB(string name, RepresentationType representation, Resolution[] resolutions, bool recreateTables) : base(name, false)
		{
			this.representation = representation;
			Resolutions = resolutions;

			var valueColumns = string.Join(", ", ValueTypes.All.Select(s => $"[{s}] FLOAT"));
			Connection.Run($"CREATE TYPE[{nameof(Representation)}] AS TABLE([ID] INT, [Timestamp] DATETIME2, {valueColumns})");
			Connection.RunScript($"{nameof(MTSR)}.SQL.{nameof(SqlTSDB)}.sql");
			if (recreateTables)
			{
				resolutions.Select(r => GetTable(r)).ForEach(t => Connection.Run($"IF OBJECT_ID('{t}') IS NOT NULL DROP TABLE [{t}]"));
			}
		}

		public Resolution[] Resolutions { get; }
		public string GetTable(Resolution resolution) => $"{representation}-{resolution}";
		public ITSDBWriter CreateWriter() => new SqlTSDBWriter(this);
	}
}
