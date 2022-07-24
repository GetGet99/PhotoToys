using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Linq;
using PhotoToys.Parameters;
using Windows.Foundation;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using System.IO;
using Windows.Storage.Streams;
using Windows.Storage.Pickers;
using Windows.Storage;
using OpenCvSharp;
using Size = Windows.Foundation.Size;
using Rect = Windows.Foundation.Rect;

namespace PhotoToys;

static class SimpleUI
{
    public static void ImShow(this Mat M, MatImage MatImage)
    {
        MatImage.Mat = M;
        GC.Collect();
    }
    public static void ImShow(this Mat M, Action<Mat> Action) => Action.Invoke(M);
    public static async Task ImShow(this Mat M, string Title, XamlRoot XamlRoot, bool NewWindow = false)
    {
        MatImage matimg;
        var UI = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition(),
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition { Height = GridLength.Auto }
            },
            Children =
            {
                new ScrollViewer {
                    ZoomMode = ZoomMode.Enabled,
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    MinZoomFactor = 0.1f,
                    MaxZoomFactor = 10f,
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    Content =
                        M.IsCompatableNumberMatrix() ?
                        new DoubleMatDisplayer(DisableView: true)
                        {
                            Mat = M
                        }.MatImage.Assign(out matimg).UIElement :
                        new MatImage(DisableView: true)
                        {
                            Mat = M
                        }.Assign(out matimg).UIElement
                }
                .Assign(out var ScrollViewer)
                .Edit(_ => ScrollViewer.SetBringIntoViewOnFocusChange(matimg.UIElement, false)),
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Children = {
                        new Button
                        {
                            Content = new SymbolIcon(Symbol.ZoomIn),
                            Margin = new Thickness(0, 0, right: 10, 0)
                        }.Edit(x => x.Click += (_, _) => ScrollViewer.ZoomToFactor(
                            MathF.Pow(10, MathF.Log10(ScrollViewer.ZoomFactor) + 0.1f)
                        )),
                        new Button
                        {
                            Content = new SymbolIcon(Symbol.ZoomOut)
                        }.Edit(x => x.Click += (_, _) => ScrollViewer.ZoomToFactor(
                            MathF.Pow(10, MathF.Log10(ScrollViewer.ZoomFactor) - 0.1f)
                        ))
                    }
                }.Edit(x => Grid.SetRow(x, 1)),
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 10, 0, 0),
                    Children =
                    {
                        new Button
                        {
                            VerticalAlignment = VerticalAlignment.Center,
                            Content = IconAndText(Symbol.Copy, "Copy"),
                            Margin = new Thickness(0, 0, 10, 0),
                        }.Edit(x => x.Click += async (_, _) => await matimg.CopyToClipboard()),
                        new Button
                        {
                            VerticalAlignment = VerticalAlignment.Center,
                            Content = IconAndText(Symbol.Save, "Save"),
                            Margin = new Thickness(0, 0, 10, 0),
                        }.Edit(x => x.Click += async (_, _) => await matimg.Save()),
                        new Button
                        {
                            VerticalAlignment = VerticalAlignment.Center,
                            Content = IconAndText(Symbol.Add, "Add To Inventory"),
                        }.Edit(x => x.Click += async (_, _) => await matimg.AddToInventory())
                    }
                }
                .Edit(x => Grid.SetRow(x, 2))
            }
        };
        if (NewWindow)
        {
            new MicaWindowWithTitleBar
            {
                Title = "View",
                Content = UI.Edit(x => x.Margin = new Thickness(16, 0, 16, 16))
            }.Activate();
        } else
            await new ContentDialog
            {
                Title = Title,
                Content = UI,
                PrimaryButtonText = "Okay",
                XamlRoot = XamlRoot
            }.ShowAsync();
    }
    public static StackPanel IconAndText(Symbol Icon, string Text)
        => IconAndText(new SymbolIcon(Icon), Text);
    public static StackPanel IconAndText(IconElement Icon, string Text)
        => new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Children =
            {
                Icon,
                new TextBlock {
                    Margin = new Thickness(10,0,0,0),
                    Text = Text
                }
            }
        };
    public static UIElement Generate(string PageName, string? PageDescription = null, Action? OnExecute = null, params ParameterFromUI[] Parameters)
    {
        var verticalstack = new FluentVerticalStack
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Top,
            Children =
                {
                    new TextBlock
                    {
                        Style = App.TitleTextBlockStyle,
                        Text = PageName
                    }
                }
        };
        if (PageDescription != null)
            verticalstack.Children.Add(new TextBlock
            {
                Text = PageDescription
            });

        var confirmbtn = new Button
        {
            Content = "Confirm",
            IsEnabled = false
        };

        foreach (var p in Parameters)
        {
            verticalstack.Children.Add(p.UI);
            p.ParameterReadyChanged += delegate
            {
                confirmbtn.IsEnabled = Parameters.All(x => x.ResultReady || x.UI.Visibility is Visibility.Collapsed);
            };
        }

        verticalstack.Children.Add(confirmbtn);
        confirmbtn.Click += delegate
        {
            if (Parameters.All(x => x.ResultReady || x.UI.Visibility is Visibility.Collapsed))
                OnExecute?.Invoke();
        };
        return new ScrollViewer
        {
            Content = verticalstack,
            HorizontalScrollMode = ScrollMode.Disabled,
            VerticalScrollMode = ScrollMode.Enabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
    }
    public static UIElement GenerateLIVE(string PageName, string? PageDescription = null, Action<Action<Mat>>? OnExecute = null, IMatDisplayer? MatDisplayer = null, bool AutoRunWhenCreate = false, params ParameterFromUI[] Parameters)
    {
        if (MatDisplayer is null) MatDisplayer = new MatImage();
        var verticalstack = new FluentVerticalStack
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 0, 30, 0),
            Children =
            {
                //new TextBlock
                //{
                //    Style = App.TitleTextBlockStyle,
                //    Text = PageName
                //}
            }
        };
        //if (PageDescription != null)
        //    verticalstack.Children.Add(new TextBlock
        //    {
        //        Text = PageDescription
        //    });
        MatDisplayer.UIElement.MaxHeight = 500;
        var Result = new Border
        {
            CornerRadius = new CornerRadius(16),
            Padding = new Thickness(16),
            Style = App.CardBorderStyle,
            Child = new Grid
            {
                RowDefinitions =
                {
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition { Height = GridLength.Auto },
                },
                Children =
                {
                    new FluentVerticalStack
                    {
                        Children =
                        {
                            new Grid
                            {
                                ColumnDefinitions =
                                {
                                    new ColumnDefinition { Width = GridLength.Auto },
                                    new ColumnDefinition(),
                                    new ColumnDefinition { Width = GridLength.Auto },
                                },
                                Children =
                                {
                                    new TextBlock
                                    {
                                        Text = "Result",
                                        VerticalAlignment = VerticalAlignment.Center,
                                    },
                                    new StackPanel
                                    {
                                        Orientation = Orientation.Horizontal,
                                        Children =
                                        {
                                            new Button
                                            {
                                                Margin = new Thickness(10, 0, 0, 0),
                                                Content = new StackPanel
                                                {
                                                    Orientation = Orientation.Horizontal,
                                                    Children =
                                                    {
                                                        new SymbolIcon(Symbol.View),
                                                        new TextBlock
                                                        {
                                                            Margin = new Thickness(10, 0, 0, 0),
                                                            Text = "View Image"
                                                        }
                                                    }
                                                },
                                                IsEnabled = false
                                            }.Assign(out var viewbtn).Edit(x =>
                                            {
                                                x.Click += async delegate
                                                {
                                                    await MatDisplayer.MatImage.View();
                                                };
                                            }),
                                            new Button
                                            {
                                                Margin = new Thickness(10, 0, 0, 0),
                                                Content = new StackPanel
                                                {
                                                    Orientation = Orientation.Horizontal,
                                                    Children =
                                                    {
                                                        new SymbolIcon((Symbol)0xE8A7), // OpenInNewWindow
                                                        new TextBlock
                                                        {
                                                            Margin = new Thickness(10, 0, 0, 0),
                                                            Text = "View Image In New Window"
                                                        }
                                                    }
                                                },
                                                IsEnabled = false
                                            }.Assign(out var viewbtnnewwin).Edit(x =>
                                            {
                                                x.Click += async delegate
                                                {
                                                    await MatDisplayer.MatImage.View(true);
                                                };
                                            })
                                        }
                                    }.Edit(x => Grid.SetColumn(x, 2))
                                }
                            },
                            new Button
                            {
                                Content = "Export Video",
                                Visibility = Visibility.Collapsed,
                            }.Assign(out var ExportVideoButton)
                        }
                    }.Assign(out var ExportVideoButtonContainer),
                    MatDisplayer.UIElement.Edit(x => Grid.SetRow(x, 1))
                }
            }
        };

        foreach (var p in Parameters)
        {
            verticalstack.Children.Add(p.UI);
            p.ParameterValueChanged += delegate
            {
                if (Parameters.All(x => x.ResultReady || x.UI.Visibility is Visibility.Collapsed))
                {
                    OnExecute?.Invoke(x =>
                    {
                        viewbtn.IsEnabled = true;
                        viewbtnnewwin.IsEnabled = true;
                        MatDisplayer.Mat = x;
                        GC.Collect();
                    });
                }
                verticalstack.InvalidateArrange();
            };
            if (p is ImageParameter imageParameter)
                imageParameter.ParameterValueChanged += delegate
                {
                    ExportVideoButton.Visibility = 
                        (from pa in Parameters
                         where pa is ImageParameter impa && impa.IsVideoMode
                         select true).Count() == 1 ? Visibility.Visible : Visibility.Collapsed;
                    ExportVideoButtonContainer.InvalidateArrange();
                };
        }
        ExportVideoButton.Click += async delegate
        {
            var para = (from pa in Parameters
                         where pa is ImageParameter impa && impa.IsVideoMode
                         select pa).FirstOrDefault(default(ParameterFromUI));
            if (para is ImageParameter video && video.VideoCapture is VideoCapture vidcapture)
            {
                var picker = new FileSavePicker
                {
                    SuggestedStartLocation = PickerLocationId.VideosLibrary
                };

                WinRT.Interop.InitializeWithWindow.Initialize(picker, App.CurrentWindowHandle);

                picker.FileTypeChoices.Add("MP4", new string[] { ".mp4" });
                picker.FileTypeChoices.Add("WMV", new string[] { ".wmv" });
                picker.FileTypeChoices.Add("MKV", new string[] { ".mkv" });

                var sf = await picker.PickSaveFileAsync();
                if (sf != null)
                {
                    var selectedframe = video.PosFrames;
                    var totalFrames = vidcapture.FrameCount;
                    var dialog = new ContentDialog
                    {
                        Content = new FluentVerticalStack
                        {
                            Children =
                            {
                                new ProgressBar
                                {
                                    //Value = 50,
                                }.Assign(out var progressBar),
                                new TextBlock
                                {

                                }.Assign(out var PercentageProgress),
                                new TextBlock
                                {

                                }.Assign(out var AverageRenderSpeed),
                                new TextBlock
                                {

                                }.Assign(out var EstimatedTime)
                            }
                        },
                        XamlRoot = Result.XamlRoot,
                    };
                    DateTime dateTime;
                    async Task RunLoop()
                    {
                        await Task.Run(async delegate
                        {
                            using var writer = new VideoWriter(sf.Path, FourCC.MP4V, vidcapture.Fps,
                               new OpenCvSharp.Size(vidcapture.FrameWidth, vidcapture.FrameHeight)
                            );
                            for (int i = 0; i < totalFrames; i++)
                            {
                                video.PosFrames = i;
                                //if (i % 10 == 0)
                                dialog.DispatcherQueue.TryEnqueue(delegate
                                {
                                    var progress = (double)(i + 1) / totalFrames * 100;
                                    progressBar.Value = progress;
                                    var renderFPS = i / (DateTime.Now - dateTime).TotalSeconds;
                                    var estimatedseconds = (totalFrames - i) / (renderFPS + 1e-3);
                                    const double onedayinseconds = 60 * 60 * 24;
                                    
                                    PercentageProgress.Text = $"{progress:N2}% ({i + 1}/{totalFrames})";
                                    AverageRenderSpeed.Text = $"Average Render Speed: {renderFPS:N2} frames per second";
                                    EstimatedTime.Text = $"Estimated Time left: {(estimatedseconds > onedayinseconds ? "More than a day" : TimeSpan.FromSeconds((totalFrames - i) / (renderFPS + 1e-3))):c}";
                                });
                                if (video.Result is null) break;
                                TaskCompletionSource<Mat> result = new();
                                OnExecute?.Invoke(x =>
                                {
                                    result.SetResult(x);
                                    GC.Collect();
                                });
                                writer.Write(await result.Task);
                            }
                            writer.Release();
                        });
                    };
                    dateTime = DateTime.Now;
                    _ = dialog.ShowAsync();

                    await RunLoop();
                    dialog.Hide();

                    video.PosFrames = selectedframe;

                }
            }
        };

        verticalstack.Children.Add(Result);

        if (AutoRunWhenCreate)
        {
            if (Parameters.All(x => x.ResultReady || x.UI.Visibility is Visibility.Collapsed))
            {
                OnExecute?.Invoke(x =>
                {
                    viewbtn.IsEnabled = true;
                    viewbtnnewwin.IsEnabled = true;
                    MatDisplayer.Mat = x;
                    GC.Collect();
                });
            }
            verticalstack.InvalidateArrange();
        }

        return new ScrollViewer
        {
            Content = verticalstack,
            HorizontalScrollMode = ScrollMode.Disabled,
            VerticalScrollMode = ScrollMode.Enabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
    }
    static Border GenerateInnerParameter(string Name, UIElement Element)
        => new Border
        {
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(16),
            Style = App.CardBorderStyle,
            Child = Element
        };
    public static Border GenerateSimpleParameter(string Name, FrameworkElement Element)
        => GenerateInnerParameter(Name: Name,
            Element: new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition(),
                    new ColumnDefinition { Width = GridLength.Auto },
                },
                Children =
                {
                    new TextBlock
                    {
                        Text = Name,
                        VerticalAlignment = VerticalAlignment.Center,
                    },
                    Element.Edit(x => Grid.SetColumn(x, 2))
                }
            });
    public static Border GenerateVerticalParameter(string Name, params UIElement[] Elements)
        => GenerateInnerParameter(Name: Name,
                Element: new FluentVerticalStack
                {
                    Children =
                    {
                        new TextBlock
                        {
                            Text = Name,
                        }
                    }
                }
                .Edit(x => x.Children.AddRange(Elements))
            );
    public class FluentVerticalStack : Panel
    {
        public FluentVerticalStack(int ElementPadding = 16)
        {
            this.ElementPadding = ElementPadding;
        }
        public int ElementPadding { get; }
        protected override Size MeasureOverride(Size availableSize)
        {
            double UsedHeight = 0;
            foreach (var child in Children)
            {
                if (child.Visibility == Visibility.Collapsed) continue;
                child.Measure(new Size(availableSize.Width, double.PositiveInfinity));
                UsedHeight += child.DesiredSize.Height + ElementPadding;
            }
            UsedHeight -= ElementPadding;
            return new Size(availableSize.Width, Math.Max(UsedHeight, 0));
        }
        protected override Size ArrangeOverride(Size finalSize)
        {
            double UsedHeight = 0;
            foreach (var child in Children)
            {
                if (child.Visibility == Visibility.Collapsed) continue;
                child.Measure(new Size(finalSize.Width, double.PositiveInfinity)); // finalSize.Height - UsedHeight
                child.Arrange(new Rect(0, UsedHeight, finalSize.Width, child.DesiredSize.Height));
                UsedHeight += child.DesiredSize.Height + ElementPadding;
            }
            UsedHeight -= ElementPadding;
            return new Size(finalSize.Width, Math.Max(UsedHeight, 0));
        }
    }
}