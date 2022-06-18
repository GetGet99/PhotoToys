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
            if (CacheInventory.ContainsKey(itemTypes))
                collection = CacheInventory[itemTypes];
            else
            {
                collection = new ObservableCollection<(string, Mat)>();
                CacheInventory.Add(itemTypes, collection);
            }
            var name = itemTypes.ToString();
            if (await LocalAppData.TryGetItemAsync(name) is not StorageFolder sf)
                sf = await LocalAppData.CreateFolderAsync(name);
            foreach (var file in await sf.GetFilesAsync())
            {
                using FileStorage CVFileStorage = new(
                    file.Path,
                    FileStorage.Modes.Read
                );
                var node = CVFileStorage["mat"];
                if (node != null)
                    collection.Add((file.DisplayName, node.ReadMat()));
            }
        }));
    }
    static readonly StorageFolder LocalAppData = ApplicationData.Current.LocalFolder;
    static readonly Dictionary<ItemTypes, ObservableCollection<(string, Mat)>> CacheInventory = new();

    public enum ItemTypes
    {
        ColorImageWithTransparency,
        ColorImage,
        GrayscaleImage,
        NumbericalMatrix,
        MiscellaneousMatrix
    }
    public static async Task Add(Mat Image)
    {
        await Add(Image, Image.DetermineType());
    }
    public static ItemTypes DetermineType(this Mat m)
    {
        var type = m.Type();
        if (type.IsInteger)
        {
            if (type.Depth == 8)
            {
                return type.Channels switch
                {
                    4 => ItemTypes.ColorImageWithTransparency,
                    3 => ItemTypes.ColorImage,
                    1 => ItemTypes.GrayscaleImage,
                    _ => ItemTypes.MiscellaneousMatrix,
                };
            }
            else
                return ItemTypes.MiscellaneousMatrix;
        }
        else
        {
            if (type.Depth == 64)
                return ItemTypes.NumbericalMatrix;
            else
                return ItemTypes.MiscellaneousMatrix;
        }
    }
    public static async Task Add(Mat Image, ItemTypes ItemType)
    {
        StorageFolder folder = await LocalAppData.GetFolderAsync(ItemType.ToString());

        var name = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fffffffK");
        var file = await folder.CreateFileAsync($"{name}.mat", CreationCollisionOption.GenerateUniqueName);
        using FileStorage CVFileStorage = new(
            file.Path,
            FileStorage.Modes.Write
        );
        CVFileStorage.Write("mat", Image);
        CacheInventory[ItemType].Add((name, Image));
    }

    public static FrameworkElement GenerateUI()
    {
        return new NavigationView
        {
            PaneDisplayMode = NavigationViewPaneDisplayMode.Top,
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
                    new TextBlock
                    {
                        Style = App.TitleTextBlockStyle,
                        Text = "Inventory"
                    },
                    new StackPanel
                    {
                        VerticalAlignment = VerticalAlignment.Center,
                        Orientation = Orientation.Horizontal,
                        Margin = new Thickness(0,0,-10,0),
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
                            #if DEBUG
                            new Button
                            {
                                Content = new SymbolIcon(Symbol.Folder),
                                Margin = new Thickness(0, 0, 10, 0)
                            }
                            .Edit(x => ToolTipService.SetToolTip(x, "Debug: Browse in File Explorer"))
                            .Edit(x => x.Click += delegate
                            {
                                Process.Start(new ProcessStartInfo
                                {
                                    FileName = LocalAppData.Path,
                                    UseShellExecute = true
                                });
                            })
                            #endif
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
            x.MenuItems.AddRange(CacheInventory.Select(item =>
                new NavigationViewItem
                {
                    Content = item.Key.ToString().ToReadableName(),
                    Tag = item.Value
                }
            ));
            x.SelectedItem = x.MenuItems[0];
        });
    }
}
