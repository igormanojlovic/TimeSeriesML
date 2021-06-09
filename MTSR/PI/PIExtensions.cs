using OSIsoft.AF.Asset;

namespace MTSR
{
	public static class PIExtensions
	{
		private static string GetElementName(this AFElementTemplate template, int id) => $"{template.Name}{id}";
		private static string GetAttributeName(Resolution resolution, ValueType valueType) => $"{resolution}_{valueType}";

		public static void AddElement(this AFElementTemplate template, int id)
			=> template.Database.Elements.Add(template.GetElementName(id), template).CheckIn();

		public static string GetTimeSeriesName(this AFElementTemplate template, int id, Resolution resolution, ValueType valueType)
			=> $"{template.GetElementName(id)}.{GetAttributeName(resolution, valueType)}";

		public static AFAttributeTemplate AddTimeSeries(this AFElementTemplate template, Resolution resolution, ValueType valueType)
		{
			var name = GetAttributeName(resolution, valueType);
			var attribute = template.AttributeTemplates.Add(name);
			attribute.DataReferencePlugIn = AFDataReference.GetPIPointDataReference(template.Database.PISystem);
			attribute.ConfigString = $@"\\%Server%\%Element%.{name}";
			attribute.Type = typeof(float);
			return attribute;
		}

		public static void AddAnalysis(this AFAttributeTemplate template, string equation, int frequency)
		{
			var at = template.ElementTemplate.AnalysisTemplates.Add(template.Name);
			at.ExtendedProperties.Add("AutoRecalculationEnabled", true);
			at.AnalysisRulePlugIn = at.Database.PISystem.AnalysisRulePlugIns["PerformanceEquation"];
			at.AnalysisRule.VariableMapping = $"Variable1||{template.Name};";
			at.AnalysisRule.ConfigString = $"Variable1:={equation};Variable2:=";
			at.TimeRulePlugIn = at.Database.PISystem.TimeRulePlugIns["Periodic"];
			at.TimeRule.ConfigString = $"Frequency={frequency}";
		}
	}
}
