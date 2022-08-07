using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;
using Microsoft.UI.Xaml.Controls.Primitives;
using DynamicLanguage;
using static DynamicLanguage.Extension;
namespace PhotoToys.Features;
interface INavigationViewItem
{
    string Name { get; }
    IconElement? Icon { get; }
    string Description { get; }
}
struct FeatureSearchQuery
{
    public string SearchQuery { get; set; }
    public IFeature Feature { get; set; }
    public override string ToString() => SearchQuery;
}
static class Features
{
    public static ICategory[] AllCategories { get; } = new ICategory[]
    {
        new BasicManipulation.BasicManipulation(),
        new ImageGenerator.ImageGenerator(),
        new Filter.Filter(),
        new Analysis.Analysis(),
        new ChannelManipulation.ChannelManipulation(),
        new AdvancedManipulation.AdvancedManipulation(),
    };
    public static IEnumerable<IFeature> AllFeatures => AllCategories.SelectMany(x => x.Features)
        .SelectMany(x => x is ICategory ic ? ic.Features : Array.Empty<IFeature>()).ToArray();
    public static IEnumerable<FeatureSearchQuery> AllSearchQueries
        => AllFeatures.SelectMany(x =>
            x.SearchableQuery.Select(q => new FeatureSearchQuery() { SearchQuery = q, Feature = x })
        );
}
interface ICategory : INavigationViewItem
{
    NavigationViewItem NavigationViewItem { get; }
    IFeature[] Features { get; }
    NavigationView? NavigationView { get; set; }
    public GridView CreateCategoryFeatureSelector(NavigationView nav, Style TitleStyle, bool CreateHeader = true)
    {
        NavigationView = nav;
        return CreateCategoryFeatureSelector(
            Name: Name,
            Description: Description,
            Features: Features,
            nv: nav,
            TitleStyle: TitleStyle,
            CreateHeader: CreateHeader
        );
    }
    public static GridView CreateCategoryFeatureSelector(string Name, string Description, IFeature[] Features, NavigationView nv, Style TitleStyle, bool CreateHeader = true)
    {
        var x = new GridView
        {
            IsTabStop = false,
            IsItemClickEnabled = true,
            ItemContainerStyle = App.GridViewItemContainerStyle,
            Style = App.GridViewWrapItemsPanelTemplateStyle,
            Margin = new Thickness(-5, 0, 0, 0)
        };
        Grid.SetRow(x, 2);
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
                        nv.SelectedItem = x.NavigationViewItem;
                        return;
                    }
                    catch
                    {

                    }
                }
            })
            )
        );
        if (CreateHeader)
        {
            x.Header = new StackPanel
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
            };
        }
        return x;
    }
}
abstract class Category : INavigationViewItem, ICategory
{
    public NavigationView? NavigationView { get; set; }
    public string Name { get; }
    public IconElement? Icon { get; }
    public const string DefaultDescription = Feature.DefaultDescription;
    public string Description { get; }

    public abstract IFeature[] Features { get; }

    public NavigationViewItem NavigationViewItem { get; }
    public Category()
    {
        Type self = GetType();
        Name = self.GetDisplayText<DisplayNameAttribute>() ?? "";
        Description = self.GetDisplayText<DisplayDescriptionAttribute>() ?? DefaultDescription;

        var icon = self.GetCustomAttributes<DisplayIconAttribute>().FirstOrDefault()?.Icon;
        if (!icon.HasValue || icon.Value == default) icon = null;
        Icon = icon.HasValue ? new SymbolIcon(icon.Value) : null;

        NavigationViewItem = new NavigationViewItem
        {
            Content = Name,
            Icon = Icon,
            SelectsOnInvoked = false
        };
        ToolTipService.SetToolTip(NavigationViewItem, Description);
        ToolTipService.SetPlacement(NavigationViewItem, PlacementMode.Mouse);

        NavigationViewItem.MenuItems.AddRange(Features.Select(x => x.NavigationViewItem));

    }
}
interface IFeature  : INavigationViewItem
{
    string DefaultName { get; }
    UIElement UIContent { get; }
    IEnumerable<string> Allias { get; }
    NavigationViewItem NavigationViewItem { get; }
    public IEnumerable<string> SearchableQuery => new string[] { DefaultName }; // .Concat(Allias); Let's temporary disable that
}
abstract class Feature : IFeature
{
    public const string DefaultDescription = "(No Description Provided)";
    public virtual string Name { get; }
    public virtual string Description { get; }
    public virtual string DefaultName => Name;
    public UIElement UIContent => UIContentLazy.Value;
    Lazy<UIElement> UIContentLazy;
    public virtual IEnumerable<string> Allias { get; } = Array.Empty<string>();
    protected abstract UIElement CreateUI();
    public virtual IconElement? Icon { get; } = null;
    

