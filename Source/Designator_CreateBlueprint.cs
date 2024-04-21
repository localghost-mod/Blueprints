using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace Blueprints
{
    public class Designator_CreateBlueprint : Designator
    {
        static BlueprintController BlueprintController => Find.World.GetComponent<BlueprintController>();
        public Designator_CreateBlueprint()
        {
            icon = Resources.Icon_AddBlueprint;
            defaultLabel = "Fluffy.Blueprints.Create".Translate();
            defaultDesc = "Fluffy.Blueprints.CreateHelp".Translate();
            useMouseIcon = true;
            soundDragChanged = SoundDefOf.Designate_DragStandard_Changed;
            soundDragSustain = SoundDefOf.Designate_DragStandard;
            soundSucceeded = SoundDefOf.Designate_PlanAdd;
            tutorTag = "Blueprint";
        }

        public override int DraggableDimensions => 2;

        public override IEnumerable<FloatMenuOption> RightClickFloatMenuOptions
            => BlueprintController.UnloadedBlueprints.Select(blueprint => new FloatMenuOption("Fluffy.Blueprints.LoadFromXML".Translate(blueprint.name), () => BlueprintController.Add(blueprint)));

        public override AcceptanceReport CanDesignateCell(IntVec3 loc)
        {
            var things = loc.GetThingList(Map);
            return loc.InBounds(Map)
                && !loc.Fogged(Map)
                && (loc.GetTerrain(Map).IsValidBlueprintTerrain() || !things.NullOrEmpty() && things.Any(thing => thing.IsValidBlueprintThing()));
        }

        public override void RenderHighlight(List<IntVec3> dragCells) => DesignatorUtility.RenderHighlightOverSelectableCells(this, dragCells);

        public override void DesignateMultiCell(IEnumerable<IntVec3> cells) => Blueprint.Create(cells, Map);
    }
}
