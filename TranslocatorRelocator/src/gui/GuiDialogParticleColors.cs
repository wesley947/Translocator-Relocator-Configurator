using System;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;
using TranslocatorRelocator.src.config;

namespace TranslocatorRelocator.src.gui
{
    // Client-side appearance editor; no network packets or world changes.
    public class GuiDialogParticleColors : GuiDialogGeneric
    {
        // Rainbow ordering: light, vivid, deep shades, plus pure white.
        private static readonly string[] PaletteHex =
        {
            "#FF6868", "#FFA248", "#FFE563", "#78D66F", "#52C9FF", "#8585FF", "#D68AFF",
            "#FF3C24", "#FF821F", "#FFD024", "#31B85C", "#168ADB", "#5965E9", "#AE4DEB",
            "#D62C37", "#E56300", "#E6AE0E", "#16A064", "#0055FF", "#3B45B8", "#6E37DC",
            "#FFFFFF"
        };

        private static readonly int[] PaletteColors = BuildPickerColors();

        private readonly ParticleVisualConfig settings;
        private int selectedMode;

        public GuiDialogParticleColors(ICoreClientAPI capi) : base("Translocator Particle Effects", capi)
        {
            settings = Core.ParticleVisuals ?? new ParticleVisualConfig();
            settings.Sanitize();

            ElementBounds shade = ElementBounds.FixedSize(390, 397)
                .WithFixedPadding(GuiStyle.ElementToDialogPadding);
            ElementBounds root = ElementStdBounds.AutosizedMainDialog
                .WithAlignment(EnumDialogArea.RightMiddle)
                .WithFixedAlignmentOffset(-GuiStyle.DialogToScreenPadding, 0);

            SingleComposer = capi.Gui.CreateCompo("translocatorparticleeffects", root)
                .AddShadedDialogBG(shade)
                .AddDialogTitleBar("Translocator Particle Effects", OnTitleBarClose)
                .BeginChildElements(shade)
                .AddStaticText("Use default effects", CairoFont.WhiteSmallText(),
                    ElementBounds.Fixed(0, 7, 190, 26))
                .AddSwitch(OnDefaultsChanged,
                    ElementBounds.Fixed(201, 0, 36, 36), "useDefaults")

                .AddStaticText("Select particle type", CairoFont.WhiteSmallText(),
                    ElementBounds.Fixed(0, 46, 195, 24))
                .AddDynamicCustomDraw(ElementBounds.Fixed(0, 70, 366, 18),
                    DrawModeBars, "modeBars")
                .AddSmallButton("Normal", OnNormal,
                    ElementBounds.Fixed(0, 93, 115, 28))
                .AddSmallButton("Go Home", OnGoHome,
                    ElementBounds.Fixed(125, 93, 115, 28))
                .AddSmallButton("Home", OnNetworkHome,
                    ElementBounds.Fixed(250, 93, 115, 28))

                .AddStaticText("Choose color", CairoFont.WhiteSmallText(),
                    ElementBounds.Fixed(0, 135, 195, 24))
                .AddColorListPicker(PaletteColors, OnColorPicked,
                    ElementBounds.Fixed(28, 159, 34, 28), 311, "colorPalette")
                .AddDynamicText("", CairoFont.WhiteSmallText(),
                    ElementBounds.Fixed(0, 261, 366, 24), "currentColor")

                .AddStaticText("Particle flow", CairoFont.WhiteSmallText(),
                    ElementBounds.Fixed(0, 295, 170, 24))
                .AddDynamicText("", CairoFont.WhiteSmallText(),
                    ElementBounds.Fixed(312, 295, 54, 24), "flowValue")
                .AddSlider(OnFlowChanged,
                    ElementBounds.Fixed(0, 322, 366, 28), "flow")
                .AddSmallButton("Done", OnDone,
                    ElementBounds.Fixed(281, 358, 84, 28))
                .EndChildElements()
                .Compose();

            SingleComposer.GetSwitch("useDefaults").SetValue(settings.UseDefaults);
            SingleComposer.GetSlider("flow").SetValues(settings.ParticleFlow, 60, 140, 5, "%");
            RefreshDisplay();
        }

