using Vintagestory.API.Client;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace TranslocatorRelocator.src.gui
{
    public class GuiDialogNetworkRenameConfirm : GuiDialogGeneric
    {
        private readonly BlockPos blockPos;
        private readonly NetworkRenamePacket rename;

        public GuiDialogNetworkRenameConfirm(BlockPos pos, NetworkRenamePacket details, ICoreClientAPI api)
            : base("Confirm Network Rename", api)
        {
            blockPos = pos;
            rename = details;
            ElementBounds background = ElementBounds.FixedSize(440, 158)
                .WithFixedPadding(GuiStyle.ElementToDialogPadding);
            ElementBounds main = ElementStdBounds.AutosizedMainDialog
                .WithAlignment(EnumDialogArea.CenterMiddle);
            SingleComposer = api.Gui.CreateCompo("networkRenameConfirm", main)
                .AddShadedDialogBG(background)
                .AddDialogTitleBar("Confirm Network Rename", () => TryClose())
                .BeginChildElements(background)
                .AddStaticText("Rename '" + rename.OldKey + "' to '" + rename.NewKey + "'?",
                    CairoFont.WhiteSmallText(), ElementBounds.Fixed(0, 15, 425, 32))
                .AddStaticText("This will update " + rename.ExpectedCount + " translocators, including unloaded areas.",
                    CairoFont.WhiteSmallText(), ElementBounds.Fixed(0, 58, 425, 35))
                .AddSmallButton("Cancel", OnCancel, ElementBounds.Fixed(0, 116, 100, 30))
                .AddSmallButton("Rename All", OnConfirm, ElementBounds.Fixed(303, 116, 120, 30))
                .EndChildElements()
                .Compose();
        }
        private bool OnCancel() { TryClose(); return true; }
        private bool OnConfirm()
        {
            capi.Network.SendBlockEntityPacket(blockPos, 1006, SerializerUtil.Serialize(rename));
            TryClose();
            return true;
        }
    }
}