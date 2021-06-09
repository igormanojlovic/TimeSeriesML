using OSIsoft.AF;
using OSIsoft.AF.Asset;
using OSIsoft.AF.Data;
using System;
using System.Collections.Generic;

namespace MTSR
{
	public class PITSDBWriter : ITSDBWriter
	{
		private readonly PITSDB db;
		private List<AFValue> tuples;

		public PITSDBWriter(PITSDB db) => this.db = db;

		private IEnumerable<string> GetErrors(AFErrors<AFValue> outcome)
		{
			foreach (var e in outcome.Errors)
			{
				yield return $"Failed writing to PI ({e.Key.PIPoint}, {e.Key.Timestamp}, {e.Key.Value}). {e.Value}";
			}
			foreach (var e in outcome.PIServerErrors)
			{
				yield return $"Failed writing to PI server. {e.Value}";
			}
			foreach (var e in outcome.PISystemErrors)
			{
				yield return $"Failed writing to PI system. {e.Value}";
			}
		}

		public void Write(int id, Resolution resolution, DateTime timestamp, Representation representation)
		{
			if (tuples == null)
			{
				tuples = new List<AFValue>(1);
			}

			representation.ForEach(v => tuples.Add(new AFValue(db.GetTimeSeries(id, resolution, v.Type), v.Value, timestamp)));
		}

		public void Flush()
		{
			if (tuples?.Count > 0)
			{
				var outcome = AFListData.UpdateValues(tuples.ToArray(), AFUpdateOption.Replace, AFBufferOption.Buffer);
				if (outcome != null && outcome.HasErrors)
				{
					throw new Exception(string.Join("\n", GetErrors(outcome)));
				}
			}
		}
	}
}
