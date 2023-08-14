﻿using Newtonsoft.Json;

namespace Shockah.InAHeartbeat;

public sealed class ModConfig
{
	[JsonProperty] public bool IsBouquetCraftable { get; internal set; } = true;
	[JsonProperty] public bool IsPendantCraftable { get; internal set; } = true;
	[JsonProperty] public QualityBasedConfig<int> DateFriendshipRequired { get; internal set; } = new(2000, 1750, 1500, 1250);
	[JsonProperty] public QualityBasedConfig<int> MarryFriendshipRequired { get; internal set; } = new(2500, 2250, 2000, 1750);
	[JsonProperty] public int BouquetFlowersRequired { get; internal set; } = 5;
	[JsonProperty] public int BouquetFlowerTypesRequired { get; internal set; } = 2;
	[JsonProperty] public QualityBasedConfig<int> PendantGemsRequired { get; internal set; } = new(2, 4, 6, 8);
}