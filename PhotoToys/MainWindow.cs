using System;
using System.Linq;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using System.Threading.Tasks;
using Microsoft.UI.Input;
using PVirtualKey = PInvoke.User32.VirtualKey;
using Microsoft.UI.Xaml.Media;
using System.Windows.Input;

namespace PhotoToys;

class MainWindow : MicaWindow
{
    public MainWindow()
    {
        #region UI Initialization
        Title = "PhotoToys";
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
                    Child = new TextBlock
                    {
                        Text = "PhotoToys",
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(10, 0, 0, 0)
                    }
                }.Edit(x => SetTitleBar(x)),
                #endregion
                #region NavigationAndContent
                new NavigationView
                {
                    IsBackEnabled = false,
                    IsBackButtonVisible = NavigationViewBackButtonVisible.Collapsed,
                    IsPaneToggleButtonVisible = false,
                    IsSettingsVisible = false,
                    Content = new Frame().Assign(out var MainFrame),
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
                    }
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
                                    new TextBlock
                                    {
                                        Style = App.TitleTextBlockStyle,
                                        Text = "Home",
                                        Margin = new Thickness(0,0,0,10)
                                    },
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
                        .Edit(navi => navi.Tag = x.CreateCategoryFeatureSelector(nav, App.TitleTextBlockStyle))
                    )
                    #endregion
                ))
                .Edit(nav => nav.FooterMenuItems.Add(
                    #region Inventory

                    new NavigationViewItem
                    {
                        SelectsOnInvoked = true,
                        Content = "Inventory",
                        Icon = new SymbolIcon(Symbol.Pictures),
                        IsSelected = true,
                        Tag = new ScrollViewer
                        {
                            HorizontalScrollMode = ScrollMode.Disabled,
                            VerticalScrollMode = ScrollMode.Enabled,
                            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                            HorizontalContentAlignment = HorizontalAlignment.Stretch,
                            Content = Inventory.GenerateUI(Inventory.ItemTypes.Image)
                        }
                    }.Assign(out InventoryNavigationViewItem)
#endregion
                ))
                .Edit(
#region Navigation SelectionChanged Event
                    x => x.SelectionChanged += (o, e) =>
                    {
                        if (e.SelectedItem is NavigationViewItem nvi)
                        {
                            if (nvi.Tag is Features.Feature feature)
                                MainFrame.Navigate(typeof(PageForFrame), feature.UIContent, e.RecommendedNavigationTransitionInfo);
                            else if (nvi.Tag is UIElement element)
                                MainFrame.Navigate(typeof(PageForFrame), element, e.RecommendedNavigationTransitionInfo);
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
                            if (q.Feature is Features.Feature feature)
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
        var SettingDialog = new ContentDialog
        {
            Content = new Parameters.CheckboxParameter(Name: "Infinite Mica", Settings.IsMicaInfinite)
            .Edit(x => x.ParameterValueChanged += delegate { Settings.IsMicaInfinite = x.Result; })
            .UI,
            PrimaryButtonText = "Okay",
        };
        SettingDialog.PrimaryButtonCommand = new Command(() => SettingDialog.Hide());
        var timer = DispatcherQueue.CreateTimer();
        timer.Interval = TimeSpan.FromMilliseconds(100);
        const ushort KEY_PRESSED = 0x8000;
        static bool IsKeyDown(PVirtualKey vk) => Convert.ToBoolean(PInvoke.User32.GetKeyState((int)vk) & KEY_PRESSED);
        timer.Tick += async delegate
        {
            try
            {
                if (IsKeyDown(PVirtualKey.VK_CONTROL))
                {
                    if (IsKeyDown(PVirtualKey.VK_SHIFT))
                        if (IsKeyDown(PVirtualKey.VK_S))
                        {

                            if (SettingDialog.XamlRoot != null) return;
                            SettingDialog.XamlRoot = Content.XamlRoot;
                            await SettingDialog.ShowAsync();
                            SettingDialog.XamlRoot = null;
                        }
                    if (IsKeyDown(PVirtualKey.VK_K))
                        AutoSuggestBox.Focus(FocusState.Programmatic);
                }
            } catch
            {

            }
            timer.Start();
        };
        timer.Start();
        #endregion
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
}