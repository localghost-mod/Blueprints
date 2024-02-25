using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;
using static HarmonyLib.AccessTools;

namespace Blueprints
{
    public static class Extensions
    {
        public static bool IsValidBlueprintTerrain(this TerrainDef terrain) => terrain.BuildableByPlayer;

        public static bool IsValidBlueprintThing(this Thing thing)
        {
            if (thing is RimWorld.Blueprint blueprint)
                return blueprint.def.entityDefToBuild.designationCategory != null && thing.Faction == Faction.OfPlayer;
            if (thing is Frame frame)
                return frame.def.entityDefToBuild.designationCategory != null && thing.Faction == Faction.OfPlayer;
            return thing.def.BuildableByPlayer && thing.Faction == Faction.OfPlayer;
        }

        public static HashSet<BuildableDef> _props;
        public static HashSet<BuildableDef> Props
        {
            get =>
                _props != null
                    ? _props
                    : (
                        _props =
                            TypeByName("VFEProps.PropDef") != null
                                ? GenDefDatabase
                                    .GetAllDefsInDatabaseForDef(TypeByName("VFEProps.PropDef"))
                                    .Select(def => Traverse.Create(def).Field("prop").GetValue<BuildableDef>())
                                    .ToHashSet()
                                : new HashSet<BuildableDef>()
                    );
        }
    }
}
