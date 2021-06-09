using OSIsoft.AF;
using OSIsoft.AF.Asset;
using OSIsoft.AF.PI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MTSR
{
	/// <summary>PI Database Connection</summary>
	/// <summary>
	/// Database size can be obtained with administrative tools (C:\Program Files\PI\adm\) in the following manner:
	/// 1) Either shut down PI system or unregister PI archive (piartool -au "C:\Program Files\PI\arc\ArchiveFileName.arc").
	/// 2) Inspect PI archive (pidiag -archk "C:\Program Files\PI\arc\ArchiveFileName.arc").
	/// 3) Note the total number of records at the end of text (size in KB).
	/// </summary>
	public class PIConnection : IDisposable
	{
		#region Fields

		private readonly AFDatabase database;
		private readonly PIServer server;

		#endregion

		#region Constructors

		public PIConnection()
		{
			database = new PISystems().DefaultPISystem.Databases.DefaultDatabase;
			server = PIServers.GetPIServers().DefaultPIServer;
			server.Connect();
		}

		#endregion

		#region Properties

		private IEnumerable<string> Points
		{
			get
			{
				foreach (PISDK.PIPoint timeseries in new PISDK.PISDK().Servers[server.Name].GetPoints("tag='*'"))
				{
					yield return timeseries.Name;
				}
			}
		}

		#endregion

		#region Public Methods

		public AFElementTemplate AddTemplate(string name) => database.ElementTemplates.Add(name);

		public void ClearTemplates()
		{
			var templates = database.ElementTemplates.ToList();
			foreach (var template in templates)
			{
				database.ElementTemplates.Remove(template);
				template.CheckIn();
			}

			var elements = database.Elements.ToList();
			foreach (var eelement in elements)
			{
				database.Elements.Remove(eelement);
				eelement.CheckIn();
			}
		}

		public AFAttribute AddTimeSeries(string name, PIPointType type)
		{
			var settings = new Dictionary<string, object>(1) { [PICommonPointAttributes.PointType] = type };
			return new AFAttribute(server.CreatePIPoint(name, settings));
		}

		public bool TryGetTimeSeries(string name, out AFAttribute timeseries)
		{
			if (!PIPoint.TryFindPIPoint(server, name, out PIPoint point))
			{
				timeseries = null;
				return false;
			}

			timeseries = new AFAttribute(point);
			return true;
		}

		public AFAttribute AddOrGetTimeSeries(string name, PIPointType type)
			=> TryGetTimeSeries(name, out AFAttribute timeseries) ? timeseries : AddTimeSeries(name, type);

		public int ClearTimeSeries()
		{
			var names = Points.ToList();
			server.DeletePIPoints(names);
			return names.Count;
		}

		public void Dispose()
		{
			try { server?.Disconnect(); } catch { }
		}

		#endregion
	}
}
