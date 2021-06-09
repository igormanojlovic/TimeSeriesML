namespace MTSR
{
	/// <summary>Multiresolution Time Series Representation</summary>
	public interface IMTSR
	{
		void Process(int id, TimeSeriesTuple tuple);
	}
}
