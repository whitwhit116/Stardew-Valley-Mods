﻿using Shockah.Kokoro.Stardew;
using Shockah.Kokoro.UI;
using StardewValley;
using System.Runtime.CompilerServices;

namespace Shockah.SeasonAffixes.Affixes.Positive
{
	internal sealed class InnovationAffix : ISeasonAffix
	{
		private SeasonAffixes Mod { get; init; }

		private static string ShortID => "Innovation";
		public string UniqueID => $"{Mod.ModManifest.UniqueID}.{ShortID}";
		public string LocalizedName => Mod.Helper.Translation.Get($"affix.positive.{ShortID}.name");
		public string LocalizedDescription => Mod.Helper.Translation.Get($"affix.positive.{ShortID}.description");
		public TextureRectangle Icon => new(Game1.objectSpriteSheet, new(32, 80, 16, 16));

		public InnovationAffix(SeasonAffixes mod)
		{
			this.Mod = mod;
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		public int GetPositivity(OrdinalSeason season)
			=> 1;

		[MethodImpl(MethodImplOptions.NoInlining)]
		public int GetNegativity(OrdinalSeason season)
			=> 0;

		// TODO: Innovation implementation
	}
}