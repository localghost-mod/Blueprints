using System.Linq;
using UnityEngine;
using Verse;

namespace Blueprints
{
    public class Window_EditBlueprint : Window
    {
        private string title => "Localghost.Blueprints.EditBlueprint".Translate(_blueprint.name);
        private Blueprint _blueprint;
        private Vector2 _scrollPosition = Vector2.zero;

        public Window_EditBlueprint(Blueprint blueprint)
        {
            _blueprint = blueprint;
            preventCameraMotion = false;
            draggable = true;
            resizeable = true;
            closeOnAccept = false;
            doCloseX = true;
        }

        public float scrollviewHeight =>
            Text.LineHeightOf(GameFont.Medium)
            + 10f
            + Text.LineHeightOf(GameFont.Small) * _blueprint.GroupedBuildables.Count
            + 12f
            + 24f
            + Text.LineHeightOf(GameFont.Small) * _blueprint.CostListAdjusted.Count;

        public override void DoWindowContents(Rect inRect)
        {
            var width = inRect.width - 16f;
            Widgets.BeginScrollView(
                inRect,
                ref _scrollPosition,
                new Rect(0f, 0f, width, scrollviewHeight)
            );
            Text.Font = GameFont.Medium;
            Rect titleRect = inRect.TopPartPixels(Text.LineHeight).AtZero();
            Widgets.Label(titleRect, title);
            var curY = Text.LineHeight + 10f;
            Text.Font = GameFont.Small;
            foreach (var buildables in _blueprint.GroupedBuildables)
            {
                var curX = 5f;
                Widgets.Label(new Rect(5f, curY, width * .2f, 100f), buildables.Value.Count + "x");
                curX += width * .2f;

                var height = 0f;
                if (buildables.Value.First().Stuff != null)
                {
                    var label =
                        buildables.Value.First().Stuff.LabelAsStuff.CapitalizeFirst()
                        + " "
                        + buildables.Key.Item1.label;

                    var iconRect = new Rect(curX, curY, 12f, 12f);
                    curX += 16f;

                    height = Text.CalcHeight(label, width - curX);
                    var labelRect = new Rect(curX, curY, width - curX, height);
                    var buttonRect = new Rect(curX - 16f, curY, width - curX + 16f, height);
                    if (Mouse.IsOver(buttonRect))
                    {
                        GUI.DrawTexture(buttonRect, TexUI.HighlightTex);
                        GUI.color = GenUI.MouseoverColor;
                    }
                    GUI.DrawTexture(iconRect, Resources.Icon_Edit);
                    GUI.color = Color.white;
                    Widgets.Label(labelRect, label);
                    if (Widgets.ButtonInvisible(buttonRect))
                        _blueprint.DrawStuffMenu(buildables.Key);
                }
                else if (!buildables.Value.First().BuildableDef.Dropdown().EnumerableNullOrEmpty())
                {
                    var label = buildables.Key.Item1.LabelCap;
                    
                    var iconRect = new Rect(curX, curY, 12f, 12f);
                    curX += 16f;

                    height = Text.CalcHeight(label, width - curX);
                    var labelRect = new Rect(curX, curY, width - curX, height);
                    var buttonRect = new Rect(curX - 16f, curY, width - curX + 16f, height);
                    if (Mouse.IsOver(buttonRect))
                    {
                        GUI.DrawTexture(buttonRect, TexUI.HighlightTex);
                        GUI.color = GenUI.MouseoverColor;
                    }
                    GUI.DrawTexture(iconRect, Resources.Icon_Edit);
                    GUI.color = Color.white;
                    Widgets.Label(labelRect, label);
                    if (Widgets.ButtonInvisible(buttonRect))
                        _blueprint.DrawDesignatorDropdownMenu(buildables.Key);
                }
                else if (false)
                {
                    var label = buildables.Key.Item1.LabelCap;

                    var iconRect = new Rect(curX, curY, 12f, 12f);
                    curX += 16f;

                    height = Text.CalcHeight(label, width - curX);
                    var labelRect = new Rect(curX, curY, width - curX, height);
                    var buttonRect = new Rect(curX - 16f, curY, width - curX + 16f, height);
                    if (Mouse.IsOver(buttonRect))
                    {
                        GUI.DrawTexture(buttonRect, TexUI.HighlightTex);
                        GUI.color = GenUI.MouseoverColor;
                    }
                    GUI.DrawTexture(iconRect, Resources.Icon_Edit);
                    GUI.color = Color.white;
                    Widgets.Label(labelRect, label);
                    if (Widgets.ButtonInvisible(buttonRect))
                        _blueprint.DrawDesignatorDropdownMenu(buildables.Key);

                }
                else
                {
                    var labelWidth = width - curX;
                    var label = buildables.Key.Item1.LabelCap;
                    height = Text.CalcHeight(label, labelWidth);
                    Widgets.Label(new Rect(curX, curY, labelWidth, height), label);
                }
                curY += height;
            }

            curY += 12f;
            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(0f, curY, width, 24f), "Fluffy.Blueprint.Cost".Translate());
            curY += 24f;

            Text.Font = GameFont.Small;
            var costlist = _blueprint.CostListAdjusted;
            for (var i = 0; i < costlist.Count; i++)
            {
                var thingCount = costlist[i];
                Texture2D image;
                if (thingCount.ThingDef == null)
                    image = BaseContent.BadTex;
                else
                    image = thingCount.ThingDef.uiIcon;
                GUI.DrawTexture(new Rect(0f, curY, 20f, 20f), image);
                if (
                    thingCount.ThingDef != null
                    && thingCount.ThingDef.resourceReadoutPriority
                        != ResourceCountPriority.Uncounted
                    && Find.CurrentMap.resourceCounter.GetCount(thingCount.ThingDef)
                        < thingCount.Count
                )
                    GUI.color = Color.red;
                Widgets.Label(new Rect(26f, curY + 2f, 50f, 100f), thingCount.Count.ToString());
                GUI.color = Color.white;
                string text;
                if (thingCount.ThingDef == null)
                    text = "(" + "UnchosenStuff".Translate() + ")";
                else
                    text = thingCount.ThingDef.LabelCap;
                var height = Text.CalcHeight(text, width - 60f) - 2f;
                Widgets.Label(new Rect(60f, curY + 2f, width - 60f, height), text);
                curY += height;
            }

            Widgets.EndScrollView();
        }
    }
}
