using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using RimWorld.Planet;
using Verse;

namespace Blueprints
{
    class BlueprintController : WorldComponent
    {
        public static readonly string SaveLocation = Path.Combine(GenFilePaths.SaveDataFolderPath, "Blueprints");
        List<Blueprint> blueprints = new List<Blueprint>();
        List<Blueprint> savedBlueprints = new List<Blueprint>();
        public IEnumerable<Blueprint> UnloadedBlueprints
        {
            get
            {
                if (!initialized)
                    Init();
                return savedBlueprints.Except(blueprints);
            }
        }
        List<Designator> designators;
        bool initialized;

        public BlueprintController(World world)
            : base(world) { }

        public bool Exported(Blueprint blueprint) => savedBlueprints.Contains(blueprint);

        public void Add(Blueprint blueprint)
        {
            if (blueprints.Contains(blueprint))
            {
                Log.Warning($"Try adding duplicate blueprint name: {blueprint.name}");
                return;
            }
            blueprints.Add(blueprint);
            designators.Add(new Designator_Blueprint(blueprint));
        }

        public void Import(string name)
        {

        }
        public void Export(Blueprint blueprint)
        {
            Scribe.saver.InitSaving(blueprint.name.toBlueprintPath(), "Blueprint");
            ScribeMetaHeaderUtility.WriteMetaHeader();
            Scribe_Deep.Look(ref blueprint, "Blueprint");
            Scribe.saver.FinalizeSaving();

            savedBlueprints.Add(blueprint);
            blueprint.exported = true;
        }

        public void Delete(Blueprint blueprint)
        {
            File.Delete(blueprint.name.toBlueprintPath());

            savedBlueprints.Remove(blueprint);
            blueprint.exported = false;
        }

        public void Remove(Designator_Blueprint designator, bool delete = false)
        {
            blueprints.Remove(designator.Blueprint);
            designators.Remove(designator);
            if (delete)
                Delete(designator.Blueprint);
        }

        public bool HasName(string name) => blueprints.Any(x => x.name == name);

        public bool TryRename(Blueprint blueprint, string name)
        {
            if (savedBlueprints.Any(x => x.name.toBlueprintPath() == name.toBlueprintPath()))
                return false;
            Delete(blueprint);
            blueprint.name = name;
            Export(blueprint);
            return true;
        }
        void Init()
        {
            Directory.CreateDirectory(SaveLocation);
            var info = new DirectoryInfo(SaveLocation);
            foreach (var file in new DirectoryInfo(SaveLocation).GetFiles().Where(file => file.Extension == ".xml").OrderByDescending(file => file.LastWriteTime))
            {
                var blueprint = new Blueprint();
                Scribe.loader.InitLoading(file.FullName);
                Scribe.EnterNode("Blueprint");
                blueprint.ExposeData();
                Scribe.ExitNode();
                Scribe.loader.FinalizeLoading();
                blueprint.Reset();
                blueprint.exported = true;
                savedBlueprints.Add(blueprint);
            }
            initialized = true;
        }
        public override void FinalizeInit()
        {
            new Harmony("fluffy.blueprints").PatchAll();
            var cat = DefDatabase<DesignationCategoryDef>.GetNamed("Blueprints");
            designators = cat.AllResolvedDesignators;
            foreach (var blueprint in blueprints)
                designators.Add(new Designator_Blueprint(blueprint));

        }

        public override void ExposeData() => Scribe_Collections.Look(ref blueprints, "Blueprints");
    }
}
