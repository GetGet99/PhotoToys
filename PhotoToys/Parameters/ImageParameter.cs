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
class ImageParameterDefinition : ParameterDefinition<Mat>
{
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
    string Name { get; }
    Parameter CreateParameter(Mat value)
        => new Parameter(Name, value);
    Parameter<Mat> ParameterDefinition<Mat>.CreateParameter(Mat value)
        => CreateParameter(value);
    UserInterface CreateUserInterface()
        => new UserInterface(Name: Name, ColorMode: ColorMode, ColorChangable: ColorChangable, OneChannelModeEnabled: OneChannelModeEnabled,
            AlphaRestore: AlphaRestore, AlphaRestoreChangable: AlphaRestoreChangable, AlphaMode: AlphaMode);
    ParameterFromUI<Mat> ParameterDefinition<Mat>.CreateUserInterface()
        => CreateUserInterface();
    bool ColorMode, ColorChangable, OneChannelModeEnabled, AlphaRestore, AlphaRestoreChangable;
    AlphaModes AlphaMode = AlphaModes.Restore;
    public ImageParameterDefinition(string Name = "Image", bool ColorMode = true, bool ColorChangable = true, bool OneChannelModeEnabled = false, bool AlphaRestore = true, bool AlphaRestoreChangable = true, AlphaModes AlphaMode = AlphaModes.Restore)
    {
        this.Name = Name;
        this.ColorMode = ColorMode;
        this.ColorChangable = ColorChangable;
        this.OneChannelModeEnabled = OneChannelModeEnabled;
        this.AlphaRestore = AlphaRestore;
        this.AlphaRestoreChangable = AlphaRestoreChangable;
        this.AlphaMode = AlphaMode;
    }
    struct Parameter : Parameter<Mat>
    {
        public Parameter(string name, Mat value)
        {
            Value = value;
            Name = name;
        }

        public Mat Value { get; }

        public string Name { get; }
    }
    class UserInterface : ParameterFromUI<Mat>
    {
        static Lazy<Inventory.InventoryPicker> InventoryPicker = new(() => new Inventory.InventoryPicker(Inventory.ItemTypes.Image));
        public override event Action? ParameterReadyChanged;
        public override event Action? ParameterValueChanged;
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
        public UserInterface(string Name, bool ColorMode, bool ColorChangable, bool OneChannelModeEnabled, bool AlphaRestore, bool AlphaRestoreChangable, AlphaModes AlphaMode)
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

            ViewAsImageParam = new CheckboxParameter("Video As Image", false, true);
            ViewAsImageParam.ParameterReadyChanged += () => ParameterReadyChanged?.Invoke();
            ViewAsImageParam.ParameterValueChanged += () => ParameterValueChanged?.Invoke();
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
                        Height = 300,
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
                                    //VerticalAlignment = VerticalAlignment.Center,
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
                                        new StackPanel
                                        {
                                            HorizontalAlignment = HorizontalAlignment.Center,
                                            Children =
                                            {
                                        new Button
                                        {
                                                    HorizontalAlignment = HorizontalAlignment.Stretch,
                                                    Content = "Browse",
                                        }.Assign(out var SelectFile),
                                        new Button
                                        {
                                                    HorizontalAlignment = HorizontalAlignment.Stretch,
                                                    Content = "Paste Image",
                                                    Margin = new Thickness(0, 10, 0, 0)
                                        }.Assign(out var FromClipboard),
                                        new Button
                                        {
                                                    HorizontalAlignment = HorizontalAlignment.Stretch,
                                                    Content = "Select From Inventory",
                                                    Margin = new Thickness(0, 10, 0, 0)
                                                }.Assign(out var SelectInventory),
                                                new Button
                                                {
                                                    HorizontalAlignment = HorizontalAlignment.Stretch,
                                                    Content = "Remove Image",
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
                                        new MatImage
                                        {
                                            UIElement =
                                            {
                                                HorizontalAlignment = HorizontalAlignment.Center
                                            }
                                        }.Assign(out var PreviewImage),
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
                                if (!VideoCapture.Read(ImageBeforeProcessed))
                                    ImageBeforeProcessed = null;
                                await CompleteDrop(
                                    ErrorTitle: "File Error",
                                    ErrorContent: $"There is an error reading the file you {action}. Make sure the file is the image file!"
                                );
                            } else
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
                            ImageBeforeProcessed = oldResult.ToBGRA();
                            oldResult.Dispose();
                            PreviewImage.Mat = ImageBeforeProcessed;
                            PreviewImageStack.Visibility = Visibility.Visible;
                            RemoveImageButton.Visibility = Visibility.Visible;
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
                                var newResult = await picker.PickAsync(SelectInventory);
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
                        x.Children.Add(ViewAsImageParam.UI.Edit(x => x.Visibility = Visibility.Collapsed));
                        if (ColorChangable)
                            x.Children.Add(ColorModeParam.UI);
                        if (AlphaRestoreChangable)
                            x.Children.Add(AlphaRestoreParam.UI);
                        if (OneChannelModeEnabled)
                            x.Children.Add(OneChannelReplacement.UI);
                    })
                    .Assign(out AdditionalOptionLayout)
                }
                }
            };
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
                    var baseMat = ImageBeforeProcessed ?? throw new InvalidOperationException();
                    return baseMat.ExtractChannel(3);
                }
                else return null;
            }
        }
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

}