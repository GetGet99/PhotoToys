using OpenCvSharp;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage;
using System.Collections.ObjectModel;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using System.Diagnostics;
using System.Collections.Specialized;
using Windows.System;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace PhotoToys;

public static class Inventory
{
    public static async Task InitializeAsync()
    {
        await RefreshCacheInventoryAsync();
    }
    public static async Task RefreshCacheInventoryAsync()
    {
        var allItemTypes = Enum.GetValues<ItemTypes>();
        await Task.WhenAll(allItemTypes.Select(async itemTypes =>
        {
            ObservableCollection<(string, Mat)> collection;
            List<(DateTimeOffset, string, Mat)> tmp = new();
            if (CacheInventory.ContainsKey(itemTypes))
            {
                collection = CacheInventory[itemTypes];
                collection.Clear();
            }
            else
            {
                collection = new ObservableCollection<(string, Mat)>();
                CacheInventory.Add(itemTypes, collection);
            }
            var name = itemTypes.ToString();
            if (await LocalAppData.TryGetItemAsync(name) is not StorageFolder sf)
                sf = await LocalAppData.CreateFolderAsync(name);
            if (itemTypes == ItemTypes.Image)
                foreach (var file in await sf.GetFilesAsync())
                {
                    if (file.FileType != ".png") continue;
                    var date = (await file.GetBasicPropertiesAsync()).ItemDate;
                    tmp.Add((date, file.DisplayName, Cv2.ImRead(file.Path, ImreadModes.Unchanged).ToBGRA()));
                }
            else
                foreach (var file in await sf.GetFilesAsync())
                {
                    if (file.FileType != ".mat") continue;
                    var date = (await file.GetBasicPropertiesAsync()).ItemDate;
                    using FileStorage CVFileStorage = new(
                        file.Path,
                        FileStorage.Modes.Read
                    );
                    var node = CVFileStorage["mat"];
                    if (node != null)
                        tmp.Add((date, file.DisplayName, node.ReadMat()));
                }
            collection.AddRange(tmp.OrderByDescending(x => x.Item1).Select(x => (x.Item2, x.Item3)));
        }));
    }
    static readonly StorageFolder LocalAppData = ApplicationData.Current.LocalFolder;
    static readonly Dictionary<ItemTypes, ObservableCollection<(string Name, Mat Matrix)>> CacheInventory = new();

    public enum ItemTypes
    {
        Image,
        NumbericalMatrix,
        MiscellaneousMatrix
    }
    public static async Task Add(Mat Image)
    {
        await Add(Image, Image.DetermineType());
    }
    public static ItemTypes DetermineType(this Mat m)
    {
        if (m.IsCompatableImage()) return ItemTypes.Image;
        if (m.IsCompatableNumberMatrix()) return ItemTypes.NumbericalMatrix;
        return ItemTypes.MiscellaneousMatrix;
    }
    public static async Task Add(Mat Image, ItemTypes ItemType)
    {
        StorageFolder folder = await LocalAppData.GetFolderAsync(ItemType.ToString());

        var name = DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss.fffffff");
        StorageFile file;
        switch (ItemType)
        {
            case ItemTypes.Image:
                file = await folder.CreateFileAsync($"{name}.png", CreationCollisionOption.GenerateUniqueName);
                Cv2.ImWrite(file.Path, Image);
                break;
            case ItemTypes.NumbericalMatrix:
            case ItemTypes.MiscellaneousMatrix:
            default:
                file = await folder.CreateFileAsync($"{name}.mat", CreationCollisionOption.GenerateUniqueName);
                using (FileStorage CVFileStorage = new(file.Path, FileStorage.Modes.Write))
                {
                    CVFileStorage.Write("mat", Image);
                }
                break;
        }

        CacheInventory[ItemType].Insert(0, (name, Image));
    }
    public static async Task Remove((string Name, Mat Imge) Tuple, ItemTypes ItemType)
    {
        StorageFolder folder = await LocalAppData.GetFolderAsync(ItemType.ToString());
        if (await folder.TryGetItemAsync($"{Tuple.Name}.{(ItemType == ItemTypes.Image ? "png" : "mat")}") is StorageFile file)
            await file.DeleteAsync();

        CacheInventory[ItemType].Remove(Tuple);
    }
    public static NavigationView GenerateUI(params ItemTypes[] AllowedItemTypes)
        => GenerateUI(out var _, AllowedItemTypes);


