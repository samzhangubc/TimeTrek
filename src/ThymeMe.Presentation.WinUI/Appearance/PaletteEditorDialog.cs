using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ThymeMe.Domain.Appearance;

namespace ThymeMe.Presentation.WinUI.Appearance;

public sealed record PaletteEditorResult(string Name, PaletteVariant Light, PaletteVariant Dark)
{
    public double LightInteractiveContrast => ContrastPolicy.Ratio(Light.Accent, Light.Surface);

    public double DarkInteractiveContrast => ContrastPolicy.Ratio(Dark.Accent, Dark.Surface);

    public bool NeedsContrastAcknowledgement => LightInteractiveContrast < 3 || DarkInteractiveContrast < 3;
}

public static class PaletteEditorDialog
{
    public static async ValueTask<PaletteEditorResult?> ShowAsync(XamlRoot xamlRoot, AppearancePalette? existing = null)
    {
        TextBox name = new() { Header = "Palette name", MaxLength = 200, Text = existing?.Name ?? "My palette" };
        StackPanel fields = new() { Spacing = 10 };
        fields.Children.Add(name);
        TextBlock guidance = new()
        {
            Text = "Each variant uses Canvas, Surface, and Accent. Enter strict #RRGGBB colors or use the color pickers.",
            TextWrapping = TextWrapping.Wrap,
        };
        fields.Children.Add(guidance);

        ColorField lightCanvas = AddColorField(fields, "Light Canvas", existing?.Light.Canvas ?? "#FFFFFF");
        ColorField lightSurface = AddColorField(fields, "Light Surface", existing?.Light.Surface ?? "#F6F8FA");
        ColorField lightAccent = AddColorField(fields, "Light Accent", existing?.Light.Accent ?? "#0969DA");
        ColorField darkCanvas = AddColorField(fields, "Dark Canvas", existing?.Dark.Canvas ?? "#0D1117");
        ColorField darkSurface = AddColorField(fields, "Dark Surface", existing?.Dark.Surface ?? "#161B22");
        ColorField darkAccent = AddColorField(fields, "Dark Accent", existing?.Dark.Accent ?? "#58A6FF");
        TextBlock contrast = new() { TextWrapping = TextWrapping.Wrap };
        fields.Children.Add(contrast);

        void UpdateContrast()
        {
            try
            {
                PaletteEditorResult preview = BuildResult();
                contrast.Text = $"Interactive contrast — Light: {preview.LightInteractiveContrast:F2}:1; Dark: {preview.DarkInteractiveContrast:F2}:1. Values below 3:1 require acknowledgement.";
            }
            catch (Exception)
            {
                contrast.Text = "Enter six valid #RRGGBB colors to calculate contrast.";
            }
        }

        PaletteEditorResult BuildResult() => new(
            name.Text,
            new PaletteVariant(lightCanvas.Text.Text, lightSurface.Text.Text, lightAccent.Text.Text).Validate(),
            new PaletteVariant(darkCanvas.Text.Text, darkSurface.Text.Text, darkAccent.Text.Text).Validate());

        foreach (ColorField field in new[] { lightCanvas, lightSurface, lightAccent, darkCanvas, darkSurface, darkAccent })
        {
            field.Text.TextChanged += (_, _) => UpdateContrast();
        }
        UpdateContrast();

        ScrollViewer scroller = new()
        {
            Content = fields,
            MaxHeight = 560,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
        };
        ContentDialog dialog = new()
        {
            XamlRoot = xamlRoot,
            Title = existing is null ? "Create custom palette" : "Edit custom palette",
            Content = scroller,
            PrimaryButtonText = "Save palette",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
        };
        if (await dialog.ShowAsync() != ContentDialogResult.Primary)
        {
            return null;
        }

        return BuildResult();
    }

    private static ColorField AddColorField(StackPanel parent, string label, string initial)
    {
        TextBox text = new() { Header = label, Text = initial, MaxLength = 7 };
        ColorPicker picker = new()
        {
            Color = AppearanceRuntime.Parse(initial),
            IsAlphaEnabled = false,
            IsAlphaSliderVisible = false,
            IsAlphaTextInputVisible = false,
            IsMoreButtonVisible = false,
            MinWidth = 280,
        };
        picker.ColorChanged += (_, args) => text.Text = $"#{args.NewColor.R:X2}{args.NewColor.G:X2}{args.NewColor.B:X2}";
        text.LostFocus += (_, _) =>
        {
            try
            {
                picker.Color = AppearanceRuntime.Parse(text.Text);
            }
            catch (Exception)
            {
                // Validation is reported when the user attempts to save.
            }
        };
        Expander expander = new() { Header = label, Content = picker };
        parent.Children.Add(text);
        parent.Children.Add(expander);
        return new ColorField(text, picker);
    }

    private sealed record ColorField(TextBox Text, ColorPicker Picker);
}
