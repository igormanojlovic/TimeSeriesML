namespace MTSR
{
	/// <summary>Time Series Database</summary>
	public interface ITSDB
	{
		Resolution[] Resolutions { get; }
		ITSDBWriter CreateWriter();
	}
}
