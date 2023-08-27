﻿using HarmonyLib;
using Microsoft.Xna.Framework;
using Shockah.Kokoro;
using Shockah.Kokoro.Stardew;
using Shockah.Kokoro.UI;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.GameData.Locations;
using StardewValley.Internal;
using StardewValley.TerrainFeatures;
using System;
using System.Collections.Generic;
using System.Linq;
using SObject = StardewValley.Object;

namespace Shockah.SeasonAffixes;

internal sealed class OvergrowthAffix : BaseSeasonAffix, ISeasonAffix // TODO: test in 1.6
{
	private static bool IsHarmonySetup = false;
	private static readonly int SpawnDelay = 250;

	private static string ShortID => "Overgrowth";
	public string LocalizedDescription => Mod.Helper.Translation.Get($"{I18nPrefix}.description");
	public TextureRectangle Icon => new(Game1.objectSpriteSheet, new(288, 256, 16, 16));

	public OvergrowthAffix() : base(ShortID, "positive") { }

	public int GetPositivity(OrdinalSeason season)
		=> 1;

	public int GetNegativity(OrdinalSeason season)
		=> 0;

	public IReadOnlySet<string> Tags { get; init; } = new HashSet<string> { VanillaSkill.CropsAspect, VanillaSkill.FlowersAspect, VanillaSkill.GatheringAspect };

	public void OnRegister()
		=> Apply(Mod.Harmony);

	private void Apply(Harmony harmony)
	{
		if (IsHarmonySetup)
			return;
		IsHarmonySetup = true;

		harmony.TryPatch(
			monitor: Mod.Monitor,
			original: () => AccessTools.Method(typeof(Crop), nameof(Crop.harvest)),
			postfix: new HarmonyMethod(AccessTools.Method(GetType(), nameof(Crop_harvest_Postfix)))
		);
	}

	private static void Crop_harvest_Postfix(int xTile, int yTile, HoeDirt soil, ref bool __result)
	{
		if (!__result)
			return;
		if (!Mod.IsAffixActive(a => a is OvergrowthAffix))
			return;

		var forage = GetForageToSpawn(soil.Location);
		if (forage is null)
			return;

		Vector2 point = new(xTile, yTile);
		Poof(soil.Location, point);
		var location = soil.Location;

		DelayedAction.functionAfterDelay(() =>
		{
			location.removeObjectsAndSpawned(xTile, yTile, 1, 1);
			location.dropObject(forage, point * 64, Game1.viewport, initialPlacement: true);
		}, SpawnDelay);
	}

	private static SObject? GetForageToSpawn(GameLocation location)
	{
		var random = new Random();
		var possibleForage = GetPossibleForage(location, random);
		if (possibleForage.Count == 0)
			return null;

		WeightedRandom<SpawnForageData> weighted = new(
			possibleForage
				.Where(entry => entry.Chance > 0)
				.Select(entry => new WeightedItem<SpawnForageData>(Math.Pow(entry.Chance, 2), entry))
		);
		var forage = weighted.Next(random);
		if (forage is null)
			return null;
		if (ItemQueryResolver.TryResolveRandomItem(forage, new ItemQueryContext(location, null, random)) is not SObject item)
			return null;
		return item;
	}

	private static List<SpawnForageData> GetPossibleForage(GameLocation location, Random random)
	{
		List<SpawnForageData> forage = new();
		forage.AddRange(GetPossibleForage(location, location.Name, random));

		if (forage.Count == 0)
		{
			forage.AddRange(GetPossibleForage(location, "BusStop", random));
			forage.AddRange(GetPossibleForage(location, "Forest", random));
			forage.AddRange(GetPossibleForage(location, "Town", random));
			forage.AddRange(GetPossibleForage(location, "Mountain", random));
			forage.AddRange(GetPossibleForage(location, "Backwoods", random));
			forage.AddRange(GetPossibleForage(location, "Railroad", random));
		}
		return forage;
	}

	private static List<SpawnForageData> GetPossibleForage(GameLocation location, string dataLocationName, Random random)
	{
		var data = Game1.content.Load<Dictionary<string, LocationData>>("Data\\Locations");
		if (!data.TryGetValue(dataLocationName, out var locationData))
			return new List<SpawnForageData>();

		return locationData.Forage
			.Where(entry =>
			{
				if (entry.Season is not null && entry.Season.Value != location.GetSeason())
					return false;
				if (entry.Condition is not null && !GameStateQuery.CheckConditions(entry.Condition, location, null, null, null, random))
					return false;
				return true;
			})
			.ToList();
	}

	private static void Poof(GameLocation location, Vector2 point)
	{
		var sprite = new TemporaryAnimatedSprite(
			textureName: Game1.mouseCursorsName,
			sourceRect: new Rectangle(464, 1792, 16, 16),
			animationInterval: 120f,
			animationLength: 5,
			numberOfLoops: 0,
			position: point * Game1.tileSize,
			flicker: false,
			flipped: Game1.random.NextBool(),
			layerDepth: 1f,
			alphaFade: 0.01f,
			color: Color.White,
			scale: Game1.pixelZoom,
			scaleChange: 0.01f,
			rotation: 0f,
			rotationChange: 0f
		)
		{
			light = true
		};
		Game1.Multiplayer.broadcastSprites(location, sprite);
	}
}