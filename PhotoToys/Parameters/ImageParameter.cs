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
using Windows.Storage.Streams;
using static PTMS.OpenCvExtension;
using static DynamicLanguage.Extension;
using DynamicLanguage;
namespace PhotoToys.Parameters;

class ImageParameter : ParameterFromUI<Mat>
{
    static Lazy<Inventory.InventoryPicker> InventoryPicker = new(() => new Inventory.InventoryPicker(Inventory.ItemTypes.Images, Inventory.ItemTypes.NumbericalMatrixes));
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
    public bool IsVideoMode => !(VideoCapture is null || ViewAsImageParam.Result);
    public SelectParameter<MatColors> ColorModeParam { get; }
    public CheckboxParameter AlphaRestoreParam { get; }
    public CheckboxParameter ViewAsImageParam { get; }
    public CheckboxParameter OneChannelReplacement { get; }
    SimpleUI.FluentVerticalStack AdditionalOptionLayout;
    bool NumberMatrixMode;
    public ImageParameter(string Name = "Image", bool ColorMode = true, bool ColorChangable = true, bool OneChannelModeEnabled = false, bool AlphaRestore = true, bool AlphaRestoreChangable = true, AlphaModes AlphaMode = AlphaModes.Restore, bool MatrixMode = false)
    {
        this.NumberMatrixMode = MatrixMode;
        this.AlphaMode = AlphaMode;
        _ColorMode = ColorMode;
        this.Name = Name;
        Inventory.ItemTypes[] AllowedItems;
        if (MatrixMode)
            AllowedItems = new Inventory.ItemTypes[]
            {
                Inventory.ItemTypes.Images,
                Inventory.ItemTypes.NumbericalMatrixes
            };
        else
            AllowedItems = new Inventory.ItemTypes[]
            {
                Inventory.ItemTypes.Images
            };
        if (ColorMode) ColorModeParam = new SelectParameter<MatColors>(GetDisplayText(new DisplayTextAttribute("Color Channel") { Sinhala = "වර්ණ නාලිකාව" }), Enum.GetValues<MatColors>());
        else ColorModeParam = new SelectParameter<MatColors>(GetDisplayText(new DisplayTextAttribute("Color Channel") { Sinhala = "වර්ණ නාලිකාව" }),
            Enum.GetValues<MatColors>().Where(x => x != MatColors.Color).ToArray()
        );

        OneChannelReplacement = new CheckboxParameter(GetDisplayText(new DisplayTextAttribute(
            "One Channel Change")
        { 
            Sinhala = "එක් නාලිකාවක් වෙනස් කිරීම" 
        }), OneChannelModeEnabled, false);
        OneChannelReplacement.AddDependency(ColorModeParam, x => x != MatColors.Color && x != MatColors.Grayscale, false);
        AlphaRestoreParam = new CheckboxParameter(AlphaMode == AlphaModes.Include ? GetDisplayText(new DisplayTextAttribute(
            "Include Alpha/Opacity")
        {
            Sinhala = "පාරාන්ධතාවය (Alpha) ඇතුළත් කරන්න"
        }) : GetDisplayText(new DisplayTextAttribute(
            "Restore Alpha/Opacity")
        {
            Sinhala = "පාරාන්ධතාවය (Alpha) ප්‍රතිසාධනය කරන්න"
        }), AlphaRestore);
        AlphaRestoreParam.AddDependency(OneChannelReplacement, x => !x, onNoResult: false);
        this.Name = Name;
        ColorModeParam.ParameterReadyChanged += () => ParameterReadyChanged?.Invoke();
        ColorModeParam.ParameterValueChanged += () => ParameterValueChanged?.Invoke();
        AlphaRestoreParam.ParameterReadyChanged += () => ParameterReadyChanged?.Invoke();
        AlphaRestoreParam.ParameterValueChanged += () => ParameterValueChanged?.Invoke();
        OneChannelReplacement.ParameterReadyChanged += () => ParameterReadyChanged?.Invoke();
        OneChannelReplacement.ParameterValueChanged += () => ParameterValueChanged?.Invoke();

        ViewAsImageParam = new CheckboxParameter("Video As Image", false, true);
        ViewAsImageParam.ParameterReadyChanged += () => ParameterReadyChanged?.Invoke();
        ViewAsImageParam.ParameterValueChanged += () => ParameterValueChanged?.Invoke();
        UI = SimpleUI.GenerateVerticalParameter(Name: Name,
            new Border
            {
                Height = 300,
                AllowDrop = true,
                Padding = new Thickness(16),
                CornerRadius = new CornerRadius(8),
                Style = App.CardBorderStyle,
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
                                    RadiusX = 8,
                                    RadiusY = 8,
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
                                    //VerticalAlignment = VerticalAlignment.Center,
                                    Children =
                                    {
                                        new TextBlock
                                        {
                                            TextAlignment = TextAlignment.Center,
                                            FontSize = 20,
                                            Text = GetDisplayText(new DisplayTextAttribute(
            "Drop File Here!")
        {
            Sinhala = "ගොනුව මෙහි දමන්න!"
        })
                                        },
                                        new TextBlock
                                        {
                                            TextAlignment = TextAlignment.Center,
                                            FontSize = 16,
                                            Text = GetDisplayText(new DisplayTextAttribute("or"){ Sinhala="හෝ" })
                                        },
                                        new StackPanel
                                        {
                                            HorizontalAlignment = HorizontalAlignment.Center,
                                            Children =
                                            {
                                                new Button
                                                {
                                                    HorizontalAlignment = HorizontalAlignment.Stretch,
                                                    Content = GetDisplayText(new DisplayTextAttribute("Browse"){ Sinhala="පිරික්සන්න (Browse)" }),
                                                }.Assign(out var SelectFile),
                                                new Button
                                                {
                                                    HorizontalAlignment = HorizontalAlignment.Stretch,
                                                    Content = GetDisplayText(new DisplayTextAttribute("Paste Image"){ Sinhala="රූපය අලවන්න (Paste)" }),
                                                    Margin = new Thickness(0, 10, 0, 0)
                                                }.Assign(out var FromClipboard),
                                                new Button
                                                {
                                                    HorizontalAlignment = HorizontalAlignment.Stretch,
                                                    Content = GetDisplayText(new DisplayTextAttribute("Select From Inventory"){ Sinhala="පින්තූර එකතුව වෙතින් තෝරන්න" }),
                                                    Margin = new Thickness(0, 10, 0, 0)
                                                }.Assign(out var SelectInventory),
                                                new Button
                                                {
                                                    HorizontalAlignment = HorizontalAlignment.Stretch,
                                                    Content = GetDisplayText(new DisplayTextAttribute("Remove Image"){ Sinhala="රූපය ඉවත් කරන්න" }),
                                                    Margin = new Thickness(0, 10, 0, 0),
                                                    Visibility = Visibility.Collapsed
                                                }
                                                .Assign(out var RemoveImageButton)
                                            }
                                        }
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
                                        new DoubleMatDisplayer
                                        {
                                            UIElement =
                                            {
                                                HorizontalAlignment = HorizontalAlignment.Center
                                            }
                                        }.Assign(out var PreviewImage).UIElement,
                                        new SimpleUI.FluentVerticalStack
                                        {
                                            Children =
                                            {
                                                new NewSlider
                                                {
                                                    Visibility = Visibility.Collapsed,
                                                    Width = 300
                                                }.Assign(out var FrameSlider),
                                                //new Button
                                                //{
                                                //    HorizontalAlignment = HorizontalAlignment.Center,
                                                //    Content = "Remove Image"
                                                //}
                                                //.Assign(out var RemoveImageButton)
                                            }
                                        }
                                        .Edit(x => Grid.SetRow(x, 1))

                                    }
                                }
                                .Edit(x => RemoveImageButton.Click += delegate
                                {
                                    x.Visibility = Visibility.Collapsed;
                                    RemoveImageButton.Visibility = Visibility.Collapsed;
                                    Grid.SetColumnSpan(UIStack, 2);
                                    VideoCapture?.Dispose();
                                    VideoCapture = null;
                                    ImageBeforeProcessed?.Dispose();
                                    ImageBeforeProcessed = null;
                                    ParameterReadyChanged?.Invoke();
                                    ParameterValueChanged?.Invoke();
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
                    }
                    catch (Exception ex)
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
                    if (sf.ContentType.Contains("image"))
                    {
                        // It's an image!
                        var stream = await sf.OpenStreamForReadAsync();
                        var bytes = new byte[stream.Length];
                        await stream.ReadAsync(bytes);
                        VideoCapture?.Dispose();
                        VideoCapture = null;
                        FrameSlider.Visibility = Visibility.Collapsed;
                        ImageBeforeProcessed?.Dispose();
                        ImageBeforeProcessed = Cv2.ImDecode(bytes, ImreadModes.Unchanged);
                        await CompleteDrop(
                            ErrorTitle: "File Error",
                            ErrorContent: $"There is an error reading the file you {action}. Make sure the file is the image file!"
                        );
                    }
                    else if (sf.ContentType.Contains("video"))
                    {
                        // It's a video!
                        VideoCapture = VideoCapture.FromFile(sf.Path);
                        VideoCapture.PosFrames = 0;

                        var framecount = VideoCapture.FrameCount;
                        var fps = VideoCapture.Fps;
                        FrameSlider.Visibility = Visibility.Visible;
                        FrameSlider.Minimum = 0;
                        FrameSlider.Maximum = framecount;
                        FrameSlider.ThumbToolTipValueConverter = new NewSlider.Converter(0, framecount,
                            x => $"{TimeSpan.FromSeconds(x / fps):c} (Frame {x})");
                        ImageBeforeProcessed?.Dispose();
                        ImageBeforeProcessed = new Mat();
                        if (VideoCapture.Read(ImageBeforeProcessed))
                            ImageBeforeProcessed.InplaceToBGRA();
                        else
                            ImageBeforeProcessed = null;
                        await CompleteDrop(
                            ErrorTitle: "File Error",
                            ErrorContent: $"There is an error reading the file you {action}. Make sure the file is the image file!"
                        );
                    }
                    else
                    {
                        ImageBeforeProcessed?.Dispose();
                        ImageBeforeProcessed = null;
                        await CompleteDrop(
                            ErrorTitle: "File Error",
                            ErrorContent: $"There is an error reading the file you {action}. Make sure the file is the image or video file!"
                        );
                    }
                }
                async Task ReadData(DataPackageView DataPackageView, string action)
                {
                    if (DataPackageView.Contains("PhotoToys Image"))
                    {
                        var item = await DataPackageView.GetDataAsync("PhotoToys Image");
                        if (item is IRandomAccessStream stream)
                        {
                            var s = stream.AsStreamForRead();
                            var bytes = new byte[s.Length];
                            s.Read(bytes);
                            ImageBeforeProcessed?.Dispose();
                            ImageBeforeProcessed = Cv2.ImDecode(bytes, ImreadModes.Unchanged);
                        }
                        await CompleteDrop(
                            ErrorTitle: "Image Format Error",
                            ErrorContent: "The image you are trying to paste is not in a correct format"
                        );
                        return;
                    }
                    if (DataPackageView.Contains("PNG"))
                    {
                        var item = await DataPackageView.GetDataAsync("PhotoToysImage");
                        if (item is IRandomAccessStream stream)
                        {
                            var s = stream.AsStreamForRead();
                            var bytes = new byte[s.Length];
                            s.Read(bytes);
                            ImageBeforeProcessed?.Dispose();
                            ImageBeforeProcessed = Cv2.ImDecode(bytes, ImreadModes.Unchanged);
                        }
                        await CompleteDrop(
                            ErrorTitle: "Image Format Error",
                            ErrorContent: "The image you are trying to paste is not in a correct format"
                        );
                        return;
                    }
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
                        VideoCapture?.Dispose();
                        VideoCapture = null;
                        FrameSlider.Visibility = Visibility.Collapsed;
                        ImageBeforeProcessed?.Dispose();
                        ImageBeforeProcessed = Cv2.ImDecode(bytes, ImreadModes.Unchanged);
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
                    if (ImageBeforeProcessed == null)
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
                    var oldResult = ImageBeforeProcessed;
                    if (oldResult.IsCompatableImage())
                    {
                        ImageBeforeProcessed = oldResult.ToBGRA();
                        oldResult.Dispose();
                        if (AdditionalOptionLayout is not null)
                            AdditionalOptionLayout.Visibility = Visibility.Visible;
                    }
                    else if (MatrixMode && oldResult.IsCompatableNumberMatrix())
                    {
                        ImageBeforeProcessed = oldResult;
                        if (AdditionalOptionLayout is not null)
                            AdditionalOptionLayout.Visibility = Visibility.Collapsed;
                    }
                    else throw new ArgumentOutOfRangeException();
                    PreviewImage.Mat = ImageBeforeProcessed;
                    PreviewImageStack.Visibility = Visibility.Visible;
                    RemoveImageButton.Visibility = Visibility.Visible;
                    Grid.SetColumnSpan(UIStack, 1);
                    ParameterReadyChanged?.Invoke();
                    ParameterValueChanged?.Invoke();
                }
                border.Drop += async (o, e) =>
                {
                    try
                    {
                        await ReadData(e.DataView, "dropped");
                    }
                    catch (Exception ex)
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
                    picker.FileTypeFilter.Add(".mp4");
                    picker.FileTypeFilter.Add(".wmv");
                    picker.FileTypeFilter.Add(".mkv");

                    var sf = await picker.PickSingleFileAsync();
                    if (sf is not null)
                        await ReadFile(sf, "selected");
                };
                FromClipboard.Click += async delegate
                {
                    await ReadData(Clipboard.GetContent(), "pasted");
                };
                FrameSlider.ValueChangedSettled += async delegate
                {
                    if (VideoCapture is not null)
                    {
                        VideoCapture.PosFrames = (int)FrameSlider.Value;
                        ImageBeforeProcessed?.Dispose();
                        ImageBeforeProcessed = new Mat();
                        if (!VideoCapture.Read(ImageBeforeProcessed))
                            ImageBeforeProcessed = null;
                        await CompleteDrop(
                                ErrorTitle: "Image Error",
                                ErrorContent: "There is an error reading the Image you selected"
                        );
                    }
                };
                SelectInventory.Click += async delegate
                {
                    var picker = InventoryPicker.Value;
                    picker.XamlRoot = border.XamlRoot;

                    try
                    {
                        var newResult = await picker.PickAsync(SelectInventory, AllowedItems: AllowedItems);
                        if (newResult != null)
                        {
                            VideoCapture?.Dispose();
                            VideoCapture = null;
                            FrameSlider.Visibility = Visibility.Collapsed;
                            ImageBeforeProcessed?.Dispose();
                            ImageBeforeProcessed = newResult;
                            await CompleteDrop(
                                    ErrorTitle: "Image Error",
                                    ErrorContent: "There is an error reading the Image you selected"
                            );
                        }
                    }
                    catch
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
                x.Children.Add(ViewAsImageParam.UI.Edit(x => x.Visibility = Visibility.Collapsed));
                if (ColorChangable)
                    x.Children.Add(ColorModeParam.UI);
                if (AlphaRestoreChangable)
                    x.Children.Add(AlphaRestoreParam.UI);
                if (OneChannelModeEnabled)
                    x.Children.Add(OneChannelReplacement.UI);
            })
            .Assign(out AdditionalOptionLayout)
        );
    }
    public override bool ResultReady => ImageBeforeProcessed != null && ColorModeParam.ResultReady;
    VideoCapture? _VideoCapture;
    public int? PosFrames
    {
        get => VideoCapture?.PosFrames;
        set
        {
            if (VideoCapture != null)
            {
                VideoCapture.PosFrames = value ?? 0;
                ImageBeforeProcessed?.Dispose();
                ImageBeforeProcessed = new Mat();
                if (!VideoCapture.Read(ImageBeforeProcessed))
                    ImageBeforeProcessed = null;
            }
        }
    }
    public VideoCapture? VideoCapture
    {
        get => _VideoCapture;
        private set
        {
            _VideoCapture = value;
            ViewAsImageParam.UI.Visibility = value is null ? Visibility.Collapsed : Visibility.Visible;
            AdditionalOptionLayout.InvalidateArrange();
        }
    }
    Mat? _ImageBeforeProcessed = null;
    Mat? ImageBeforeProcessed
    {
        get => _ImageBeforeProcessed;
        set
        {
            //VideoCapture?.Dispose();
            _ImageBeforeProcessed = value;
        }
    }
    public override Mat Result
    {
        get
        {
            using var tracker = new ResourcesTracker();
            var baseMat = ImageBeforeProcessed ?? throw new InvalidOperationException();
            Mat outputMat;
            if (NumberMatrixMode)
            {
                return baseMat.Clone().InplaceInsertAlpha(AlphaResult);
            }
            else
            {
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
    }
    public Mat? AlphaResult
    {
        get
        {
            if (!ResultReady) throw new InvalidOperationException();
            if (AlphaRestoreParam.Result)
            {
                var baseMat = ImageBeforeProcessed ?? throw new InvalidOperationException();
                if (baseMat.Channels() is 4)
                    return baseMat.ExtractChannel(3);
                else return null;
            }
            else return null;
        }
    }
    public int? Width => _ImageBeforeProcessed?.Width;
    public int? Height => _ImageBeforeProcessed?.Height;
    public Mat PostProcess(Mat m)
    {
        using var tracker = new ResourcesTracker();
        var baseMat = ImageBeforeProcessed ?? throw new InvalidOperationException();
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
            var AlphaResult = this.AlphaResult;
            if (AlphaResult is null) goto End;
            var newmat = m.ToBGR().InplaceInsertAlpha(AlphaResult);
            AlphaResult.Dispose();
            return newmat;
        }
    End:
        return m.Clone();
    }

    public override string Name { get; }

    public override FrameworkElement UI { get; }
}