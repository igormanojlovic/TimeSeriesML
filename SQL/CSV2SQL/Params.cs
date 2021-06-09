namespace CSV2DB
{
	public class Params
	{
		#region Properties

		public string DataFolder { get; set; } = string.Empty;
		public char CSVSeparator { get; set; } = ',';
		public string SqlInstance { get; set; } = string.Empty;
		public string SqlDatabase { get; set; } = string.Empty;
		public string SqlTable { get; set; } = string.Empty;
		public int LineFrom { get; set; } = int.MinValue;
		public int LineTo { get; set; } = int.MaxValue;

		#endregion
	}
}