    public NavigationViewItem NavigationViewItem { get; }
    public Feature()
    {
        Type self = GetType();
        Name = self.GetDisplayText<DisplayNameAttribute>() ?? "";
        Description = self.GetDisplayText<DisplayDescriptionAttribute>() ?? DefaultDescription;

        var icon = self.GetCustomAttributes<DisplayIconAttribute>().FirstOrDefault()?.Icon;
        if (!icon.HasValue || icon.Value == default) icon = null;
        Icon = icon.HasValue ? new SymbolIcon(icon.Value) : null;

        UIContentLazy = new Lazy<UIElement>(CreateUI, false);
        NavigationViewItem = new NavigationViewItem
        {
            Content = Name,
            //Icon = Icon,
            Tag = this
        };
        ToolTipService.SetToolTip(NavigationViewItem, Description);
        ToolTipService.SetPlacement(NavigationViewItem, PlacementMode.Mouse);
    }
}
abstract class FeatureCategory : IFeature, ICategory
{
    public NavigationView? NavigationView { get; set; }
    public virtual string Name { get; }
    public virtual string DefaultName => Name;
    public virtual string Description { get; } = Category.DefaultDescription;
    public UIElement UIContent => UIContentLazy.Value;
    Lazy<UIElement> UIContentLazy;
    public virtual IEnumerable<string> Allias { get; } = Array.Empty<string>();
    public NavigationViewItem NavigationViewItem { get; }
    public virtual IconElement? Icon { get; } = null;
    public abstract IFeature[] Features { get; }
    public FeatureCategory()
    {
        Type self = GetType();
        Name = self.GetDisplayText<DisplayNameAttribute>() ?? "";
        Description = self.GetDisplayText<DisplayDescriptionAttribute>() ?? Category.DefaultDescription;

        var icon = self.GetCustomAttributes<DisplayIconAttribute>().FirstOrDefault()?.Icon;
        if (!icon.HasValue || icon.Value == default) icon = null;
        Icon = icon.HasValue ? new SymbolIcon(icon.Value) : null;
        
        UIContentLazy = new Lazy<UIElement>(CreateUI, false);
        NavigationViewItem = new NavigationViewItem
        {
            Content = Name,
            //Icon = Icon,
            Tag = this
        };
        ToolTipService.SetToolTip(NavigationViewItem, Description);
        ToolTipService.SetPlacement(NavigationViewItem, PlacementMode.Mouse);

        NavigationViewItem.MenuItems.AddRange(Features.Select(x => x.NavigationViewItem));
    }
    protected UIElement CreateUI()
        => NavigationView is null ?
            throw new NullReferenceException() :
            (this as ICategory).CreateCategoryFeatureSelector(NavigationView, TitleStyle: App.TitleTextBlockStyle);
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
class DisplayNameAttribute : DisplayTextAttribute
{
    public DisplayNameAttribute(string Default) : base(Default)
    {
    }
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
class DisplayDescriptionAttribute : DisplayTextAttribute
{
    public DisplayDescriptionAttribute(string Default) : base(Default)
    {

    }
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
class DisplayIconAttribute : Attribute
{
    public DisplayIconAttribute() { }
    public DisplayIconAttribute(Symbol Icon) { this.Icon = Icon; }
    public Symbol Icon { get; set; }

}