using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Blueprints
{
    public static class Utils
    {
        private static IEnumerable<BuildableDef> _buildableDefs = DefDatabase<TerrainDef>
            .AllDefsListForReading.Select(def => def as BuildableDef)
            .Concat(DefDatabase<ThingDef>.AllDefsListForReading.Select(def => def as BuildableDef))
            .Where(def => def.BuildableByPlayer);

        public static IEnumerable<BuildableDef> Dropdown(this BuildableDef def) =>
            def.designatorDropdown == null ? null : _buildableDefs.Where(_def => _def.designatorDropdown == def.designatorDropdown);
    }
}
