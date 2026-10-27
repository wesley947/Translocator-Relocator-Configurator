using System;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace TranslocatorRelocator.src.gui
{
    public class GuiDialogLinkKeyInput : GuiDialogGeneric
    {
        private BlockPos blockEntityPos;

        const int MAX_KEY_LENGHT = 16;
        private bool makeHome;
        private readonly string originalNetwork;

        public GuiDialogLinkKeyInput(string DialogTitle, BlockPos pos, string linkKey, bool isHome, string[] networkNames, ICoreClientAPI capi) : base(DialogTitle, capi)
        {
            blockEntityPos = pos;
            makeHome = isHome;
            originalNetwork = linkKey;
            // First choice is only a label. Choosing a real network copies its name
            // into the original editable field; players can still type any name.
            string[] available = (networkNames ?? Array.Empty<string>())
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            string[] choices = new string[available.Length + 1];
            choices[0] = "";
            Array.Copy(available, 0, choices, 1, available.Length);
            string[] displayNames = (string[])choices.Clone();
            displayNames[0] = "Networks...";
            ElementBounds textInputBounds = ElementBounds.Fixed(0.0, 0.0, (MAX_KEY_LENGHT*10)+26, 28);
            ElementBounds clipBounds1 = textInputBounds.ForkBoundingParent().WithFixedPosition(0.0, 30.0);
            ElementBounds cancelButtonBounds = ElementBounds.FixedSize(0.0, 0.0).FixedUnder(clipBounds1, isHome ? 97.0 : 55.0).WithAlignment(EnumDialogArea.LeftFixed).WithFixedPadding(8.0, 2.0).WithFixedAlignmentOffset(-1.0, 0.0);
            ElementBounds saveButtonBounds = ElementBounds.FixedSize(0.0, 0.0).FixedUnder(clipBounds1, isHome ? 97.0 : 55.0).WithAlignment(EnumDialogArea.RightFixed).WithFixedPadding(10.0, 2.0).WithFixedAlignmentOffset(6, 0);
            ElementBounds shadeElementBounds = ElementBounds.FixedSize(350, isHome ? 177 : 135).WithFixedPadding(GuiStyle.ElementToDialogPadding);
            ElementBounds mainBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.RightMiddle).WithFixedAlignmentOffset(0.0 - GuiStyle.DialogToScreenPadding, 0.0);
            CairoFont cairoFont = CairoFont.TextInput().WithFontSize(20).WithFont("Consolas");
            cairoFont.LineHeightMultiplier = 0.9;
            var composer = capi.Gui.CreateCompo("blockentitytexteditordialog", mainBounds)
                .AddShadedDialogBG(shadeElementBounds)
                .AddDialogTitleBar(DialogTitle, OnTitleBarClose)
                .BeginChildElements(shadeElementBounds)
                .BeginClip(clipBounds1)
                .AddTextInput(textInputBounds, OnTextInputChanged, cairoFont, "linkKey")
                .EndClip()
                .AddDropDown(choices, displayNames, 0, OnNetworkChosen, ElementBounds.Fixed(199, 30, 128, 28), "existingNetworks")
                .AddStaticText("Set as Home", CairoFont.WhiteSmallText(), ElementBounds.Fixed(0, 69, 128, 25))
                .AddSwitch(value => makeHome = value, ElementBounds.Fixed(140, 64, 36, 36), "makeHome");
            if (isHome)
                composer.AddSmallButton("Rename Network", OnRenameNetwork,
                    ElementBounds.Fixed(0, 112, 169, 27));
            base.SingleComposer = composer
                .AddSmallButton(Lang.Get("Cancel"), OnButtonCancel, cancelButtonBounds)
                .AddSmallButton(Lang.Get("Save"), OnButtonSave, saveButtonBounds)
                .EndChildElements()
                .Compose();
            base.SingleComposer.GetTextInput("linkKey").SetMaxHeight(100);
            base.SingleComposer.GetTextInput("linkKey").SetMaxLines(1);
            base.SingleComposer.GetSwitch("makeHome").SetValue(isHome);

            if (linkKey.Length > 0)
            {
                base.SingleComposer.GetTextInput("linkKey").SetValue(linkKey);
            }
        }

        private void OnTitleBarClose()
        {
            OnButtonCancel();
        }

        private void OnNetworkChosen(string networkName, bool selected)
        {
            if (!selected || string.IsNullOrEmpty(networkName) || SingleComposer == null) return;
            SingleComposer.GetTextInput("linkKey")?.SetValue(networkName);
        }

        private void OnTextInputChanged(string value)
        {
            if (value.Length > MAX_KEY_LENGHT)
            {
                base.SingleComposer.GetTextInput("linkKey").SetValue(value.Remove(MAX_KEY_LENGHT));
            }
        }

        private bool OnButtonSave()
        {
            string linkKey = base.SingleComposer.GetTextInput("linkKey").GetText();
            OnSave(linkKey);
            TryClose();
            return true;
        }

        private bool OnRenameNetwork()
        {
            TryClose();
            new GuiDialogNetworkRenameInput(blockEntityPos, originalNetwork, capi).TryOpen();
            return true;
        }

        private bool OnButtonCancel()
        {
            TryClose();
            return true;
        }

        public override void OnKeyDown(KeyEvent args)
        {
            if (args.KeyCode == ((int)GlKeys.Enter) && base.SingleComposer.GetTextInput("linkKey").HasFocus)
            {
                OnButtonSave();
                return;
            }
            base.OnKeyDown(args);
        }

        private void OnSave(string linkKey)
        {
            byte[] data = SerializerUtil.Serialize(new EditLinkKeyPacket
            {
                LinkKey = linkKey,
                IsHome = makeHome,
            });
            capi.Network.SendBlockEntityPacket(blockEntityPos, 1002, data);
        }
    }
}