    public static NavigationView GenerateUI(out Dictionary<ItemTypes, GridView> GridView, params ItemTypes[] AllowedItemTypes)
    {
        var _GridView = new Dictionary<ItemTypes, GridView>();
        GridView = _GridView;
        if (AllowedItemTypes.Length == 0) AllowedItemTypes = Enum.GetValues<ItemTypes>();
        return new NavigationView
        {
            PaneDisplayMode = NavigationViewPaneDisplayMode.Top,
            IsSettingsVisible = false,
            IsBackButtonVisible = NavigationViewBackButtonVisible.Collapsed,
            IsTitleBarAutoPaddingEnabled = false,
            #region NavigationView Toolbar
            Header = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition(),
                    new ColumnDefinition { Width = GridLength.Auto }
                },
                Children =
                {
                    //new TextBlock
                    //{
                    //    Style = App.SubtitleTextBlockStyle,
                    //    Text = "Inventory"
                    //},
                    new StackPanel
                    {
                        VerticalAlignment = VerticalAlignment.Center,
                        Orientation = Orientation.Horizontal,
                        Margin = new Thickness(0,0,0,0),
                        Children =
                        {
                            new Button
                            {
                                Content = new SymbolIcon(Symbol.Refresh),
                                //Width = 30,
                                //Height = 30,
                                Margin = new Thickness(0, 0, 10, 0)
                            }
                            .Edit(x => ToolTipService.SetToolTip(x, "Refresh"))
                            .Edit(x => x.Click += async delegate
                            {
                                await RefreshCacheInventoryAsync();
                            }),
                            new Button
                            {
                                Content = new SymbolIcon(Symbol.Folder),
                                Margin = new Thickness(0, 0, 10, 0)
                            }
                            .Edit(x => ToolTipService.SetToolTip(x, "Browse in File Explorer"))
                            .Edit(x => x.Click += delegate
                            {
                                Process.Start(new ProcessStartInfo
                                {
                                    FileName = LocalAppData.Path,
                                    UseShellExecute = true
                                });
                            })
                        }
                    }
                    .Edit(x => Grid.SetColumn(x, 2))
                }
            },
            Content = new Frame().Assign(out var Frame)
            #endregion
        }
        .Edit(x =>
        {
            x.SelectionChanged += (o, e) =>
            {
                if (e.SelectedItem is NavigationViewItem nvi && nvi.Tag is UIElement Element)
                    Frame.Navigate(typeof(PageForFrame), Element, e.RecommendedNavigationTransitionInfo);
            };
            x.MenuItems.AddRange(CacheInventory.Where(x => AllowedItemTypes.Contains(x.Key)).Select(item =>
                new NavigationViewItem
                {
                    Content = item.Key.ToString().ToReadableName(),
                    Tag = new GridView
                    {
                        HorizontalAlignment = HorizontalAlignment.Left
                    }.Edit(gridview =>
                    {
                        _GridView.Add(item.Key, gridview);
                        UIElement Element((string Name, Mat Matrix) tuple)
                        {
                            return (
                                    item.Key == ItemTypes.Image ?
                                    new MatImage(AddToInventoryLabel: "Duplicate", AddToInventorySymbol: Symbol.Copy)
                                    {
                                        Mat = tuple.Matrix,
                                        UIElement =
                                        {
                                            Margin = new Thickness(10),
                                            MaxWidth = 130
                                        },
                                        MenuFlyout =
                                        {
                                            Items =
                                            {
                                                new MenuFlyoutItem
                                                {
                                                    Icon = new SymbolIcon(Symbol.Delete),
                                                    Text = "Delete Permanently"
                                                }.Edit(x => x.Click += async delegate
                                                {
                                                    await Remove(tuple, item.Key);
                                                })
                                            }
                                        }
                                    }
                                    .UIElement : new TextBlock
                                    {
                                        Text = "A Matrix" // Placehohlder
                                    }
                            );
                        }
                        gridview.Items.AddRange(item.Value.Select(
                            x => Element(x)
                        ));
                        gridview.KeyDown += async (o, e) =>
                        {
                            var idx = gridview.SelectedIndex;
                            if (idx < 0) return;
                            switch (e.Key)
                            {
                                case VirtualKey.Delete:
                                    gridview.SelectedIndex--;
                                    await Remove(item.Value[idx], item.Key);
                                    break;
                                case VirtualKey.D:

                                    gridview.SelectedIndex = 0;
                                    await Add(item.Value[idx].Matrix, item.Key);
                                    break;
                            }
                        };
                        item.Value.CollectionChanged += (o, e) =>
                        {
                            var newIndex = e.NewStartingIndex;
                            var oldIndex = e.OldStartingIndex;
                            switch (e.Action)
                            {
                                case NotifyCollectionChangedAction.Add:
                                    var (Name, Matrix) = item.Value[newIndex];
                                    gridview.Items.Insert(newIndex, Element((Name, Matrix)));
                                    break;
                                case NotifyCollectionChangedAction.Remove:
                                    gridview.Items.RemoveAt(oldIndex);
                                    break;
                                case NotifyCollectionChangedAction.Move:
                                    var a = gridview.Items[oldIndex];
                                    var b = gridview.Items[newIndex];
                                    gridview.Items[oldIndex] = b;
                                    gridview.Items[newIndex] = a;
                                    break;
                                case NotifyCollectionChangedAction.Replace:
                                    gridview.Items[oldIndex] = item.Value[oldIndex];
                                    break;
                                case NotifyCollectionChangedAction.Reset:
                                    gridview.Items.Clear();
                                    break;
                            }
                        };
                    })
                }
            ));
            x.SelectedItem = x.MenuItems[0];
        });
    }
    public class InventoryPicker : Flyout
    {
        public Mat? PickedItem { get; private set; } = null;
        public InventoryPicker(params ItemTypes[] AllowedItemTypes)
        {
            //Resources["ContentDialogMinWidth"] = 800;
            //Resources["ContentDialogMinHeight"] = 600;
            //Background = new AcrylicBrush
            //{
            //    TintColor = App.SolidBackgroundFillColorBase,
            //    TintOpacity = 0.5
            //};
            //FullSizeDesired = true;
            //MinWidth = MaxWidth = Width = 800;
            Content = new Grid
            {
                Width = 375,
                Height = 306,
                //Width = 800,
                //Height = 600,
                RowDefinitions =
                {
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition(),
                    new RowDefinition { Height = GridLength.Auto }
                },
                Children =
                {
                    new Grid
                    {
                        ColumnDefinitions =
                        {
                            new ColumnDefinition { Width = GridLength.Auto },
                            new ColumnDefinition(),
                            new ColumnDefinition { Width = GridLength.Auto }
                        },
                        Children =
                        {
                            new TextBlock
                            {
                                Text = "Inventory",
                                Style = App.TitleTextBlockStyle
                            },
                            new Button
                            {
                                Content = new SymbolIcon(Symbol.Cancel)
                            }
                            .Edit(x => Grid.SetColumn(x, 2))
                            .Edit(x => x.Click += (_, _) => Hide())
                        }
                    },
                    GenerateUI(out var gridviews, AllowedItemTypes).Assign(out var navigation).Edit(x => Grid.SetRow(x, 1)),
                    new Button
                    {
                        Margin = new Thickness(16),
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Content = "Select"
                    }
                    .Edit(x =>
                    {
                        Grid.SetRow(x, 2);
                        void UpdateButton()
                        {
                            var idx = navigation.MenuItems.IndexOf(navigation.SelectedItem);
                            x.IsEnabled = gridviews.Values.ElementAt(idx).SelectedIndex != -1;
                        }
                        navigation.Header = null;
                        navigation.SelectionChanged += (_, _) => UpdateButton();
                        foreach (var gridview in gridviews.Values)
                            gridview.SelectionChanged += (_, _) => UpdateButton();
                        UpdateButton();
                        x.Click += delegate
                        {
                            UpdateButton();
                            if (x.IsEnabled)
                            {
                                var idx = navigation.MenuItems.IndexOf(navigation.SelectedItem);
                                var values = gridviews.ElementAt(idx);
                                PickedItem = CacheInventory[values.Key][values.Value.SelectedIndex].Matrix.Clone();
                                Hide();
                            }
                        };
                    })
                }
            };
        }
        public async Task<Mat?> PickAsync(FrameworkElement PlacementTarget)
        {
            PickedItem = null;
            Placement = FlyoutPlacementMode.Bottom;
            ShowAt(PlacementTarget);
            while (IsOpen) await Task.Delay(1000);
            //await ShowAsync();
            return PickedItem;
        }
    }

}
public static class Settings
{
    static ApplicationDataContainer ApplicationSetting = ApplicationData.Current.LocalSettings;
    public static bool IsMicaInfinite
    {
        get => (bool)(ApplicationSetting.Values[nameof(IsMicaInfinite)] ?? false);
        set => ApplicationSetting.Values[nameof(IsMicaInfinite)] = value;
    }

}