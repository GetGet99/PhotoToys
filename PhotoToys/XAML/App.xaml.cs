using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Linq;
using Windows.UI;
namespace PhotoToys;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application
{
    static (FontFamily FontFamily, double FontSizeMultiplier)? DefaultFont = DynamicLanguage.SystemLanguage.Font;
    public static Style CardBorderStyle => (Style)Current.Resources["CardBorderStyle"];
    public static Style CardControlStyle => (Style)Current.Resources["CardControlStyle"];
    public static Style AccentButtonStyle => (Style)Current.Resources["AccentButtonStyle"];
    public static Style GridViewItemContainerStyle => (Style)Current.Resources["GridViewItemContainerStyle"];
    public static Style GridViewWrapItemsPanelTemplateStyle => (Style)Current.Resources["GridViewItemsPanelTemplate"];
    public static Style TitleTextBlockStyle { get; } = NewTextBlockStyle((Style)Current.Resources["TitleTextBlockStyle"]);
    public static Style SubtitleTextBlockStyle { get; } = NewTextBlockStyle((Style)Current.Resources["SubtitleTextBlockStyle"]);
    public static Style BodyStrongTextBlockStyle { get; } = NewTextBlockStyle((Style)Current.Resources["BodyStrongTextBlockStyle"]);
    public static Style BodyTextBlockStyle { get; } = NewTextBlockStyle((Style)Current.Resources["BodyTextBlockStyle"]);
    public static Style CaptionTextBlockStyle { get; } = NewTextBlockStyle((Style)Current.Resources["CaptionTextBlockStyle"]);
    public static Color SolidBackgroundFillColorBase => (Color)Current.Resources["SolidBackgroundFillColorBase"];
    public static Brush CardStrokeColorDefaultBrush => (Brush)Current.Resources["CardStrokeColorDefaultBrush"];
    public static Brush CardBackgroundFillColorDefaultBrush => (Brush)Current.Resources["CardBackgroundFillColorDefaultBrush"];
    public static Color LayerFillColorDefaultColor => (Color)Current.Resources["LayerFillColorDefault"];
    public static SvgImageSource Logo => (SvgImageSource)Current.Resources["Logo"];
    public App()
    {
        InitializeComponent();
    }
    static Style NewTextBlockStyle(Style OldStyle)
    {
        if (DefaultFont is null) return OldStyle;
        var (FontFamily, FontSizeMultiplier) = DefaultFont.Value;
        var FamilySetter = new Setter(Microsoft.UI.Xaml.Controls.Control.FontFamilyProperty, FontFamily);

        var Style = new Style
        {
            TargetType = typeof(Microsoft.UI.Xaml.Controls.TextBlock),
            Setters =
            {
                FamilySetter
            }
        };
        Style.Setters.AddRange(OldStyle.Setters.Select(x =>
        {
            if (x is not Setter Setter) return x;
            else if (Setter.Property == Microsoft.UI.Xaml.Controls.TextBlock.FontSizeProperty)
            {
                return new Setter
                {
                    Property = Setter.Property,
                    Value = (double)Setter.Value * FontSizeMultiplier
                };
            }
            else if (Setter.Property == Microsoft.UI.Xaml.Controls.TextBlock.FontFamilyProperty)
            {
                return FamilySetter;
            }
            return x;
        }));
        return Style;
    }
    /// <summary>
    /// Invoked when the application is launched normally by the end user.  Other entry points
    /// will be used such as when the application is launched to open a specific file.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        var Font = DynamicLanguage.SystemLanguage.Font;
        if (Font is not null)
        {
            var (FontFamily, FontSize) = Font.Value;
            //Resources.Values
            var FamilySetter = new Setter(Microsoft.UI.Xaml.Controls.Control.FontFamilyProperty, FontFamily);
            var SizeSetter = new Setter(Microsoft.UI.Xaml.Controls.Control.FontSizeProperty, 16 * FontSize);
            //SetterFont2.Value = FontFamily;
            Resources[typeof(Microsoft.UI.Xaml.Controls.NavigationViewItem)] = new Style
            {
                TargetType = typeof(Microsoft.UI.Xaml.Controls.NavigationViewItem),
                Setters =
                {
                    FamilySetter,
                    SizeSetter
                }
            };
            Resources[typeof(Microsoft.UI.Xaml.Controls.TextBlock)] = new Style
            {
                TargetType = typeof(Microsoft.UI.Xaml.Controls.TextBlock),
                Setters =
                {
                    FamilySetter,
                    SizeSetter
                }
            };
            Style a;
            a = TitleTextBlockStyle;
            a.Setters.Add(FamilySetter);
            a = SubtitleTextBlockStyle;
            a.Setters.Add(FamilySetter);
            a = BodyStrongTextBlockStyle;
            a.Setters.Add(FamilySetter);
            a = BodyTextBlockStyle;
            a.Setters.Add(FamilySetter);
            a = CaptionTextBlockStyle;
            a.Setters.Add(FamilySetter);
        }
        await Inventory.InitializeAsync();
        Window = new MainWindow();
        Window.Activate();
    }

    static Window? Window;
    public static Window CurrentWindow => Window ?? throw new NullReferenceException();
    public static IntPtr CurrentWindowHandle => WinRT.Interop.WindowNative.GetWindowHandle(Window);
}
