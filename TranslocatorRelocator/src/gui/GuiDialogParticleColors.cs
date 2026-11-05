using System;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;
using TranslocatorRelocator.src.config;

namespace TranslocatorRelocator.src.gui
{
    // Separate, client-side appearance editor. The existing network dialog stays compact.
    public class GuiDialogParticleColors : GuiDialogGeneric
    {
        private readonly ParticleVisualConfig settings;
        private readonly ElementBounds paletteBounds;
        private int selectedMode;
        private double hue;
        private double saturation;
        private double value;
        private bool draggingPalette;

        public GuiDialogParticleColors(ICoreClientAPI capi) : base("Translocator Particle Effects", capi)
        {
            settings = Core.ParticleVisuals ?? new ParticleVisualConfig();
            settings.Sanitize();
            SetEditorFromCustomColor();

            ElementBounds shade = ElementBounds.FixedSize(410, 473)
                .WithFixedPadding(GuiStyle.ElementToDialogPadding);
            ElementBounds root = ElementStdBounds.AutosizedMainDialog
                .WithAlignment(EnumDialogArea.LeftMiddle)
                .WithFixedAlignmentOffset(GuiStyle.DialogToScreenPadding, 0);
            paletteBounds = ElementBounds.Fixed(0, 147, 374, 140);

            SingleComposer = capi.Gui.CreateCompo("translocatorparticleeffects", root)
                .AddShadedDialogBG(shade)
                .AddDialogTitleBar("Translocator Particle Effects", OnTitleBarClose)
                .BeginChildElements(shade)
                .AddStaticText("Use default effects", CairoFont.WhiteSmallText(),
                    ElementBounds.Fixed(0, 8, 185, 26))
                .AddSwitch(OnDefaultsChanged, ElementBounds.Fixed(201, 0, 36, 36), "useDefaults")
                .AddStaticText("Choose a particle type", CairoFont.WhiteSmallText(),
                    ElementBounds.Fixed(0, 49, 187, 23))
                .AddDropDown(
                    new[] { "normal", "gohome", "home" },
                    new[] { "Normal / Random", "Go Home", "Network Home" },
                    0, OnModeChanged, ElementBounds.Fixed(190, 45, 184, 28), "particleMode")
                .AddStaticText("Hue", CairoFont.WhiteSmallText(),
                    ElementBounds.Fixed(0, 102, 67, 24))
                .AddSlider(OnHueChanged, ElementBounds.Fixed(82, 97, 291, 28), "hue")
                .AddDynamicCustomDraw(paletteBounds, DrawPalette, "colorPalette")
                .AddDynamicText("", CairoFont.WhiteSmallText(),
                    ElementBounds.Fixed(0, 295, 374, 25), "currentColor")
                .AddStaticText("Particle flow", CairoFont.WhiteSmallText(),
                    ElementBounds.Fixed(0, 339, 145, 24))
                .AddSlider(OnFlowChanged, ElementBounds.Fixed(158, 334, 216, 28), "flow")
                .AddStaticText("Emission intensity", CairoFont.WhiteSmallText(),
                    ElementBounds.Fixed(0, 386, 155, 24))
                .AddSlider(OnEmissionChanged, ElementBounds.Fixed(158, 381, 216, 28), "emission")
                .AddSmallButton("Done", OnDone, ElementBounds.Fixed(290, 431, 84, 28))
                .EndChildElements()
                .Compose();

            SingleComposer.GetSwitch("useDefaults").SetValue(settings.UseDefaults);
            SingleComposer.GetSlider("hue").SetValues((int)Math.Round(hue), 0, 359, 1, "°");
            SingleComposer.GetSlider("flow").SetValues(settings.ParticleFlow, 60, 140, 5, "%");
            SingleComposer.GetSlider("emission").SetValues(settings.EmissionIntensity, 50, 100, 5, "%");
            RefreshDisplay();
        }

        private void OnTitleBarClose() => TryClose();
        private bool OnDone() { TryClose(); return true; }

        public override void OnGuiClosed()
        {
            draggingPalette = false;
            Core.SaveParticleVisuals();
            base.OnGuiClosed();
        }

        private void SetEditorFromCustomColor()
        {
            ParticleVisualConfig.RgbToHsv(settings.GetCustomColor(selectedMode),
                out hue, out saturation, out value);
        }

