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

namespace PhotoToys.Parameters;

class ImageParameter : IParameterFromUI<Mat>
{
    public event Action? ParameterReadyChanged;
    public ImageParameter(string Name = "Image")
    {
        this.Name = Name;
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
                                        }.Assign(out var FromClipboard)
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
                                        new Image
                                        {
                                            HorizontalAlignment = HorizontalAlignment.Center,
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
                            //e.AcceptedOperation = DataPackageOperation.Copy;
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
                            d.Complete();
                        };
                        async Task ReadFile(StorageFile sf, string action)
                        {
                            var stream = await sf.OpenStreamForReadAsync();
                            var bytes = new byte[stream.Length];
                            await stream.ReadAsync(bytes);
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
                            PreviewImage.Source = _Result.ToBitmapImage();
                            PreviewImageStack.Visibility = Visibility.Visible;
                            Grid.SetColumnSpan(UIStack, 1);
                            ParameterReadyChanged?.Invoke();
                        }
                        border.Drop += async (o, e) => await ReadData(e.DataView, "dropped");
                        SelectFile.Click += async delegate
                        {
                            var picker = new FileOpenPicker
                            {
                                ViewMode = PickerViewMode.Thumbnail,
                                SuggestedStartLocation = PickerLocationId.PicturesLibrary
                            };
                            
                            WinRT.Interop.InitializeWithWindow.Initialize(picker, App.WindowHandle);

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
                    })
                }
            }
        };
    }
    public bool ResultReady => _Result != null;
    Mat? _Result = null;
    public Mat Result => _Result ?? throw new InvalidOperationException();

    public string Name { get; private set; }

    public FrameworkElement UI { get; }
}
