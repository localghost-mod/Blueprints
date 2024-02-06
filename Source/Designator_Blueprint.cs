using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Blueprints
{
    public class Designator_Blueprint : Designator
    {
        private float _middleMouseDownTime;

        public Designator_Blueprint(Blueprint blueprint)
        {
            Blueprint = blueprint;
            icon = Resources.Icon_Blueprint;
            soundSucceeded = SoundDefOf.Designate_PlaceBuilding;
        }

        public Blueprint Blueprint { get; }

        public override int DraggableDimensions => 0;
        public override string Label => Blueprint.name;
        public override void SelectedProcessInput(Event ev)
        {
            base.SelectedProcessInput(ev);
            HandleRotationShortcuts();
        }

        public override IEnumerable<FloatMenuOption> RightClickFloatMenuOptions
        {
            get
            {
                var options = new List<FloatMenuOption>();

                // edit
                options.Add(
                    new FloatMenuOption(
                        "Localghost.Blueprints.Edit".Translate(),
                        () => Find.WindowStack.Add(new Window_EditBlueprint(Blueprint))
                    )
                );

                // rename
                options.Add(
                    new FloatMenuOption(
                        "Fluffy.Blueprints.Rename".Translate(),
                        delegate
                        {
                            Find.WindowStack.Add(new Dialog_NameBlueprint(Blueprint));
                        }
                    )
                );

                // delete blueprint
                options.Add(
                    new FloatMenuOption(
                        "Fluffy.Blueprints.Remove".Translate(),
                        delegate
                        {
                            BlueprintController.Remove(this, false);
                        }
                    )
                );

                // delete blueprint and remove from disk
                if (Blueprint.exported)
                    options.Add(
                        new FloatMenuOption(
                            "Fluffy.Blueprints.RemoveAndDeleteXML".Translate(),
                            delegate
                            {
                                BlueprintController.Remove(this, true);
                            }
                        )
                    );
                // store to xml
                else
                    options.Add(
                        new FloatMenuOption(
                            "Fluffy.Blueprints.SaveToXML".Translate(),
                            delegate
                            {
                                BlueprintController.SaveToXML(Blueprint);
                            }
                        )
                    );

                return options;
            }
        }

        public override AcceptanceReport CanDesignateCell(IntVec3 loc)
        {
            // always return true - we're looking at the larger blueprint in SelectedUpdate() and DesignateSingleCell() instead.
            return true;
        }

        // note that even though we're designating a blueprint, as far as the game is concerned we're only designating the _origin_ cell
        public override void DesignateSingleCell(IntVec3 origin)
        {
            var somethingSucceeded = false;
            var planningMode = Event.current.shift;

            // looping through cells, place where needed.
            foreach (var item in Blueprint.Contents(Availability.Available))
            {
                var placementReport = item.CanPlace(origin);
                if (planningMode && placementReport != PlacementReport.AlreadyPlaced)
                {
                    item.Plan(origin);
                    somethingSucceeded = true;
                }

                if (!planningMode && placementReport == PlacementReport.CanPlace)
                {
                    item.Designate(origin);
                    somethingSucceeded = true;
                }
            }

            // TODO: Add succeed/failure sounds, failure reasons
            Finalize(somethingSucceeded);
        }

        // copy-pasta from RimWorld.Designator_Place, with minor changes.
        public override void DoExtraGuiControls(float leftX, float bottomY)
        {
            var height = 90f;
            var width = 200f;

            var margin = 9f;
            var topmargin = 15f;
            var numButtons = 3;
            var button = Mathf.Min(
                (width - (numButtons + 1) * margin) / numButtons,
                height - topmargin
            );

            var winRect = new Rect(leftX, bottomY - height, width, height);

            Find.WindowStack.ImmediateWindow(
                73095,
                winRect,
                WindowLayer.GameUI,
                delegate
                {
                    var rotationDirection = RotationDirection.None;
                    Text.Anchor = TextAnchor.MiddleCenter;
                    Text.Font = GameFont.Medium;

                    var rotLeftRect = new Rect(margin, topmargin, button, button);
                    Widgets.Label(rotLeftRect, KeyBindingDefOf.Designator_RotateLeft.MainKeyLabel);
                    if (Widgets.ButtonImage(rotLeftRect, Resources.RotLeftTex))
                    {
                        SoundDefOf.DragSlider.PlayOneShotOnCamera();
                        rotationDirection = RotationDirection.Counterclockwise;
                        Event.current.Use();
                    }

                    var flipRect = new Rect(2 * margin + button, topmargin, button, button);
                    Widgets.Label(flipRect, KeyBindingDefOf2.Blueprint_Flip.MainKeyLabel);
                    if (Widgets.ButtonImage(flipRect, Resources.FlipTex))
                    {
                        SoundDefOf.DragSlider.PlayOneShotOnCamera();
                        Blueprint.Flip();
                        Event.current.Use();
                    }

                    var rotRightRect = new Rect(3 * margin + 2 * button, topmargin, button, button);
                    Widgets.Label(
                        rotRightRect,
                        KeyBindingDefOf.Designator_RotateRight.MainKeyLabel
                    );
                    if (Widgets.ButtonImage(rotRightRect, Resources.RotRightTex))
                    {
                        SoundDefOf.DragSlider.PlayOneShotOnCamera();
                        rotationDirection = RotationDirection.Clockwise;
                        Event.current.Use();
                    }

                    if (rotationDirection != RotationDirection.None)
                        Blueprint.Rotate(rotationDirection);
                    Text.Anchor = TextAnchor.UpperLeft;
                    Text.Font = GameFont.Small;
                }
            );
        }

        public override bool GroupsWith(Gizmo other)
        {
            return Blueprint == (other as Designator_Blueprint).Blueprint;
        }

        public override void Selected()
        {
            base.Selected();
            if (!Blueprint.Contents(Availability.Available).Any())
            {
                Messages.Message(
                    "Fluffy.Blueprints.NothingAvailableInBlueprint".Translate(Blueprint.name),
                    MessageTypeDefOf.RejectInput
                );
            }
            else
            {
                var unavailable = Blueprint
                    .Contents(Availability.Unavailable)
                    .Select(item => item.BuildableDef.label)
                    .Distinct();
                if (unavailable.Any())
                    Messages.Message(
                        "Fluffy.Blueprints.XNotAvailableInBlueprint".Translate(
                            Blueprint.name,
                            string.Join(", ", unavailable.ToArray())
                        ),
                        MessageTypeDefOf.CautionInput
                    );
            }
        }

        public override void SelectedUpdate()
        {
            GenDraw.DrawNoBuildEdgeLines();
            if (!ArchitectCategoryTab.InfoRect.Contains(UI.MousePositionOnUI))
            {
                var origin = UI.MouseCell();
                Blueprint.DrawGhost(origin);
            }
        }

        // Copy-pasta from RimWorld.HandleRotationShortcuts()
        private void HandleRotationShortcuts()
        {
            var rotationDirection = RotationDirection.None;
            if (Event.current.button == 2)
            {
                if (Event.current.type == EventType.MouseDown)
                {
                    Event.current.Use();
                    _middleMouseDownTime = Time.realtimeSinceStartup;
                }

                if (
                    Event.current.type == EventType.MouseUp
                    && Time.realtimeSinceStartup - _middleMouseDownTime < 0.15f
                )
                    rotationDirection = RotationDirection.Clockwise;
            }

            if (KeyBindingDefOf.Designator_RotateRight.KeyDownEvent)
                rotationDirection = RotationDirection.Clockwise;
            if (KeyBindingDefOf.Designator_RotateLeft.KeyDownEvent)
                rotationDirection = RotationDirection.Counterclockwise;
            if (KeyBindingDefOf2.Blueprint_Flip.KeyDownEvent)
            {
                SoundDefOf.DragSlider.PlayOneShotOnCamera();
                Blueprint.Flip();
            }

            if (rotationDirection == RotationDirection.Clockwise)
            {
                SoundDefOf.DragSlider.PlayOneShotOnCamera();
                Blueprint.Rotate(RotationDirection.Clockwise);
            }

            if (rotationDirection == RotationDirection.Counterclockwise)
            {
                SoundDefOf.DragSlider.PlayOneShotOnCamera();
                Blueprint.Rotate(RotationDirection.Counterclockwise);
            }
        }
    }
}
