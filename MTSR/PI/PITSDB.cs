using OSIsoft.AF.Asset;
using OSIsoft.AF.PI;
using System.Collections.Generic;

namespace MTSR
{
	public class PITSDB : ITSDB
	{
		#region Fields

		private readonly Dictionary<int, Dictionary<Resolution, Dictionary<ValueType, AFAttribute>>> db;

		#endregion

		#region Constructors

		public PITSDB(int[] sourceIDs, Resolution[] targetResolutions)
		{
			Resolutions = targetResolutions;
			db = new Dictionary<int, Dictionary<Resolution, Dictionary<ValueType, AFAttribute>>>(sourceIDs.Length);
			using (var connection = new PIConnection())
			{
				var template = PrepareTemplate(connection, targetResolutions);
				LoadTimeSeries(connection, template, sourceIDs, targetResolutions);
			}
		}

		#endregion

		#region Properties

		public Resolution[] Resolutions { get; }

		#endregion

		#region Methods

		private AFElementTemplate PrepareTemplate(PIConnection connection, Resolution[] resolutions)
		{
			connection.ClearTemplates();

			var template = connection.AddTemplate(nameof(PITSDB));
			foreach (var resolution in resolutions)
			{
				foreach (var valueType in ValueTypes.All)
				{
					template.AddTimeSeries(resolution, valueType);
				}
			}

			template.CheckIn();
			return template;
		}

		private void LoadTimeSeries(PIConnection connection, AFElementTemplate template, int[] sourceIDs, Resolution[] resolutions)
		{
			foreach (int id in sourceIDs)
			{
				template.AddElement(id);
				foreach (var resolution in resolutions)
				{
					foreach (var valueType in ValueTypes.All)
					{
						var name = template.GetTimeSeriesName(id, resolution, valueType);
						var timeseries = connection.AddOrGetTimeSeries(name, PIPointType.Float32);
						AddTimeSeries(id, resolution, valueType, timeseries);
					}
				}
			}
		}

		private void AddTimeSeries(int id, Resolution resolution, ValueType valueType, AFAttribute timeseries)
		{
			if (!db.TryGetValue(id, out Dictionary<Resolution, Dictionary<ValueType, AFAttribute>> resolution2timeseries))
			{
				db.Add(id, resolution2timeseries = new Dictionary<Resolution, Dictionary<ValueType, AFAttribute>>());
			}
			if (!resolution2timeseries.TryGetValue(resolution, out Dictionary<ValueType, AFAttribute> valueType2timeseries))
			{
				resolution2timeseries.Add(resolution, valueType2timeseries = new Dictionary<ValueType, AFAttribute>());
			}

			valueType2timeseries[valueType] = timeseries;
		}

		public AFAttribute GetTimeSeries(int id, Resolution resolution, ValueType valueType) => db[id][resolution][valueType];
		public ITSDBWriter CreateWriter() => new PITSDBWriter(this);

		#endregion
	}
}
