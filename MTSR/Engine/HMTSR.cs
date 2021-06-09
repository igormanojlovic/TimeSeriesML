using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace MTSR
{
	/// <summary>Hierarchical Multiresolution Time Series Representation</summary>
	public abstract class HMTSR<TFunction, TRepresentation> : IMTSR where TRepresentation : Representation, new()
	{
		private class Buffer
		{
			public Buffer(TimeSeriesTuple first) => Last = first;
			public TRepresentation Root { get; set; } = new TRepresentation();
			public Dictionary<Resolution, TRepresentation> Derived { get; } = new Dictionary<Resolution, TRepresentation>();
			public TimeSeriesTuple Last { get; set; }
		}

		private readonly ITSDB db;
		private readonly Resolution root;
		private readonly Resolution[] descendants;
		private readonly Dictionary<Resolution, Resolution[]> hierarchy;
		private readonly ConcurrentDictionary<int, Buffer> buffers = new ConcurrentDictionary<int, Buffer>();

		protected HMTSR(ITSDB db)
		{
			this.db = db;
			root = db.Resolutions.Root(out hierarchy);
			descendants = db.Resolutions.Where(r => !r.Equals(root)).ToArray();
		}

		private void ProcessDerived(int id, Buffer buffer, Resolution parent, TimeInterval interval, TRepresentation representation, ITSDBWriter writer)
		{
			foreach (var child in hierarchy.GetChildren(parent))
			{
				if (!buffer.Derived.TryGetValue(child, out TRepresentation derived))
				{
					buffer.Derived.Add(child, derived = new TRepresentation());
				}

				var childInterval = child.GetInterval(interval.From);
				buffer.Derived[child] = derived = Merge(derived, representation);
				if (interval.To == childInterval.To)
				{
					writer.Write(id, child, childInterval.From, derived);
					ProcessDerived(id, buffer, child, childInterval, derived, writer);
					buffer.Derived[child] = new TRepresentation();
				}
			}
		}

		protected abstract TFunction GetPiecewiseFunction(TimeSeriesTuple t1, TimeSeriesTuple t2);
		protected abstract TRepresentation Aggregate(TFunction f, TimeInterval interval);
		protected abstract TRepresentation Merge(TRepresentation a, TRepresentation b);

		public void Process(int id, TimeSeriesTuple tuple)
		{
			if (!buffers.TryGetValue(id, out Buffer buffer))
			{
				buffers.TryAdd(id, buffer = new Buffer(tuple));
				return;
			}

			var writer = db.CreateWriter();
			var function = GetPiecewiseFunction(buffer.Last, tuple);
			var extension = new TimeInterval(buffer.Last.Timestamp, tuple.Timestamp);
			foreach (var interval in root.GetIntervals(extension))
			{
				buffer.Root = Merge(buffer.Root, Aggregate(function, interval.Over(extension)));
				if (interval.To <= tuple.Timestamp)
				{
					writer.Write(id, root, interval.From, buffer.Root);
					ProcessDerived(id, buffer, root, interval, buffer.Root, writer);
					buffer.Root = new TRepresentation();
				}
			}

			buffer.Last = tuple;
			writer.Flush();
		}
	}

	/// <summary>Hierarchical Multiresolution Linear-function-based Piecewise Statistical Approximation</summary>
	public class HMLPSA : HMTSR<LinearFunction, PLFPSA>
	{
		public const string FullName = "Hierarchical Multiresolution Piecewise-Linear-Function-based Piecewise Statistical Approximation";
		public HMLPSA(ITSDB db) : base(db) { }
		protected override LinearFunction GetPiecewiseFunction(TimeSeriesTuple t1, TimeSeriesTuple t2) => new LinearFunction(t1, t2);
		protected override PLFPSA Aggregate(LinearFunction f, TimeInterval interval) => new PLFPSA(f, interval);
		protected override PLFPSA Merge(PLFPSA a, PLFPSA b) => new PLFPSA(a, b);
	}
}
