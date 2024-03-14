// Copyright Karel Kroeze, 2020-2021.
// Blueprints/Blueprints/Blueprint.cs

using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace Blueprints
{
    public enum Availability
    {
        Available,
        Unavailable,
        Unset
    }

    public class Blueprint : IExposable
    {
        private HashSet<FailReason> _failReasonsMentioned = new HashSet<FailReason>();
        private IntVec2 _size;

        private List<BuildableInfo> _contents;
        public bool exported;
        public string name;

        public Blueprint() { }

        public Blueprint(List<BuildableInfo> contents, IntVec2 size, string defaultName = null, bool temporary = false)
        {
            // input
            _contents = contents;
            name = defaultName;
            _size = size;

            // provide reference to this blueprint in all contents
            contents.ForEach(item => item.blueprint = this);

            // just created, so not exported yet
            exported = false;

            // 'orrible default name
            if (name == null || !CouldBeValidBlueprintName(name))
                name = "Fluffy.Blueprints.DefaultBlueprintName".Translate();

            // increment numeric suffix until we have a unique name
            if (BlueprintController.FindBlueprint(name) != null)
            {
                var i = 1;
                while (BlueprintController.FindBlueprint(name + "_" + i) != null)
                {
                    i++;
                }

                // set name
                name = name + "_" + i;
            }

            // ask for name
            if (!temporary)
                Find.WindowStack.Add(new Dialog_NameBlueprint(this));
        }

        public List<BuildableInfo> Contents(Availability availability = Availability.Unset) =>
            availability == Availability.Available
                ? _contents.Where(item => item.Designator.Visible).ToList()
                : availability == Availability.Unavailable
                    ? _contents.Where(item => !item.Designator.Visible).ToList()
                    : Contents(Availability.Available).Concat(Contents(Availability.Unavailable)).ToList();

        public List<BuildableDef> Buildables => Contents(Availability.Available).Select(item => item.BuildableDef).ToList();

        public IEnumerable<ThingDefCount> CostListAdjusted(Availability availability = Availability.Unset) =>
            Contents(availability)
                .Select(item => item.BuildableDef.CostListAdjusted(item.Stuff, false))
                .SelectMany(x => x)
                .GroupBy(defcount => defcount.thingDef, defcount => defcount.count, (thingdef, counts) => new ThingDefCount(thingdef, counts.Sum()))
                .OrderByDescending(defcount => defcount.Count);

        public Dictionary<(BuildableDef, ThingDef), List<BuildableInfo>> GroupedBuildables(Availability availability = Availability.Unset) =>
            Contents(availability).GroupBy(item => (item.BuildableDef, item.Stuff)).ToDictionary(group => group.Key, group => group.ToList());

        public void ExposeData()
        {
            Scribe_Collections.Look(ref _contents, "BuildableThings", LookMode.Deep, this);
            Scribe_Values.Look(ref name, "Name");
            Scribe_Values.Look(ref _size, "Size");
            Scribe_Values.Look(ref exported, "Exported");
        }

        public static bool CouldBeValidBlueprintName(string name) => true;

        public static void Create(IEnumerable<IntVec3> cells, Map map)
        {
            // bail out if empty
            if (cells == null || !cells.Any())
            {
                Messages.Message("Fluffy.Blueprints.CannotCreateBluePrint_NothingSelected".Translate(), MessageTypeDefOf.RejectInput);
                return;
            }

            // get list of buildings in the cells, note that this includes frames and blueprints, and so _may include floors!_
            var things = new List<Thing>(cells.SelectMany(cell => cell.GetThingList(map).Where(thing => thing.IsValidBlueprintThing())).Distinct());

            // get list of creatable terrains
            var terrains = new List<Pair<TerrainDef, IntVec3>>();
            terrains.AddRange(cells.Select(cell => new Pair<TerrainDef, IntVec3>(cell.GetTerrain(map), cell)).Where(p => p.First.IsValidBlueprintTerrain()));

            // get edges of blueprint area
            // (might be bigger than selected region, but never smaller).
            var allCells = cells.Concat(things.SelectMany(thing => thing.OccupiedRect().Cells));

            var left = allCells.Min(cell => cell.x);
            var top = allCells.Max(cell => cell.z);
            var right = allCells.Max(cell => cell.x);
            var bottom = allCells.Min(cell => cell.z);

            // total size ( +1 because x = 2 ... x = 4 => 4 - 2 + 1 cells )
            var size = new IntVec2(right - left + 1, top - bottom + 1);

            // fetch origin for default (North) orientation
            var origin = Resources.CenterPosition(new IntVec3(left, 0, bottom), size, Rot4.North);

            // create list of buildables
            var buildables = new List<BuildableInfo>();
            foreach (var thing in things)
            {
                buildables.Add(new BuildableInfo(thing, origin));
            }

            foreach (var terrain in terrains)
            {
                buildables.Add(new BuildableInfo(terrain.First, terrain.Second, origin));
            }

            // try to get a decent default name: get rooms for occupied cells, then see if there is only one type.
            var rooms = allCells.Select(c => c.GetRoom(map)).Where(r => r != null && r.Role != RoomRoleDefOf.None).Distinct().GroupBy(r => r.Role.LabelCap);

#if DEBUG
            foreach (var room in rooms)
            {
                Blueprints.Debug.Message($"{room.Count()}x {room.Key}");
            }
#endif

            // only one type of room
            string defaultName = null;
            if (rooms.Count() == 1)
            {
                var room = rooms.First();
                defaultName = room.Count() > 1 ? "Fluffy.Blueprints.Plural".Translate(room.Key) : room.Key;
            }

            // add to controller - controller handles adding to designations
            var blueprint = new Blueprint(buildables, size, defaultName);
            BlueprintController.Add(blueprint);
        }

        public static void Create(IEnumerable<Thing> things, bool temporary = false)
        {
            // get edges of blueprint area
            // (might be bigger than selected region, but never smaller).
            var cells = things.SelectMany(thing => thing.OccupiedRect().Cells);

            var left = cells.Min(cell => cell.x);
            var top = cells.Max(cell => cell.z);
            var right = cells.Max(cell => cell.x);
            var bottom = cells.Min(cell => cell.z);

            // total size ( +1 because x = 2 ... x = 4 => 4 - 2 + 1 cells )
            var size = new IntVec2(right - left + 1, top - bottom + 1);

            // fetch origin for default (North) orientation
            var origin = Resources.CenterPosition(new IntVec3(left, 0, bottom), size, Rot4.North);

            // create list of buildables
            var buildables = new List<BuildableInfo>();
            foreach (var thing in things)
            {
                buildables.Add(new BuildableInfo(thing, origin));
            }

            // add to controller - controller handles adding to designations
            var blueprint = new Blueprint(buildables, size, "Selection", temporary);
            if (temporary)
                Find.DesignatorManager.Select(new Designator_Blueprint(blueprint));
            else
                BlueprintController.Add(blueprint);
        }

        public void Reset() => _contents = _contents.Where(item => item.BuildableDef != null).ToList();

        public void DrawGhost(IntVec3 origin) => Contents(Availability.Available).ForEach(item => item.DrawGhost(origin));

        public void DrawStuffMenu((BuildableDef, ThingDef) filter) =>
            Find.WindowStack.Add(
                new FloatMenu(
                    DefDatabase<ThingDef>
                        .AllDefsListForReading.Where(def => def.IsStuff && filter.Item1.stuffCategories.Any(cat => def.stuffProps.categories.Contains(cat)))
                        .Select(
                            stuff => new FloatMenuOption($"{stuff.LabelCap} ({Find.CurrentMap.resourceCounter.GetCount(stuff)})", () => ForEach(filter, item => item.Stuff = stuff))
                        )
                        .ToList()
                )
            );

        public void DrawDesignatorDropdownMenu((BuildableDef, ThingDef) info) =>
            Find.WindowStack.Add(
                new FloatMenu(
                    info.Item1.Dropdown().Select(buildable => new FloatMenuOption(buildable.LabelCap, () => ForEach(info, item => item.BuildableDef = buildable))).ToList()
                )
            );

        public void Flip() =>
            Contents()
                .ForEach(item =>
                {
                    var success = item.Flip();
                    if (!success && _failReasonsMentioned.Contains(success))
                        Messages.Message(success.reason, MessageTypeDefOf.RejectInput, false);
                    _failReasonsMentioned.Add(success);
                });

        public static AcceptanceReport IsValidBlueprintName(string name) =>
            !CouldBeValidBlueprintName(name)
                ? new AcceptanceReport("Fluffy.Blueprints.InvalidBlueprintName".Translate(name))
                : BlueprintController.FindBlueprint(name) != null
                    ? new AcceptanceReport("Fluffy.Blueprints.NameAlreadyTaken".Translate(name))
                    : AcceptanceReport.WasAccepted;

        public void Rotate(RotationDirection direction)
        {
            _size = _size.Rotated();
            foreach (var item in Contents())
            {
                var success = item.Rotate(direction);
                if (!success && !_failReasonsMentioned.Contains(success))
                {
                    Messages.Message(success.reason, MessageTypeDefOf.RejectInput, false);
                    _failReasonsMentioned.Add(success);
                }
            }
        }

        private void ForEach((BuildableDef, ThingDef) filter, Action<BuildableInfo> action) =>
            Contents().Where(item => (item.BuildableDef, item.Stuff) == filter).ToList().ForEach(item => action(item));

        protected internal bool ShouldLinkWith(IntVec3 position, ThingDef thingDef)
        {
            // get things at neighbouring position
            var thingsAtPosition = Contents(Availability.Available)
                .Where(item => item.Position == position && item.BuildableDef is ThingDef)
                .Select(item => item.BuildableDef as ThingDef);

            // if there's nothing there, there's nothing to link with
            if (!thingsAtPosition.Any())
                return false;

            // loop over things to see if any of the things at the neighbouring location share a linkFlag with the thingDef we're looking at
            foreach (var thing in thingsAtPosition)
                if ((thing.graphicData.linkFlags & thingDef.graphicData.linkFlags) != LinkFlags.None)
                    return true;

            // nothing stuck, return false
            return false;
        }
    }
}
