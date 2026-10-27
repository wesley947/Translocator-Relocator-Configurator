using Vintagestory.API.Client;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace TranslocatorRelocator.src.gui
{
    public class GuiDialogNetworkRenameInput : GuiDialogGeneric
    {
        private readonly BlockPos blockPos;
        private readonly string oldKey;

        public GuiDialogNetworkRenameInput(BlockPos pos, string oldNetwork, ICoreClientAPI api)
            : base("Rename Entire Network", api)
        {
            blockPos = pos;
            oldKey = oldNetwork;
            ElementBounds background = ElementBounds.FixedSize(365, 153)
                .WithFixedPadding(GuiStyle.ElementToDialogPadding);
            ElementBounds main = ElementStdBounds.AutosizedMainDialog
                .WithAlignment(EnumDialogArea.CenterMiddle);
            SingleComposer = api.Gui.CreateCompo("networkRenameInput", main)
                .AddShadedDialogBG(background)
                .AddDialogTitleBar("Rename Entire Network", () => TryClose())
                .BeginChildElements(background)
                .AddStaticText("Current network: " + oldKey, CairoFont.WhiteSmallText(),
                    ElementBounds.Fixed(0, 15, 340, 25))
                .AddTextInput(ElementBounds.Fixed(0, 48, 310, 28), OnNameChanged,
                    CairoFont.TextInput().WithFontSize(20), "newNetwork")
                .AddSmallButton("Cancel", OnCancel, ElementBounds.Fixed(0, 111, 90, 30))
                .AddSmallButton("Review Rename", OnReview, ElementBounds.Fixed(204, 111, 140, 30))
                .EndChildElements()
                .Compose();
            SingleComposer.GetTextInput("newNetwork").SetMaxLines(1);
            SingleComposer.GetTextInput("newNetwork").SetValue(oldNetwork);
        }

        private void OnNameChanged(string value)
        {
            if (value.Length > 16) SingleComposer.GetTextInput("newNetwork").SetValue(value.Substring(0, 16));
        }
        private bool OnCancel() { TryClose(); return true; }
        private bool OnReview()
        {
            string newName = SingleComposer.GetTextInput("newNetwork").GetText().Trim();
            if (newName.Length == 0 || newName == oldKey) return true;
            capi.Network.SendBlockEntityPacket(blockPos, 1004, SerializerUtil.Serialize(new NetworkRenamePacket
            {
                OldKey = oldKey, NewKey = newName
            }));
            TryClose();
            return true;
        }
    }
}