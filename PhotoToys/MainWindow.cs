using System;
using System.Linq;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Media.Animation;
using PVirtualKey = PInvoke.User32.VirtualKey;
using Microsoft.UI.Xaml.Media;
using System.Windows.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using PInvoke;
using Windows.ApplicationModel;
using System.Runtime.InteropServices;

namespace PhotoToys;

class MainWindow : MicaWindow
{
    public MainWindow()
    {
        #region UI Initialization
        Title = "PhotoToys (Beta)";
        ExtendsContentIntoTitleBar = true;
        NavigationViewItem? InventoryNavigationViewItem = null;

        Content = new Grid
        {
            RowDefinitions = {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition()
            },
            Children =
            {
                #region TitleBar
                new Border
                {
                    Height = 30,
                    Child = new Grid
                    {
                        ColumnDefinitions = {
                            new ColumnDefinition { Width = GridLength.Auto },
                            new ColumnDefinition()
                        },
                        Children =
                        {
                            new Border
                            {
                                Child = new Image
                                {
                                    Margin = new Thickness(8, 0, 0, 0),
                                    Width = 16,
                                    Height = 16,
                                    VerticalAlignment = VerticalAlignment.Center,
                                    Source = new BitmapImage(new Uri("ms-appx:///Assets/PhotoToys.png"))
                                }
                            }.Edit(x => x.PointerPressed += delegate
                            {
                                // Reference https://stackoverflow.com/questions/12761169/send-keys-through-sendinput-in-user32-dll
                                static void MultiKeyPress(params PVirtualKey[] keys){
                                    User32.INPUT[] inputs = new User32.INPUT[keys.Count() * 2];
                                    for(int a = 0; a < keys.Count(); ++a){
                                        for(int b = 0; b < 2; ++b){
                                            inputs[(b == 0) ? a : inputs.Count() - 1 - a].type = User32.InputType.INPUT_KEYBOARD;
                                            inputs[(b == 0) ? a : inputs.Count() - 1 - a].Inputs.ki = new User32.KEYBDINPUT {
                                                wVk = keys[a],
                                                wScan = 0,
                                                dwFlags = b is 0 ? 0 : User32.KEYEVENTF.KEYEVENTF_KEYUP,
                                                time = 0,
                                            };
                                        }
                                    }
                                    if (User32.SendInput(inputs.Length, inputs, Marshal.SizeOf(typeof(User32.INPUT))) == 0)
                                        throw new Exception();
                                }
                                MultiKeyPress(PVirtualKey.VK_MENU, PVirtualKey.VK_SPACE);
                            }),
                            new Border
                            {
                                Child = new StackPanel
                                {
                                    Orientation = Orientation.Horizontal,
                                    VerticalAlignment = VerticalAlignment.Center,
                                    Children =
                                    {
                                        new TextBlock
                                        {
                                            Text = "PhotoToys",
                                            Style = App.CaptionTextBlockStyle,
                                            Margin = new Thickness(4, 0, 0, 0)
                                        },
                                        new TextBlock
                                        {
                                            Text = "BETA",
                                            FontSize = 10,
                                            VerticalAlignment = VerticalAlignment.Bottom,
                                            Foreground = new SolidColorBrush { Color = Microsoft.UI.Colors.DarkGray },
                                            Margin = new Thickness(8, 0, 0, 0)
                                        }
                                    }
                                }
                            }.Edit(x => {
                                Grid.SetColumn(x, 1);
                                SetTitleBar(x);
                            })
                        }
                    }
                },
                #endregion
                #region NavigationAndContent
                new NavigationView
                {
                    IsBackEnabled = false,
                    IsBackButtonVisible = NavigationViewBackButtonVisible.Collapsed,
                    IsPaneToggleButtonVisible = false,
                    IsSettingsVisible = false,
                    Content = new Frame
                    {
                        Margin = new Thickness(0, 0, -30, 0),
                    }.Assign(out var MainFrame),
                    #region AutoSuggestBox
                    AutoSuggestBox = new AutoSuggestBox
                    {
                        QueryIcon = new SymbolIcon(Symbol.Find),
                        ItemsSource = Features.Features.AllSearchQueries,
                        PlaceholderText = "Search"
                    }
                    .Edit(x => AutomationProperties.SetName(x, "Search"))
                    .Edit(autoSuggestBox =>
                        autoSuggestBox.TextChanged += (o, e) =>
                        {
                            if (e.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
                                autoSuggestBox.ItemsSource =
                                    Features.Features.AllSearchQueries
                                    .Append(new Features.FeatureSearchQuery
                                    {
                                        SearchQuery = "Inventory"
                                    })
                                    .Where(x =>
                                        x.SearchQuery.ToLower().Contains(autoSuggestBox.Text.ToLower())
                                    );
                        }
                    )
                    .Assign(out var AutoSuggestBox),
                    #endregion
                    ContentTransitions = {
                        new ContentThemeTransition()
                    },
                    #region Header
                    Header = new SimpleUI.FluentVerticalStack
                    {
                        Margin = new Thickness(-26, 0, 0, 0),
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        VerticalAlignment = VerticalAlignment.Top,
                        Children =
                            {
                                new TextBlock
                                {
                                    Style = App.TitleTextBlockStyle,
                                    Text = ""
                                }.Assign(out var NavigationHeaderTitle),
                                new TextBlock
                                {
                                    Style = App.BodyTextBlockStyle,
                                    Text = "",
                                    Visibility = Visibility.Collapsed,
                                }.Assign(out var NavigationHeaderSubtitle)
                            }
                    }
        #endregion
    }
                .Edit(nav => Grid.SetRow(nav, 1))
                .Edit(nav => nav.MenuItems.Add(
                    #region HomePage
                    new NavigationViewItem
                    {
                        Content = "Home",
                        Icon = new SymbolIcon(Symbol.Home),
                        IsSelected = true,
                        Tag = new ScrollViewer
                        {
                            HorizontalScrollMode = ScrollMode.Disabled,
                            VerticalScrollMode = ScrollMode.Enabled,
                            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                            HorizontalContentAlignment = HorizontalAlignment.Stretch,
                        }
                        .Edit(
                            HomeScrollViewer =>
                            HomeScrollViewer.Content = new Grid
                            {
                                RowDefinitions =
                                {
                                    new RowDefinition {Height = GridLength.Auto },
                                    new RowDefinition {Height = GridLength.Auto }
                                },
                                VerticalAlignment = VerticalAlignment.Top,
                                Children =
                                {
                                    //new TextBlock
                                    //{
                                    //    Style = App.TitleTextBlockStyle,
                                    //    Text = "Home",
                                    //    Margin = new Thickness(0,0,0,10)
                                    //},
                                    new Grid()
                                    .Edit(x => Grid.SetRow(x, 1))
                                    .Edit(x => x.RowDefinitions.AddRange(
                                        Enumerable.Range(0, Features.Features.AllCategories.Length)
                                        .Select(_ => new RowDefinition {Height = GridLength.Auto})
                                    ))
                                    .Edit(x => x.Children.AddRange(
                                        Features.Features.AllCategories.Enumerate().Select(category =>
                                            category.Value.CreateCategoryFeatureSelector(nav, App.SubtitleTextBlockStyle)
                                            .Edit(x => Grid.SetRow(x, category.Index))
                                        )
                                    ))
                                }
                            }
                        )
                    }
                    #endregion
                ))
                .Edit(nav => nav.MenuItems.Add(new NavigationViewItemSeparator()))
                .Edit(nav => nav.MenuItems.AddRange(
                    #region All Categories
                    Features.Features.AllCategories.Select(x =>
                        x.NavigationViewItem
                        .Edit(navi => navi.SelectsOnInvoked = true)
                        .Edit(navi => navi.Tag = new Lazy<(string, UIElement)>(() => (x.Description, x.CreateCategoryFeatureSelector(nav, App.TitleTextBlockStyle).Edit(x => x.Header = null))))
                    )
                    #endregion
                ))
                .Edit(nav => nav.FooterMenuItems.AddRange(
                    new NavigationViewItem[]
                    {
                        #region Inventory
                        InventoryNavigationViewItem = new NavigationViewItem
                        {
                            SelectsOnInvoked = true,
                            Content = "Inventory",
                            Icon = new SymbolIcon(Symbol.Pictures),
                            IsSelected = false,
                            Tag = new Lazy<UIElement>(() => new Grid
                            {
                                Margin = new Thickness(0, 0, 30, 0),
                                RowDefinitions =
                                {
                                    new RowDefinition { Height = GridLength.Auto },
                                    new RowDefinition()
                                },
                                Children =
                                {
                                    //new TextBlock
                                    //{
                                    //    Style = App.TitleTextBlockStyle,
                                    //    Text = "Inventory"
                                    //},
                                    new ScrollViewer
                                    {
                                        HorizontalScrollMode = ScrollMode.Disabled,
                                        VerticalScrollMode = ScrollMode.Enabled,
                                        VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                                        HorizontalContentAlignment = HorizontalAlignment.Stretch,
                                        Content = Inventory.GenerateUI(Inventory.ItemTypes.Images, Inventory.ItemTypes.NumbericalMatrixes)
                                    }.Edit(x => Grid.SetRow(x, 1))
                                }
                            })
                        },
                        #endregion
                        new NavigationViewItem
                        {
                            SelectsOnInvoked = true,
                            Content = "About",
                            Icon = new SymbolIcon((Symbol)0xE946),
                            IsSelected = false,
                            Tag = new Lazy<UIElement>(() => new Grid
                            {
                                RowDefinitions =
                                {
                                    new RowDefinition { Height = GridLength.Auto },
                                    new RowDefinition()
                                },
                                Children =
                                {
                                    //new TextBlock
                                    //{
                                    //    Style = App.TitleTextBlockStyle,
                                    //    Text = "About"
                                    //},
                                    new ScrollViewer
                                    {
                                        HorizontalScrollMode = ScrollMode.Disabled,
                                        VerticalScrollMode = ScrollMode.Enabled,
                                        VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                                        HorizontalContentAlignment = HorizontalAlignment.Stretch,
                                        Content = new AboutPage()
                                    }.Edit(x => Grid.SetRow(x, 1))
                                }
                            })
                        }
                    }
                ))
                .Edit(

            #region Navigation SelectionChanged Event
                    x => x.SelectionChanged += (o, e) =>
                    {
                        if (e.SelectedItem is NavigationViewItem nvi)
                        {
                            if (nvi.Tag is Features.ICategory category)
                            {
                                NavigationHeaderTitle.Text = category.Name;
                                NavigationHeaderSubtitle.Text = category.Description;
                                NavigationHeaderSubtitle.Visibility = Visibility.Visible;
                                MainFrame.Navigate(typeof(PageForFrame), category.CreateCategoryFeatureSelector(nav: x, TitleStyle: App.TitleTextBlockStyle, false), e.RecommendedNavigationTransitionInfo);
                            }
                            else if (nvi.Tag is Features.IFeature feature)
                            {
                                NavigationHeaderTitle.Text = feature.Name;
                                NavigationHeaderSubtitle.Text = feature.Description;
                                NavigationHeaderSubtitle.Visibility = Visibility.Visible;
                                MainFrame.Navigate(typeof(PageForFrame), feature.UIContent, e.RecommendedNavigationTransitionInfo);
                            }
                            else if (nvi.Tag is UIElement Element)
                            {
                                MainFrame.Navigate(typeof(PageForFrame), Element, e.RecommendedNavigationTransitionInfo);
                                NavigationHeaderTitle.Text = nvi.Content.ToString();
                                NavigationHeaderSubtitle.Text = "";
                                NavigationHeaderSubtitle.Visibility = Visibility.Collapsed;
                            }
                            else if (nvi.Tag is Lazy<(string, UIElement)> lazyDescElement)
                            {
                                MainFrame.Navigate(typeof(PageForFrame), lazyDescElement.Value.Item2, e.RecommendedNavigationTransitionInfo);
                                NavigationHeaderTitle.Text = nvi.Content.ToString();
                                NavigationHeaderSubtitle.Text = lazyDescElement.Value.Item1;
                                NavigationHeaderSubtitle.Visibility = Visibility.Visible;
                            }
                            else if (nvi.Tag is Lazy<UIElement> lazyElement)
                            {
                                MainFrame.Navigate(typeof(PageForFrame), lazyElement.Value, e.RecommendedNavigationTransitionInfo);
                                NavigationHeaderTitle.Text = nvi.Content.ToString();
                                NavigationHeaderSubtitle.Text = "";
                                NavigationHeaderSubtitle.Visibility = Visibility.Collapsed;
                            }
                        }
                    }
                    #endregion
                )
                .Edit(
            #region AutoSuggestBox SuggestionChosen Event
                    x => AutoSuggestBox.SuggestionChosen += (o, e) =>
                    {
                        if (e.SelectedItem is Features.FeatureSearchQuery q)
                        {
                            if (q.Feature is Features.IFeature feature)
                                x.SelectedItem = feature.NavigationViewItem;
                            else if (q.SearchQuery == "Inventory")
                                if (InventoryNavigationViewItem != null)
                                    x.SelectedItem = InventoryNavigationViewItem;
                        }
                    }
                    #endregion
                )
            #endregion
        }
        };
        if (Environment.OSVersion.Version.Build < 22000)
            if (Content is Grid g)
                g.Background = new SolidColorBrush { Color = Windows.UI.Color.FromArgb(255, 32, 32, 32) };
        ElementSoundPlayer.State = Settings.IsSoundEnabled ? ElementSoundPlayerState.On : ElementSoundPlayerState.Off;
        var SettingDialog = new ContentDialog
        {
            Content = new SimpleUI.FluentVerticalStack
            {
                Children =
                {
                    new Parameters.CheckboxParameter(Name: "Infinite Mica", Settings.IsMicaInfinite)
                    .Edit(x => x.ParameterValueChanged += delegate
                    {
                        Settings.IsMicaInfinite = x.Result;
                    }).UI,
                    new Parameters.CheckboxParameter(Name: "Sound", Settings.IsMicaInfinite)
                    .Edit(x => x.ParameterValueChanged += delegate
                    {
                        Settings.IsSoundEnabled = x.Result;
                        ElementSoundPlayer.State = Settings.IsSoundEnabled ? ElementSoundPlayerState.On : ElementSoundPlayerState.Off;
                    }).UI
                }
            },
            PrimaryButtonText = "Okay",
        };
        SettingDialog.PrimaryButtonCommand = new Command(() => SettingDialog.Hide());
        var timer = DispatcherQueue.CreateTimer();
        timer.Interval = TimeSpan.FromMilliseconds(100);
        const ushort KEY_PRESSED = 0x8000;
        static bool IsKeyDown(PVirtualKey vk) => Convert.ToBoolean(PInvoke.User32.GetKeyState((int)vk) & KEY_PRESSED);
        bool DialogOpening = false;
        timer.Tick += async delegate
                {
                    try
                    {
                        if (IsKeyDown(PVirtualKey.VK_CONTROL))
                        {
                            if (IsKeyDown(PVirtualKey.VK_SHIFT))
                                if (IsKeyDown(PVirtualKey.VK_S))
                                {
                                    if (DialogOpening) return;
                            //if (SettingDialog.XamlRoot != null) return;
                                    DialogOpening = true;
                                    SettingDialog.XamlRoot = Content.XamlRoot;
                                    await SettingDialog.ShowAsync();
                                    DialogOpening = false;
                                }
                            if (IsKeyDown(PVirtualKey.VK_K))
                                AutoSuggestBox.Focus(FocusState.Programmatic);
                        }
                    }
                    catch
                    {

                    }
                    timer.Start();
                };
        timer.Start();
        Activated += OnWindowCreate;
        #endregion
    }
    void OnWindowCreate(object _, WindowActivatedEventArgs _1)
    {
        var icon = User32.LoadImage(
            hInst: IntPtr.Zero,
            name: $@"{Package.Current.InstalledLocation.Path}\Assets\PhotoToys.ico".ToCharArray(),
            type: User32.ImageType.IMAGE_ICON,
            cx: 0,
            cy: 0,
            fuLoad: User32.LoadImageFlags.LR_LOADFROMFILE | User32.LoadImageFlags.LR_DEFAULTSIZE | User32.LoadImageFlags.LR_SHARED
        );
        var Handle = App.CurrentWindowHandle;
        User32.SendMessage(Handle, User32.WindowMessage.WM_SETICON, (IntPtr)1, icon);
        User32.SendMessage(Handle, User32.WindowMessage.WM_SETICON, (IntPtr)0, icon);
        Activated -= OnWindowCreate;
    }
}
class Command : ICommand
{
    Action Action;
    public Command(Action a)
    {
        Action = a;
        CanExecuteChanged?.Invoke(this, new EventArgs());
    }
    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => true;

    public void Execute(object? parameter) => Action?.Invoke();
}
class CustomPInvoke
{
    [DllImport("user32.dll")]
    public static extern bool TrackPopupMenuEx(IntPtr hmenu, uint fuFlags, int x, int y,
        IntPtr hwnd, IntPtr lptpm);
}