        private static int[] BuildPickerColors()
        {
            var colors = new int[PaletteHex.Length];
            for (int i = 0; i < colors.Length; i++)
            {
                int[] rgb = ParticleVisualConfig.ParseRgb(PaletteHex[i]);
                // ColorListPicker uses ARGB, unlike the particle emission RGBA packing.
                colors[i] = ColorUtil.ToRgba(255, rgb[0], rgb[1], rgb[2]);
            }
            return colors;
        }

        private void DrawModeBars(Context ctx, ImageSurface surface, ElementBounds bounds)
        {
            const double width = 115;
            const double gap = 10;
            for (int i = 0; i < 3; i++)
            {
                int[] rgb = settings.ActiveRgb(i);
                ctx.SetSourceRGB(rgb[0] / 255d, rgb[1] / 255d, rgb[2] / 255d);
                ctx.Rectangle(i * (width + gap), 0, width, 16);
                ctx.Fill();

                if (selectedMode == i)
                {
                    ctx.SetSourceRGB(1, 1, 1);
                    ctx.LineWidth = 1.5;
                    ctx.Rectangle(i * (width + gap) + 0.75, 0.75, width - 1.5, 14.5);
                    ctx.Stroke();
                }
            }
        }

        private void RefreshDisplay()
        {
            if (SingleComposer == null) return;
            bool editing = !settings.UseDefaults;

            var flowSlider = SingleComposer.GetSlider("flow");
            if (flowSlider.Enabled != editing) flowSlider.Enabled = editing;
            flowSlider.SetValue(settings.EffectiveFlow);
            for (int i = 0; i < PaletteHex.Length; i++)
            {
                var swatch = SingleComposer.GetColorListPicker("colorPalette-" + i);
                if (swatch != null) swatch.Enabled = editing;
            }

            string effectiveColor = settings.GetColor(selectedMode);
            string displayName = selectedMode == 1 ? "Go Home" : selectedMode == 2 ? "Network Home" : "Normal";
            string status = settings.UseDefaults ? "Default" : "Custom";
            SingleComposer.GetDynamicText("currentColor").SetNewText(
                displayName + " — " + status + ": " + effectiveColor, forceRedraw: true);
            SingleComposer.GetDynamicText("flowValue").SetNewText(
                settings.EffectiveFlow + "%", forceRedraw: true);

            string match = settings.GetColor(selectedMode);
            int selectedIndex = Array.FindIndex(PaletteHex,
                hex => string.Equals(hex, match, StringComparison.OrdinalIgnoreCase));
            SingleComposer.ColorListPickerSetValue("colorPalette", selectedIndex);
            SingleComposer.GetCustomDraw("modeBars")?.Redraw();
        }

        private bool SelectMode(int mode)
        {
            selectedMode = mode;
            RefreshDisplay();
            return true;
        }

        private bool OnNormal() => SelectMode(0);
        private bool OnGoHome() => SelectMode(1);
        private bool OnNetworkHome() => SelectMode(2);

        private void OnColorPicked(int index)
        {
            if (settings.UseDefaults || index < 0 || index >= PaletteHex.Length) return;
            settings.SetCustomColor(selectedMode, PaletteHex[index]);
            RefreshDisplay();
            Core.SaveParticleVisuals();
        }

        private void OnDefaultsChanged(bool enabled)
        {
            // Switching to defaults NEVER overwrites the remembered custom palette or flow.
            settings.UseDefaults = enabled;
            RefreshDisplay();
            Core.SaveParticleVisuals();
        }

        private bool OnFlowChanged(int newValue)
        {
            if (settings.UseDefaults) return true;
            settings.ParticleFlow = Math.Clamp(newValue, 60, 140);
            RefreshDisplay();
            return true;
        }

        private void OnTitleBarClose() => TryClose();
        private bool OnDone() { TryClose(); return true; }

        public override void OnMouseUp(MouseEvent args)
        {
            base.OnMouseUp(args);
            Core.SaveParticleVisuals();
        }

        public override void OnGuiClosed()
        {
            Core.SaveParticleVisuals();
            base.OnGuiClosed();
        }
    }
}
