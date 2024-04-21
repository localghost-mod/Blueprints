using System.Collections.Generic;
using System.IO;
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
        public static string toBlueprintPath(this string name) => Path.Combine(BlueprintController.SaveLocation, string.Concat(name.Split(Path.GetInvalidFileNameChars())) + ".xml");
        public static void SafeLook<T>(ref T value, string label) where T : Def, new()
        {
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                string text = value == null ? "null" : value.defName;
                Scribe_Values.Look(ref text, label, "null", false);
            }
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                var subNode = Scribe.loader.curXmlParent[label];
                value = (subNode == null || subNode.InnerText == null || subNode.InnerText == "null") ? default : DefDatabase<T>.GetNamedSilentFail(BackCompatibility.BackCompatibleDefName(typeof(T), subNode.InnerText, false, subNode));
            }

        }
    }
}
