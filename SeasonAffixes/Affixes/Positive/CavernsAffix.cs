﻿using HarmonyLib;
using Newtonsoft.Json;
using Shockah.CommonModCode.GMCM;
using Shockah.Kokoro;
using Shockah.Kokoro.GMCM;
using Shockah.Kokoro.Stardew;
using Shockah.Kokoro.UI;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Locations;
using System;
using System.Collections.Generic;
using System.Linq;
using SObject = StardewValley.Object;

namespace Shockah.SeasonAffixes;

partial class ModConfig
{
	[JsonProperty] public int CavernsMinFloors { get; internal set; } = 2;
	[JsonProperty] public int CavernsMaxFloors { get; internal set; } = 3;
	[JsonProperty] public int CavernsMinGems { get; internal set; } = 12;
	[JsonProperty] public int CavernsMaxGems { get; internal set; } = 20;
	[JsonProperty] public bool CavernsAllowPrismaticShard { get; internal set; } = false;
}

internal sealed class CavernsAffix : BaseSeasonAffix, ISeasonAffix
{
	private static bool IsHarmonySetup = false;

	private static string ShortID => "Caverns";
	public string LocalizedDescription => Mod.Helper.Translation.Get($"{I18nPrefix}.description");
	public TextureRectangle Icon => new(Game1.objectSpriteSheet, new(128, 208, 16, 16));

	private static readonly Lazy<Func<MineShaft, bool>> IsDinoAreaGetter = new(() => AccessTools.Property(typeof(MineShaft), "isDinoArea").EmitInstanceGetter<MineShaft, bool>());
	private static readonly Lazy<Func<MineShaft, bool>> IsMonsterAreaGetter = new(() => AccessTools.Property(typeof(MineShaft), "isMonsterArea").EmitInstanceGetter<MineShaft, bool>());
	private static readonly Lazy<Func<MineShaft, int>> StonesLeftOnThisLevelGetter = new(() => AccessTools.Property(typeof(MineShaft), "stonesLeftOnThisLevel").EmitInstanceGetter<MineShaft, int>());
	private static readonly Lazy<Action<MineShaft, int>> StonesLeftOnThisLevelSetter = new(() => AccessTools.Property(typeof(MineShaft), "stonesLeftOnThisLevel").EmitInstanceSetter<MineShaft, int>());
	private static readonly PerScreen<HashSet<int>> GemCavernFloors = new(() => new());

	public CavernsAffix() : base(ShortID, "positive") { }

	public int GetPositivity(OrdinalSeason season)
		=> 1;

	public int GetNegativity(OrdinalSeason season)
		=> 0;

	public IReadOnlySet<string> Tags { get; init; } = new HashSet<string> { VanillaSkill.GemAspect };

	public void OnRegister()
		=> Apply(Mod.Harmony);

	public void OnActivate(AffixActivationContext context)
	{
		Mod.Helper.Events.GameLoop.DayStarted += OnDayStarted;
		SetUpGemCavernFloors();
	}

	public void OnDeactivate(AffixActivationContext context)
	{
		Mod.Helper.Events.GameLoop.DayStarted -= OnDayStarted;
	}

	public void SetupConfig(IManifest manifest)
	{
		var api = Mod.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu")!;
		GMCMI18nHelper helper = new(api, Mod.ModManifest, Mod.Helper.Translation);
		helper.AddNumberOption($"{I18nPrefix}.config.minFloors", () => Mod.Config.CavernsMinFloors, min: 1, max: 40, interval: 1);
		helper.AddNumberOption($"{I18nPrefix}.config.maxFloors", () => Mod.Config.CavernsMaxFloors, min: 1, max: 40, interval: 1);
		helper.AddNumberOption($"{I18nPrefix}.config.minGems", () => Mod.Config.CavernsMinGems, min: 1, max: 60, interval: 1);
		helper.AddNumberOption($"{I18nPrefix}.config.maxGems", () => Mod.Config.CavernsMaxGems, min: 1, max: 60, interval: 1);
		helper.AddBoolOption($"{I18nPrefix}.config.allowPrismaticShard", () => Mod.Config.CavernsAllowPrismaticShard);
	}

	private void Apply(Harmony harmony)
	{
		if (IsHarmonySetup)
			return;
		IsHarmonySetup = true;

		harmony.TryPatch(
			monitor: Mod.Monitor,
			original: () => AccessTools.Method(typeof(MineShaft), "populateLevel"),
			postfix: new HarmonyMethod(AccessTools.Method(GetType(), nameof(MineShaft_populateLevel_Postfix)))
		);
	}

	private void OnDayStarted(object? sender, DayStartedEventArgs e)
		=> SetUpGemCavernFloors();

