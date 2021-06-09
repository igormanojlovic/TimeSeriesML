using System;
using System.Collections.Generic;
using System.Data;

namespace MTSR
{
	public class SqlTSDBWriter : ITSDBWriter
	{
		private readonly SqlTSDB db;
		private readonly Dictionary<Resolution, DataTable> resolution2data = new Dictionary<Resolution, DataTable>();
		public SqlTSDBWriter(SqlTSDB db) => this.db = db;

		public void Write(int id, Resolution resolution, DateTime timestamp, Representation representation)
		{
			if (!resolution2data.TryGetValue(resolution, out DataTable data))
			{
				resolution2data.Add(resolution, data = new DataTable(nameof(Representation)));
				data.Columns.Add(nameof(id), id.GetType());
				data.Columns.Add(nameof(timestamp), timestamp.GetType());
				ValueTypes.All.ForEach(t => data.Columns.Add(t.ToString(), typeof(double)));
			}

			var row = data.NewRow();
			row[nameof(id)] = id;
			row[nameof(timestamp)] = timestamp;
			representation.ForEach(v => row[v.Type.ToString()] = v.Value.IsValid() ? (object)v.Value : DBNull.Value);
			data.Rows.Add(row);
		}

		public void Flush()
		{
			foreach (var data in resolution2data)
			{
				string table = db.GetTable(data.Key);
				using (var cmd = db.Connection.CreateProcedureCommand(nameof(Flush)))
				{
					cmd.AddParameter(nameof(table), table);
					cmd.AddParameter(data.Value.TableName, data.Value);
					cmd.ExecuteNonQuery();
				}
			}
		}
	}
}
