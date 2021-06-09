using System;

namespace MTSR
{
	/// <summary>Time Series Database Writer</summary>
	public interface ITSDBWriter
	{
		void Write(int id, Resolution resolution, DateTime timestamp, Representation representation);
		void Flush();
	}
}
