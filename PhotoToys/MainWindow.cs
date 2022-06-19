using System;
using System.Linq;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using System.Threading.Tasks;

namespace PhotoToys;

class MainWindow : MicaWindow
{
    public MainWindow()
    {
        #region UI Initialization
        Title = "PhotoToys";
        ExtendsContentIntoTitleBar = true;
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
                                    .Where(x =>
                                        x.SearchQuery.ToLower().Contains(autoSuggestBox.Text.ToLower())
                                    );
                        }
                    )
                    .Assign(out var autoSuggestBox),
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
                    }
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
                    x => autoSuggestBox.SuggestionChosen += (o, e) =>
                    {
                        if (e.SelectedItem is Features.FeatureSearchQuery q)
                            x.SelectedItem = q.Feature.NavigationViewItem;
                    }
#endregion
                )
#endregion
            }
        };
#endregion
    }
}