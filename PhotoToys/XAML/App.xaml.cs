using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using Windows.UI;
namespace PhotoToys;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application
{
    public static Style CardBorderStyle => (Style)Current.Resources["CardBorderStyle"];
    public static Style CardControlStyle => (Style)Current.Resources["CardControlStyle"];
    public static Style TitleTextBlockStyle => (Style)Current.Resources["TitleTextBlockStyle"];
    public static Style AccentButtonStyle => (Style)Current.Resources["AccentButtonStyle"];
    public static Style GridViewItemContainerStyle => (Style)Current.Resources["GridViewItemContainerStyle"];
    public static Style SubtitleTextBlockStyle => (Style)Current.Resources["SubtitleTextBlockStyle"];
    public static Style GridViewWrapItemsPanelTemplateStyle => (Style)Current.Resources["GridViewItemsPanelTemplate"];
    public static Style BodyStrongTextBlockStyle => (Style)Current.Resources["BodyStrongTextBlockStyle"];
    public static Style BodyTextBlockStyle => (Style)Current.Resources["BodyTextBlockStyle"];
    public static Style CaptionTextBlockStyle => (Style)Current.Resources["CaptionTextBlockStyle"];
    public static Color SolidBackgroundFillColorBase => (Color)Current.Resources["SolidBackgroundFillColorBase"];
    public static Brush CardStrokeColorDefaultBrush => (Brush)Current.Resources["CardStrokeColorDefaultBrush"];
    public static Brush CardBackgroundFillColorDefaultBrush => (Brush)Current.Resources["CardBackgroundFillColorDefaultBrush"];
    public static Color LayerFillColorDefaultColor => (Color)Current.Resources["LayerFillColorDefault"];
    public static SvgImageSource Logo => (SvgImageSource)Current.Resources["Logo"];
    public App()
    {
        InitializeComponent();
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
            //Resources.ThemeDictionaries.Add("Default", new ResourceDictionary
            //{
            //    ["ContentControlThemeFontFamily"] = Font,
            //    ["TextBlockDefaultFontOverridea"] = new Style
            //    {
            //        TargetType = typeof(Microsoft.UI.Xaml.Controls.TextBlock),
            //        Setters =
            //        {
            //            new Setter(Microsoft.UI.Xaml.Controls.TextBlock.FontFamilyProperty, Font)
            //        }
            //    }
            //});
            //Resources.Add(typeof(Microsoft.UI.Xaml.Controls.Control), new Style
            //{
            //    TargetType = typeof(Microsoft.UI.Xaml.Controls.Control),
            //    Setters =
            //    {
            //        new Setter
            //        {
            //            Property = Microsoft.UI.Xaml.Controls.Control.FontFamilyProperty,
            //            Value = Font
            //        }
            //    }
            //});
        }
        await Inventory.InitializeAsync();
        Window = new MainWindow();
        Window.Activate();
    }

    static Window? Window;
    public static Window CurrentWindow => Window ?? throw new NullReferenceException();
    public static IntPtr CurrentWindowHandle => WinRT.Interop.WindowNative.GetWindowHandle(Window);
}
