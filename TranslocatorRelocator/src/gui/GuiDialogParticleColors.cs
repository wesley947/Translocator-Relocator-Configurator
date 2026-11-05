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
        // Nine distinct, saturated colors ordered from warm to cool, then pink and white.
        // Existing custom values outside this palette are preserved until changed.
        private static readonly string[] PaletteHex =
        {
            ParticleVisualConfig.DefaultGoHome, "#FF8A00", "#FFE000",
            ParticleVisualConfig.DefaultNormal, "#00C8C8", "#0055FF",
            ParticleVisualConfig.DefaultHome, "#FF36B8", "#FFFFFF"
        };

        private static readonly int[] PaletteColors = BuildPickerColors();

        private readonly ParticleVisualConfig settings;
        private int selectedMode;

        public GuiDialogParticleColors(ICoreClientAPI capi) : base("Translocator Particle Effects", capi)
        {
            settings = Core.ParticleVisuals ?? new ParticleVisualConfig();
            settings.Sanitize();

            ElementBounds shade = ElementBounds.FixedSize(370, 308)
                .WithFixedPadding(GuiStyle.ElementToDialogPadding);
            ElementBounds root = ElementStdBounds.AutosizedMainDialog
                .WithAlignment(EnumDialogArea.RightMiddle)
                .WithFixedAlignmentOffset(-GuiStyle.DialogToScreenPadding, 0);

            SingleComposer = capi.Gui.CreateCompo("translocatorparticleeffects", root)
                .AddShadedDialogBG(shade)
                .AddDialogTitleBar("Translocator Particle Effects", OnTitleBarClose)
                .BeginChildElements(shade)
                .AddStaticText("Select particle type", CairoFont.WhiteSmallText(),
                    ElementBounds.Fixed(0, 37, 195, 24))
                .AddDynamicCustomDraw(ElementBounds.Fixed(0, 61, 366, 18),
                    DrawModeBars, "modeBars")
                .AddTextToggleButtons(
                    new[] { "Normal", "Go Home", "Home" }, CairoFont.WhiteSmallText(),
                    OnModeSelected,
                    new[] {
                        ElementBounds.Fixed(0, 84, 115, 28),
                        ElementBounds.Fixed(125, 84, 115, 28),
                        ElementBounds.Fixed(250, 84, 115, 28)
                    }, "particleMode")

                .AddStaticText("Choose color", CairoFont.WhiteSmallText(),
                    ElementBounds.Fixed(0, 126, 195, 24))
                .AddColorListPicker(PaletteColors, OnColorPicked,
                    ElementBounds.Fixed(0, 150, 34, 28), 345, "colorPalette")
                .AddStaticText("Particle flow", CairoFont.WhiteSmallText(),
                    ElementBounds.Fixed(0, 191, 170, 24))
                .AddDynamicText("", CairoFont.WhiteSmallText(),
                    ElementBounds.Fixed(312, 191, 54, 24), "flowValue")
                .AddSlider(OnFlowChanged,
                    ElementBounds.Fixed(0, 218, 366, 28), "flow")
                .AddStaticText("Use Defaults", CairoFont.WhiteSmallText(),
                    ElementBounds.Fixed(0, 268, 190, 26))
                .AddSwitch(OnDefaultsChanged,
                    ElementBounds.Fixed(124, 263, 36, 36), "useDefaults")
                .AddSmallButton("Done", OnDone,
                    ElementBounds.Fixed(281, 266, 84, 28))
                .EndChildElements()
                .Compose();

            SingleComposer.GetSwitch("useDefaults").SetValue(settings.UseDefaults);
            SingleComposer.GetSlider("flow").SetValues(settings.ParticleFlow, 60, 140, 5, "%");
            SingleComposer.ToggleButtonsSetValue("particleMode", selectedMode);
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

            SingleComposer.ToggleButtonsSetValue("particleMode", selectedMode);
            SingleComposer.GetDynamicText("flowValue").SetNewText(
                settings.EffectiveFlow + "%", forceRedraw: true);

            string match = settings.GetColor(selectedMode);
            int selectedIndex = Array.FindIndex(PaletteHex,
                hex => string.Equals(hex, match, StringComparison.OrdinalIgnoreCase));
            SingleComposer.ColorListPickerSetValue("colorPalette", selectedIndex);
            SingleComposer.GetCustomDraw("modeBars")?.Redraw();
        }

        private void OnModeSelected(int mode)
        {
            selectedMode = mode;
            RefreshDisplay();
        }

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
