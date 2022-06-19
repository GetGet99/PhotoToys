using Microsoft.UI.Xaml;
//using Microsoft.UI.Xaml.Media;
using System;
using Windows.UI;
namespace PhotoToys;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application
{
    public static Style LayeringBackgroundBorderStyle => (Style)Current.Resources["LayeringBackgroundBorder"];
    public static Style LayeringBackgroundControlStyle => (Style)Current.Resources["LayeringBackgroundControl"];
    public static Style TitleTextBlockStyle => (Style)Current.Resources["TitleTextBlockStyle"];
    public static Style AccentButtonStyle => (Style)Current.Resources["AccentButtonStyle"];
    public static Style GridViewItemContainerStyle => (Style)Current.Resources["GridViewItemContainerStyle"];
    public static Style SubtitleTextBlockStyle => (Style)Current.Resources["SubtitleTextBlockStyle"];
    public static Style GridViewWrapItemsPanelTemplateStyle => (Style)Current.Resources["GridViewItemsPanelTemplate"];
    public static Style BodyStrongTextBlockStyle => (Style)Current.Resources["BodyStrongTextBlockStyle"];
    public static Style CaptionTextBlockStyle => (Style)Current.Resources["CaptionTextBlockStyle"];
    public static Color SolidBackgroundFillColorBase => (Color)Current.Resources["SolidBackgroundFillColorBase"];
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
        await Inventory.InitializeAsync();
        Window = new MainWindow();
        Window.Activate();
    }

    static Window? Window;
    public static Window CurrentWindow => Window ?? throw new NullReferenceException();
    public static IntPtr CurrentWindowHandle => WinRT.Interop.WindowNative.GetWindowHandle(Window);
}
