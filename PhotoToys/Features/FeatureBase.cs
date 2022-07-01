using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using PhotoToys.Parameters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Text;
using Microsoft.UI.Xaml.Controls.Primitives;
namespace PhotoToys.Features;
interface INavigationViewItem
{
    string Name { get; }
    Microsoft.UI.Xaml.Controls.IconElement? Icon { get; }
    string Description { get; }
}
struct FeatureSearchQuery
{
    public string SearchQuery { get; set; }
    public Feature Feature { get; set; }
    public override string ToString() => SearchQuery;
}
static class Features
{
    public static Category[] AllCategories { get; } = new Category[]
    {
        new BasicManipulation(),
        new Filter(),
        new Analysis(),
        new ChannelManipulation(),
        new AdvancedManipulation()
    };
    public static IEnumerable<Feature> AllFeatures => AllCategories.SelectMany(x => x.Features);
    public static IEnumerable<FeatureSearchQuery> AllSearchQueries
        => AllFeatures.SelectMany(x =>
            x.SearchableQuery.Select(q => new FeatureSearchQuery() { SearchQuery = q, Feature = x })
        );
}

abstract class Category : INavigationViewItem
{
    public abstract string Name { get; }
    public virtual IconElement? Icon { get; } = null;
    public const string DefaultDescription = Feature.DefaultDescription;
    public virtual string Description { get; } = DefaultDescription;

    public abstract Feature[] Features { get; }

    public NavigationViewItem NavigationViewItem { get; }
    public Category()
    {
        NavigationViewItem = new NavigationViewItem
        {
            Content = Name,
            Icon = Icon,
            SelectsOnInvoked = false
        };
        ToolTipService.SetToolTip(NavigationViewItem, Description);
        ToolTipService.SetPlacement(NavigationViewItem, PlacementMode.Mouse);

        NavigationViewItem.MenuItems.AddRange(Features.Select(x =>x.NavigationViewItem));

    }
    public GridView CreateCategoryFeatureSelector(NavigationView nav, Style TitleStyle)
        => new GridView
        {
            IsItemClickEnabled = true,
            Header = new StackPanel
            {
                Margin = new Thickness(5, 0, 0, 0),
                Children =
                {
                    new TextBlock
                    {
                        Style = TitleStyle,
                        Text = Name,
                        Margin = new Thickness(0,0,0,10)
                    },
                    new TextBlock
                    {
                        Text = Description,
                        TextWrapping = TextWrapping.WrapWholeWords,
                        TextTrimming = TextTrimming.WordEllipsis,
                        Margin = new Thickness(0,0,0,10)
                    }
                }
            },
            ItemContainerStyle = App.GridViewItemContainerStyle,
            Style = App.GridViewWrapItemsPanelTemplateStyle,
            Margin = new Thickness(-5, 0, 0, 0)
        }.Edit(x => Grid.SetRow(x, 2))
        .Edit(
            x =>
            x.Items.AddRange(
                Features.Select(x => new Button
                {
                    Width = 350,
                    Height = 100,
                    VerticalContentAlignment = VerticalAlignment.Top,
                    Content = new SimpleUI.FluentVerticalStack(ElementPadding: 4)
                    {
                        Children =
                            {
                                new TextBlock
                                {
                                    Style = App.BodyStrongTextBlockStyle,
                                    Text = x.Name
                                },
                                new TextBlock
                                {
                                    Text = x.Description,
                                    Style = App.CaptionTextBlockStyle,
                                    TextWrapping = TextWrapping.WrapWholeWords,
                                    TextTrimming = TextTrimming.WordEllipsis
                                }
                            }
                    }
                }
                .Edit(btn => btn.Click += async delegate
                {
                    while (true)
                    {
                        await Task.Delay(10);
                        try
                        {
                            nav.SelectedItem = x.NavigationViewItem;
                            return;
                        }
                        catch
                        {

                        }
                    }
                })
                )
            )
        );
}

abstract class Feature : INavigationViewItem
{
    public abstract string Name { get; }
    public UIElement UIContent => UIContentLazy.Value;
    Lazy<UIElement> UIContentLazy;
    public virtual IEnumerable<string> Allias { get; } = Array.Empty<string>();
    protected abstract UIElement CreateUI();
    public virtual IconElement? Icon { get; } = null;
    public const string DefaultDescription = "(No Description Provided)";
    public virtual string Description { get; } = DefaultDescription;

    public IEnumerable<string> SearchableQuery => Name.AsSingleEnumerable(); // .Concat(Allias); Let's temporary disable that

    public NavigationViewItem NavigationViewItem { get; }
    public Feature()
    {
        UIContentLazy = new Lazy<UIElement>(CreateUI, false);
        NavigationViewItem = new NavigationViewItem
        {
            Content = Name,
            Icon = Icon,
            Tag = this
        };
        ToolTipService.SetToolTip(NavigationViewItem, Description);
        ToolTipService.SetPlacement(NavigationViewItem, PlacementMode.Mouse);
    }
}
