using RimWorld;
using UnityEngine;
using Verse;

namespace Blueprints
{
    public class Dialog_NameBlueprint : Dialog_Rename
    {
        static BlueprintController BlueprintController => Find.World.GetComponent<BlueprintController>();
        private readonly Blueprint blueprint;

        public Dialog_NameBlueprint(Blueprint blueprint)
        {
            this.blueprint = blueprint;
            curName = blueprint.name;
        }

        protected override string Title => "Fluffy.Blueprints.Rename".Translate();

        protected override AcceptanceReport NameIsValid(string name)
        {
            // always ok if we didn't change anything
            if (name == blueprint.name)
                return true;

            // otherwise check for used symbols and uniqueness
            var validName = Blueprint.IsValidBlueprintName(name);
            if (!validName.Accepted)
                return validName;

            // finally, if we're renaming an already exported blueprint, check if the new name doesn't already exist
            if (blueprint.exported && !BlueprintController.TryRename(blueprint, name))
                return new AcceptanceReport("Fluffy.Blueprints.ExportedBlueprintWithThatNameAlreadyExists".Translate(name));

            // if all checks are passed, return true.
            return true;
        }

        protected override void SetName(string name) => blueprint.name = name;
    }

    public class Dialog_Rename : Window
    {
#if v1_4
        protected Dialog_Rename()
            : base()
#else
        protected Dialog_Rename()
            : base(null)
#endif
        {
            doCloseX = true;
            forcePause = true;
            closeOnAccept = false;
            closeOnClickedOutside = true;
            absorbInputAroundWindow = true;
        }

        protected virtual void SetName(string name) { }

        protected virtual string Title => "Rename".Translate();

        public override Vector2 InitialSize => new Vector2(280f, 175f);

        protected virtual AcceptanceReport NameIsValid(string name) => true;

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Small;
            bool flag = false;
            if (Event.current.type == EventType.KeyDown && (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter))
            {
                flag = true;
                Event.current.Use();
            }
            Rect rect = new Rect(inRect);
            Text.Font = GameFont.Medium;
            rect.height = Text.LineHeight + 10f;
            Widgets.Label(rect, Title);
            Text.Font = GameFont.Small;
            GUI.SetNextControlName("RenameField");
            string text = Widgets.TextField(new Rect(0f, rect.height, inRect.width, 35f), curName);
            if (text.Length < 100)
                curName = text;
            else
                ((TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl)).SelectAll();

            if (!focusedRenameField)
            {
                UI.FocusControl("RenameField", this);
                focusedRenameField = true;
            }
            if (Widgets.ButtonText(new Rect(15f, inRect.height - 35f - 10f, inRect.width - 15f - 15f, 35f), "Confirm".Translate()) || flag)
            {
                AcceptanceReport acceptanceReport = NameIsValid(curName);
                if (!acceptanceReport.Accepted)
                {
                    if (acceptanceReport.Reason.NullOrEmpty())
                    {
                        Messages.Message("NameIsInvalid".Translate(), MessageTypeDefOf.RejectInput, false);
                        return;
                    }
                    Messages.Message(acceptanceReport.Reason, MessageTypeDefOf.RejectInput, false);
                    return;
                }
                else
                {
                    SetName(curName);
                    Find.WindowStack.TryRemove(this, true);
                }
            }
        }

        public string curName;
        public bool focusedRenameField;
    }
}