	private static void SetUpGemCavernFloors()
	{
		GemCavernFloors.Value.Clear();

		var shafts = Enumerable.Range(1, MineShaft.bottomOfMineLevel)
			.Select(i => new MineShaft(i))
			.Where(IsPotentialPreLoadGemCavernFloor)
			.Select(shaft =>
			{
				shaft.loadLevel(shaft.mineLevel);
				return shaft;
			})
			.Where(IsPotentialPostLoadGemCavernFloor)
			.ToList();

		Random random = new((int)Game1.stats.DaysPlayed + (int)Game1.uniqueIDForThisGame / 2);
		int totalFloors = Math.Min(random.Next(Mod.Config.CavernsMinFloors, Mod.Config.CavernsMaxFloors + 1), shafts.Count);

		for (int i = 0; i < totalFloors; i++)
		{
			var shaft = random.NextElement(shafts);
			shafts.Remove(shaft);
			GemCavernFloors.Value.Add(shaft.mineLevel);
		}
	}

	private static bool IsPotentialPreLoadGemCavernFloor(MineShaft shaft)
	{
		if (shaft.mineLevel > MineShaft.bottomOfMineLevel)
			return false;
		if (shaft.mineLevel % 10 == 0)
			return false;
		return true;
	}

	private static bool IsPotentialPostLoadGemCavernFloor(MineShaft shaft)
	{
		if (shaft.isLevelSlimeArea() || IsMonsterAreaGetter.Value(shaft) || IsDinoAreaGetter.Value(shaft))
			return false;
		return true;
	}

	private static void MineShaft_populateLevel_Postfix(MineShaft __instance)
	{
		if (!Context.IsMainPlayer)
			return;
		if (!Mod.IsAffixActive(a => a is CavernsAffix))
			return;
		if (!GemCavernFloors.Value.Contains(__instance.mineLevel))
			return;

		GemCavernFloors.Value.Remove(__instance.mineLevel);

		List<IntPoint> possibleTiles = new();
		for (int y = 0; y < __instance.Map.DisplayHeight / Game1.tileSize; y++)
		{
			for (int x = 0; x < __instance.Map.DisplayWidth / Game1.tileSize; x++)
			{
				if (__instance.isTileClearForMineObjects(new(x, y)))
					possibleTiles.Add(new(x, y));
			}
		}

		Random random = new((int)Game1.stats.DaysPlayed + __instance.mineLevel * 150 + (int)Game1.uniqueIDForThisGame / 2);
		int gemsToSpawn = Math.Min(random.Next(Mod.Config.CavernsMinGems, Mod.Config.CavernsMaxGems + 1), (int)(possibleTiles.Count * 0.75f));

		WeightedRandom<string> weightedRandom = new();
		foreach (var (itemId, price) in GetGemDefinitions())
			weightedRandom.Add(new(1.0 / Math.Sqrt(price), itemId));

		for (int i = 0; i < gemsToSpawn; i++)
		{
			IntPoint point = Game1.random.NextElement(possibleTiles);
			possibleTiles.Remove(point);

			string gemId = weightedRandom.Next(random);
			string? stoneId = GetStoneIdForGem(gemId);

			if (stoneId is null)
			{
				if (ItemRegistry.Create(gemId) is not SObject item)
					continue;
				__instance.dropObject(item, new(point.X * Game1.tileSize, point.Y * Game1.tileSize), Game1.viewport, initialPlacement: true);
			}
			else
			{
				if (ItemRegistry.Create("(O)40") is not SObject stone)
					continue;
				stone.MinutesUntilReady = 5;
				__instance.Objects.Add(new(point.X, point.Y), stone);
				StonesLeftOnThisLevelSetter.Value(__instance, StonesLeftOnThisLevelGetter.Value(__instance) + 1);
			}
		}
	}

	private static List<(string ItemId, int Price)> GetGemDefinitions()
	{
		List<(string ItemId, int Price)> results = new();
		var data = Game1.content.Load<Dictionary<string, string>>("Data\\ObjectInformation");
		foreach (var (itemId, rawItemData) in data)
		{
			if (itemId == "74" && !Mod.Config.CavernsAllowPrismaticShard)
				continue;
			var split = rawItemData.Split("/");
			if (split[3] != "Minerals -2")
				continue;
			results.Add((ItemId: $"(O){itemId}", Price: int.Parse(split[1])));
		}
		return results;
	}

	private static string? GetStoneIdForGem(string gemIndex)
		=> gemIndex switch
		{
			"(O)72" => "(O)2", // Diamond
			"(O)64" => "(O)4", // Ruby
			"(O)70" => "(O)6", // Jade
			"(O)66" => "(O)8", // Amethyst
			"(O)68" => "(O)10", // Topaz
			"(O)60" => "(O)12", // Emerald
			"(O)62" => "(O)14", // Aquamarine
			_ => null,
		};
}