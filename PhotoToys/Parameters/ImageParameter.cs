using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OpenCvSharp;
using System.IO;
using Windows.ApplicationModel.DataTransfer;
using Microsoft.UI.Xaml.Media;
using Windows.Storage;
using Microsoft.UI;
using Microsoft.UI.Xaml.Shapes;
using Windows.Storage.Pickers;
using System.Diagnostics;

namespace PhotoToys.Parameters;

class ImageParameter : ParameterFromUI<Mat>
{
    static Lazy<Inventory.InventoryPicker> InventoryPicker = new(() => new Inventory.InventoryPicker(Inventory.ItemTypes.Image));
    public override event Action? ParameterReadyChanged;
    public override event Action? ParameterValueChanged;
    public enum AlphaModes
    {
        Restore,
        Include
    }
    public enum MatColors
    {
        Color,
        Grayscale,
        Red,
        Green,
        Blue,
        Alpha
    }
    readonly AlphaModes AlphaMode;
    bool _ColorMode;
    public bool ColorMode
    {
        get => _ColorMode;
        set
        {
            _ColorMode = value;
            if (!_ColorMode &&
                ColorModeParam.Items[0] is ComboBoxItem cbi1 &&
                cbi1.Tag is MatColors c1 &&
                c1 == MatColors.Color)
            {
                if (ColorModeParam.SelectionIndex == 0) ColorModeParam.SelectionIndex++;
                ColorModeParam.Items.RemoveAt(0);
            }
            if (_ColorMode &&
                ColorModeParam.Items[0] is ComboBoxItem cbi2 &&
                cbi2.Tag is MatColors c2 &&
                c2 != MatColors.Color)
                ColorModeParam.Items.Insert(0, ColorModeParam.GenerateItem(MatColors.Color));
        }
    }
    public SelectParameter<MatColors> ColorModeParam { get; }
    public CheckboxParameter AlphaRestoreParam { get; }
    public CheckboxParameter OneChannelReplacement { get; }
    public ImageParameter(string Name = "Image", bool ColorMode = true, bool ColorChangable = true, bool OneChannelModeEnabled = false, bool AlphaRestore = true, bool AlphaRestoreChangable = true, AlphaModes AlphaMode = AlphaModes.Restore)
    {
        this.AlphaMode = AlphaMode;
        this._ColorMode = ColorMode;
        this.Name = Name;
        if (ColorMode) ColorModeParam = new SelectParameter<MatColors>("Color Channel", Enum.GetValues<MatColors>());
        else ColorModeParam = new SelectParameter<MatColors>("Color Channel",
            Enum.GetValues<MatColors>().Where(x => x != MatColors.Color).ToArray()
        );

        OneChannelReplacement = new CheckboxParameter("One Channel Change", OneChannelModeEnabled, false);
        OneChannelReplacement.AddDependency(ColorModeParam, x => x != MatColors.Color && x != MatColors.Grayscale, false);
        AlphaRestoreParam = new CheckboxParameter($"{(AlphaMode == AlphaModes.Include ? "Include" : "Restore")} Alpha/Opacity", AlphaRestore);
        AlphaRestoreParam.AddDependency(OneChannelReplacement, x => !x, onNoResult: false);
        this.Name = Name;
        ColorModeParam.ParameterReadyChanged += () => ParameterReadyChanged?.Invoke();
        ColorModeParam.ParameterValueChanged += () => ParameterValueChanged?.Invoke();
        AlphaRestoreParam.ParameterReadyChanged += () => ParameterReadyChanged?.Invoke();
        AlphaRestoreParam.ParameterValueChanged += () => ParameterValueChanged?.Invoke();
        OneChannelReplacement.ParameterReadyChanged += () => ParameterReadyChanged?.Invoke();
        OneChannelReplacement.ParameterValueChanged += () => ParameterValueChanged?.Invoke();
        UI = new Border
        {
            CornerRadius = new CornerRadius(16),
            Padding = new Thickness(16),
            Style = App.LayeringBackgroundBorderStyle,
            Child = new SimpleUI.FluentVerticalStack
            {
                Children =
                {
                    new TextBlock
                    {
                        Text = Name
                    },
                    new Border
                    {
                        Height = 250,
                        AllowDrop = true,
                        Padding = new Thickness(16),
                        Style = App.LayeringBackgroundBorderStyle,
                        Child = new Grid
                        {
                            ColumnDefinitions =
                            {
                                new ColumnDefinition(),
                                new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star)}
                            },
                            Children =
                            {
                                new Rectangle
                                {
                                    Margin = new Thickness(-16),
                                    RadiusX = 16,
                                    RadiusY = 16,
                                    HorizontalAlignment = HorizontalAlignment.Stretch,
                                    VerticalAlignment = VerticalAlignment.Stretch,
                                    StrokeDashCap = PenLineCap.Flat,
                                    StrokeDashOffset = 1.5,
                                    StrokeDashArray = new DoubleCollection {3},
                                    Stroke = new SolidColorBrush(Colors.Gray),
                                    StrokeThickness = 3,
                                }.Edit(x => Grid.SetColumnSpan(x, 2)),
                                new SimpleUI.FluentVerticalStack
                                {
                                    VerticalAlignment = VerticalAlignment.Center,
                                    Children =
                                    {
                                        new TextBlock
                                        {
                                            TextAlignment = TextAlignment.Center,
                                            FontSize = 20,
                                            Text = "Drop File Here!"
                                        },
                                        new TextBlock
                                        {
                                            TextAlignment = TextAlignment.Center,
                                            FontSize = 16,
                                            Text = "or"
                                        },
                                        new Button
                                        {
                                            HorizontalAlignment = HorizontalAlignment.Center,
                                            Content = "Select file from your computer"
                                        }.Assign(out var SelectFile),
                                        new Button
                                        {
                                            HorizontalAlignment = HorizontalAlignment.Center,
                                            Content = "Paste Image"
                                        }.Assign(out var FromClipboard),
                                        new Button
                                        {
                                            HorizontalAlignment = HorizontalAlignment.Center,
                                            Content = "Select From Inventory"
                                        }.Assign(out var SelectInventory)
                                    }
                                }
                                .Assign(out var UIStack)
                                .Edit(x => Grid.SetColumnSpan(x, 2)),
                                new Grid
                                {
                                    Visibility = Visibility.Collapsed,
                                    RowDefinitions =
                                    {
                                        new RowDefinition(),
                                        new RowDefinition { Height = GridLength.Auto }
                                    },
                                    Children =
                                    {
                                        new MatImage
                                        {
                                            UIElement =
                                            {
                                                HorizontalAlignment = HorizontalAlignment.Center
                                            }
                                        }.Assign(out var PreviewImage),
                                        new Button
                                        {
                                            Margin = new Thickness(0, 10, 0, 0),
                                            HorizontalAlignment = HorizontalAlignment.Center,
                                            Content = "Remove Image"
                                        }
                                        .Edit(x => Grid.SetRow(x, 1))
                                        .Assign(out var RemoveImageButton)
                                        
                                    }
                                }
                                .Edit(x => RemoveImageButton.Click += delegate
                                {
                                    x.Visibility = Visibility.Collapsed;
                                    Grid.SetColumnSpan(UIStack, 2);
                                    _Result?.Dispose();
                                    _Result = null;
                                    ParameterReadyChanged?.Invoke();

                                })
                                .Edit(x => Grid.SetColumn(x, 1))
                                .Assign(out var PreviewImageStack)
                            }
                        }
                    }.Edit(border =>
                    {
                        border.DragOver += async (o, e) =>
                        {
                            var d = e.GetDeferral();
                            try
                            {
                                if (e.DataView.AvailableFormats.Contains(StandardDataFormats.StorageItems))
                                {
                                    var files = (await e.DataView.GetStorageItemsAsync()).ToArray();
                                    if (files.All(f => f is StorageFile sf && sf.ContentType.ToLower().Contains("image")))
                                        e.AcceptedOperation = DataPackageOperation.Copy;
                                }
                                if (e.DataView.AvailableFormats.Contains(StandardDataFormats.Bitmap))
                                {
                                    e.AcceptedOperation = DataPackageOperation.Copy;
                                }
                            } catch (Exception ex)
                            {
                                ContentDialog c = new()
                                {
                                    Title = "Unhandled Error (Drag Item Over)",
                                    Content = ex.Message,
                                    PrimaryButtonText = "Okay",
                                    XamlRoot = border.XamlRoot
                                };
                                _ = c.ShowAsync();
                            }
                            d.Complete();
                        };
                        async Task ReadFile(StorageFile sf, string action)
                        {
                            var stream = await sf.OpenStreamForReadAsync();
                            var bytes = new byte[stream.Length];
                            await stream.ReadAsync(bytes);
                            _Result?.Dispose();
                            _Result = Cv2.ImDecode(bytes, ImreadModes.Unchanged);
                            await CompleteDrop(
                                ErrorTitle: "File Error",
                                ErrorContent: $"There is an error reading the file you {action}. Make sure the file is the image file!"
                            );
                        }
                        async Task ReadData(DataPackageView DataPackageView, string action)
                        {
                            if (DataPackageView.Contains(StandardDataFormats.StorageItems))
                            {
                                var a = await DataPackageView.GetStorageItemsAsync();
                                if (a[^1] is StorageFile sf && sf.ContentType.ToLower().Contains("image"))
                                    await ReadFile(sf, action);
                                return;
                            }
                            if (DataPackageView.Contains(StandardDataFormats.Bitmap))
                            {
                                var a = await DataPackageView.GetBitmapAsync();
                                var b = await a.OpenReadAsync();
                                var stream = b.AsStream();
                                var bytes = new byte[stream.Length];
                                await stream.ReadAsync(bytes);
                                _Result?.Dispose();
                                _Result = Cv2.ImDecode(bytes, ImreadModes.Unchanged);
                                await CompleteDrop(
                                        ErrorTitle: "Image Error",
                                        ErrorContent: "There is an error reading the Image you dropped"
                                );
                                return;
                            }
                            ContentDialog c = new()
                            {
                                Title = "Error",
                                Content = $"The content you {action} is not supported",
                                PrimaryButtonText = "Okay",
                                XamlRoot = border.XamlRoot
                            };
                            await c.ShowAsync();
                            return;
                        }
                        async Task CompleteDrop(string ErrorTitle, string ErrorContent)
                        {
                            if (_Result == null)
                            {
                                ContentDialog c = new()
                                {
                                    Title = ErrorTitle,
                                    Content = ErrorContent,
                                    PrimaryButtonText = "Okay",
                                    XamlRoot = border.XamlRoot
                                };
                                await c.ShowAsync();
                                return;
                            }
                            var oldResult = _Result;
                            _Result = oldResult.ToBGRA();
                            oldResult.Dispose();
                            PreviewImage.Mat = _Result;
                            PreviewImageStack.Visibility = Visibility.Visible;
                            Grid.SetColumnSpan(UIStack, 1);
                            ParameterReadyChanged?.Invoke();
                            ParameterValueChanged?.Invoke();
                        }
                        border.Drop += async (o, e) => {
                            try
                            {
                            await ReadData(e.DataView, "dropped");
                            } catch (Exception ex)
                            {
                                ContentDialog c = new()
                                {
                                    Title = "Unhandled Error (Drop Item)",
                                    Content = ex.Message,
                                    PrimaryButtonText = "Okay",
                                    XamlRoot = border.XamlRoot
                                };
                                _ = c.ShowAsync();
                            }
                        };
                        SelectFile.Click += async delegate
                        {
                            var picker = new FileOpenPicker
                            {
                                ViewMode = PickerViewMode.Thumbnail,
                                SuggestedStartLocation = PickerLocationId.PicturesLibrary
                            };
                            
                            WinRT.Interop.InitializeWithWindow.Initialize(picker, App.CurrentWindowHandle);

                            picker.FileTypeFilter.Add(".jpg");
                            picker.FileTypeFilter.Add(".jpeg");
                            picker.FileTypeFilter.Add(".png");

                            var sf = await picker.PickSingleFileAsync();
                            if (sf != null)
                                await ReadFile(sf, "selected");
                        };
                        FromClipboard.Click += async delegate
                        {
                            await ReadData(Clipboard.GetContent(), "pasted");
                        };
                        
                        SelectInventory.Click += async delegate
                        {
                            var picker = InventoryPicker.Value;
                            picker.XamlRoot = border.XamlRoot;

                            try
                            {
                                var newResult = await picker.PickAsync(SelectInventory);
                                if (newResult != null)
                                {
                                    _Result?.Dispose();
                                    _Result = newResult;
                                    await CompleteDrop(
                                            ErrorTitle: "Image Error",
                                            ErrorContent: "There is an error reading the Image you selected"
                                    );
                                }
                            } catch
                            {

                            }
                        };
                    }),
                    new SimpleUI.FluentVerticalStack
                    {
                        //RowDefinitions = {
                        //    new RowDefinition { Height = GridLength.Auto },
                        //    new RowDefinition { Height = GridLength.Auto },
                        //    new RowDefinition { Height = GridLength.Auto }
                        //},
                        //Margin = new Thickness { Bottom = -10 }
                    }
                    .Edit(x =>
                    {
                        if (ColorChangable)
                            x.Children.Add(ColorModeParam.UI.Edit(x => x.Margin = new Thickness { Bottom = 10 }).Edit(x => Grid.SetRow(x, 0)));
                        if (AlphaRestoreChangable)
                            x.Children.Add(AlphaRestoreParam.UI.Edit(x => x.Margin = new Thickness { Bottom = 10 }).Edit(x => Grid.SetRow(x, 1)));
                        if (OneChannelModeEnabled)
                        {
                            x.Children.Add(OneChannelReplacement.UI.Edit(x => x.Margin = new Thickness { Bottom = 10 }).Edit(x => Grid.SetRow(x, 2)));
                            //void UpdateOneChannelReplacement()
                            //{
                            //    bool e = ColorModeParam.ResultReady && ColorModeParam.Result != MatColors.Color;
                            //    OneChannelReplacement.UI.Visibility = e ? Visibility.Visible : Visibility.Collapsed;
                            //}
                            //ColorModeParam.ParameterValueChanged += UpdateOneChannelReplacement;
                            //UpdateOneChannelReplacement();
                            //if (AlphaRestoreChangable)
                            //{
                            //    void UpdateAlphaRestore()
                            //    {
                            //        bool e = OneChannelReplacement.UI.Visibility == Visibility.Visible && OneChannelReplacement.ResultReady && OneChannelReplacement.Result;
                            //        AlphaRestoreParam.UI.Visibility = e ? Visibility.Collapsed : Visibility.Visible;
                            //    }
                            //    AlphaRestoreParam.ParameterValueChanged += UpdateOneChannelReplacement;
                            //    UpdateAlphaRestore();
                            //}
                        }
                    })
                }
            }
        };
    }
    public override bool ResultReady => _Result != null && ColorModeParam.ResultReady;
    Mat? _Result = null;
    public override Mat Result
    {
        get
        {
            using var tracker = new ResourcesTracker();
            var baseMat = _Result ?? throw new InvalidOperationException();
            Mat outputMat;
            switch (ColorModeParam.Result)
            {
                case MatColors.Color:
                    outputMat = baseMat.ToBGR().Track(tracker);
                    break;
                case MatColors.Grayscale:
                    outputMat = baseMat.ToGray().Track(tracker);
                    break;
                case MatColors.Blue:
                    outputMat = baseMat.ExtractChannel(0).Track(tracker);
                    break;
                case MatColors.Green:
                    outputMat = baseMat.ExtractChannel(1).Track(tracker);
                    break;
                case MatColors.Red:
                    outputMat = baseMat.ExtractChannel(2).Track(tracker);
                    break;
                case MatColors.Alpha:
                    outputMat = baseMat.ExtractChannel(3).Track(tracker);
                    break;
                default:
                    Debugger.Break();
                    throw new Exception();
            }
            return ColorMode ? outputMat.ToBGR() : outputMat.ToGray();
        }
    }
    public Mat? AlphaResult
    {
        get
        {
            if (!ResultReady) throw new InvalidOperationException();
            if (AlphaRestoreParam.Result)
            {
                var baseMat = _Result ?? throw new InvalidOperationException();
                return baseMat.ExtractChannel(3);
            }
            else return null;
        }
    }
    public Mat PostProcess(Mat m)
    {
        using var tracker = new ResourcesTracker();
        var baseMat = _Result ?? throw new InvalidOperationException();
        if (ColorModeParam.Result != MatColors.Color && OneChannelReplacement.Result)
        {
            var newmat = new Mat();
            Cv2.Split(baseMat.ToBGRA().Track(tracker), out var mats);
            mats.Track(tracker);
            mats[ColorModeParam.Result switch
            {
                MatColors.Blue => 0,
                MatColors.Green => 1,
                MatColors.Red => 2,
                MatColors.Alpha => 3,
                _ => throw new InvalidOperationException()
            }] = m.ToGray().Track(tracker);
            Cv2.Merge(mats, newmat);
            return newmat;
        }
        if (AlphaMode == AlphaModes.Restore && AlphaRestoreParam.Result)
        {
            var AlphaResult = this.AlphaResult ?? throw new InvalidOperationException();
            var newmat = m.ToBGR().InplaceInsertAlpha(AlphaResult);
            AlphaResult.Dispose();
            return newmat;
        }
        return m.Clone();
    }

    public override string Name { get; }

    public override FrameworkElement UI { get; }
}