        private void RefreshDisplay()
        {
            if (SingleComposer == null) return;
            bool editing = !settings.UseDefaults;
            SingleComposer.GetSlider("hue").Enabled = editing;
            SingleComposer.GetSlider("flow").Enabled = editing;
            SingleComposer.GetSlider("emission").Enabled = editing;

            string label = settings.UseDefaults
                ? "Default color: " + settings.GetColor(selectedMode) + "   (custom values preserved)"
                : "Custom color: " + settings.GetCustomColor(selectedMode);
            SingleComposer.GetDynamicText("currentColor").SetNewText(label, forceRedraw: true);
            SingleComposer.GetCustomDraw("colorPalette")?.Redraw();
        }

        private void OnDefaultsChanged(bool enabled)
        {
            settings.UseDefaults = enabled;
            RefreshDisplay();
            Core.SaveParticleVisuals();
        }

        private void OnModeChanged(string mode, bool selected)
        {
            if (!selected) return;
            selectedMode = mode == "gohome" ? 1 : mode == "home" ? 2 : 0;
            SetEditorFromCustomColor();
            SingleComposer.GetSlider("hue").SetValue((int)Math.Round(hue));
            RefreshDisplay();
        }

        private bool OnHueChanged(int newHue)
        {
            if (settings.UseDefaults) return true;
            hue = newHue;
            UpdateCustomColor();
            return true;
        }

        private bool OnFlowChanged(int newFlow)
        {
            if (settings.UseDefaults) return true;
            settings.ParticleFlow = Math.Clamp(newFlow, 60, 140);
            return true;
        }

        private bool OnEmissionChanged(int newIntensity)
        {
            if (settings.UseDefaults) return true;
            settings.EmissionIntensity = Math.Clamp(newIntensity, 50, 100);
            return true;
        }

        private void UpdateCustomColor()
        {
            settings.SetCustomColor(selectedMode, ParticleVisualConfig.HsvHex(hue, saturation, value));
            RefreshDisplay();
            // Appearance changes immediately. We only write the JSON on mouse release / closing.
        }

        // 2D continuously-selectable saturation/value field at the chosen hue.
        private void DrawPalette(Context ctx, ImageSurface surface, ElementBounds bounds)
        {
            const int columns = 50;
            const int rows = 22;
            double width = bounds.OuterWidthInt;
            double height = bounds.OuterHeightInt;
            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < columns; x++)
                {
                    double s = (x + 0.5) / columns;
                    double v = 1 - (y + 0.5) / rows;
                    int[] rgb = ParticleVisualConfig.ParseRgb(ParticleVisualConfig.HsvHex(hue, s, v));
                    ctx.SetSourceRGB(rgb[0] / 255d, rgb[1] / 255d, rgb[2] / 255d);
                    ctx.Rectangle(width * x / columns, height * y / rows,
                        width / columns + 1, height / rows + 1);
                    ctx.Fill();
                }
            }

            ctx.SetSourceRGB(0, 0, 0);
            ctx.LineWidth = 3;
            ctx.Arc(saturation * width, (1 - value) * height, 6, 0, 2 * Math.PI);
            ctx.Stroke();
            ctx.SetSourceRGB(1, 1, 1);
            ctx.LineWidth = 1.5;
            ctx.Arc(saturation * width, (1 - value) * height, 6, 0, 2 * Math.PI);
            ctx.Stroke();
        }

        private void PickPalette(int x, int y)
        {
            double width = Math.Max(1, paletteBounds.OuterWidth);
            double height = Math.Max(1, paletteBounds.OuterHeight);
            saturation = Math.Clamp((x - paletteBounds.absX) / width, 0, 1);
            value = Math.Clamp(1 - (y - paletteBounds.absY) / height, 0, 1);
            UpdateCustomColor();
        }

        public override void OnMouseDown(MouseEvent args)
        {
            if (!settings.UseDefaults && args.Button == EnumMouseButton.Left &&
                paletteBounds.PointInside(args.X, args.Y))
            {
                draggingPalette = true;
                PickPalette(args.X, args.Y);
                args.Handled = true;
                return;
            }
            base.OnMouseDown(args);
        }

        public override void OnMouseMove(MouseEvent args)
        {
            if (draggingPalette)
            {
                PickPalette(args.X, args.Y);
                args.Handled = true;
                return;
            }
            base.OnMouseMove(args);
        }

        public override void OnMouseUp(MouseEvent args)
        {
            if (draggingPalette)
            {
                draggingPalette = false;
                PickPalette(args.X, args.Y);
                Core.SaveParticleVisuals();
                args.Handled = true;
                return;
            }
            // Also persist changes made by the two built-in sliders.
            Core.SaveParticleVisuals();
            base.OnMouseUp(args);
        }
    }
}
