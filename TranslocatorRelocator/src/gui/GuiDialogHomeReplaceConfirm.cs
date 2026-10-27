using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace TranslocatorRelocator.src.gui
{
    public class GuiDialogHomeReplaceConfirm : GuiDialogGeneric
    {
        private readonly BlockPos blockPos;
        private readonly string network;
        private readonly string expectedHome;

        public GuiDialogHomeReplaceConfirm(BlockPos pos, string network, string existingHome,
            ICoreClientAPI capi) : base("Replace Network Home?", capi)
        {
            blockPos = pos;
            this.network = network;
            expectedHome = existingHome;

            ElementBounds bg = ElementBounds.FixedSize(430, 145).WithFixedPadding(GuiStyle.ElementToDialogPadding);
            ElementBounds main = ElementStdBounds.AutosizedMainDialog
                .WithAlignment(EnumDialogArea.CenterMiddle);
            SingleComposer = capi.Gui.CreateCompo("translocatorHomeConfirm", main)
                .AddShadedDialogBG(bg)
                .AddDialogTitleBar("Replace Network Home?", () => TryClose())
                .BeginChildElements(bg)
                .AddStaticText("Network: " + network + "  |  Existing Home: " + existingHome,
                    CairoFont.WhiteSmallText(), ElementBounds.Fixed(0, 15, 425, 26))
                .AddStaticText("Replace it? The old Home will become a regular translocator.",
                    CairoFont.WhiteSmallText(), ElementBounds.Fixed(0, 57, 425, 36))
                .AddSmallButton(Lang.Get("Cancel"), OnCancel, ElementBounds.Fixed(0, 110, 90, 30))
                .AddSmallButton("Replace Home", OnReplace, ElementBounds.Fixed(292, 110, 130, 30))
                .EndChildElements()
                .Compose();
        }

        private bool OnCancel() { TryClose(); return true; }
        private bool OnReplace()
        {
            capi.Network.SendBlockEntityPacket(blockPos, 1002, SerializerUtil.Serialize(new EditLinkKeyPacket
            {
                LinkKey = network,
                IsHome = true,
                ReplaceHome = true,
                ExpectedHomePos = expectedHome
            }));
            TryClose();
            return true;
        }
    }
}