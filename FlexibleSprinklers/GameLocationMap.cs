﻿using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;

namespace Shockah.FlexibleSprinklers
{
    public class GameLocationMap: IMap
    {
        private readonly GameLocation location;

        public GameLocationMap(GameLocation location)
        {
            this.location = location;
        }
        
        public SoilType this[IntPoint point]
        {
            get
            {
                var tileVector = new Vector2(point.X, point.Y);
                if (location.Objects.TryGetValue(tileVector, out Object @object) && @object.IsSprinkler())
                    return SoilType.Sprinkler;
                if (!location.terrainFeatures.TryGetValue(tileVector, out TerrainFeature feature))
                    return SoilType.NonSoil;
                if (!(feature is HoeDirt))
                    return SoilType.NonSoil;
                if (location.doesTileHavePropertyNoNull(point.X, point.Y, "NoSprinklers", "Back").ToUpper().StartsWith("T"))
                    return SoilType.NonWaterable;

                var soil = (HoeDirt)feature;
                return soil.needsWatering() ? SoilType.Dry : SoilType.Wet;
            }
        }

        public void WaterTile(IntPoint point)
        {
            var can = new WateringCan();
            var tileVector = new Vector2(point.X, point.Y);

            if (location.terrainFeatures.TryGetValue(tileVector, out TerrainFeature feature))
                feature.performToolAction(can, 0, tileVector, location);
            if (location.Objects.TryGetValue(tileVector, out Object @object))
                @object.performToolAction(can, location);

            // TODO: add animation, if needed
        }
    }
}