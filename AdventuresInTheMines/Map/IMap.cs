﻿using Shockah.CommonModCode;

namespace Shockah.AdventuresInTheMines.Map
{
	public interface IMap<TTile>
	{
		TTile this[IntPoint point] { get; }

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Nested in another interface")]
		public interface WithKnownSize : IMap<TTile>
		{
			IntRectangle Bounds { get; }
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Nested in another interface")]
		public interface Writable : IMap<TTile>
		{
			new TTile this[IntPoint point] { get;  set; }
		}
	}
}