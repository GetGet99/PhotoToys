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

namespace PhotoToys;

static class SimpleUI
{
    public static async Task ImShow(this OpenCvSharp.Mat M, string Title, XamlRoot XamlRoot)
    {
        await new ContentDialog
        {
            Title = Title,
            Content = new Grid
            {
                RowDefinitions =
                {
                    new RowDefinition(),
                    new RowDefinition { Height = GridLength.Auto }
                },
                Children =
                {
                    new Image
                    {
                        Source = M.ToBitmapImage()
                    },
                    new Button
                    {
                        VerticalAlignment = VerticalAlignment.Center,
                        Content = "Copy"
                    }.Edit(x => x.Click += delegate
                    {
                        var data = new DataPackage();
                        OpenCvSharp.Cv2.ImEncode(".png", M, out var bytes);
                        var ms = new MemoryStream(bytes);
                        var memref = RandomAccessStreamReference.CreateFromStream(ms.AsRandomAccessStream());
                        data.SetData("PNG", ms.AsRandomAccessStream());
                        data.SetBitmap(memref);
                        Clipboard.SetContent(data);
                    })
                    .Edit(x => Grid.SetRow(x, 1))
                }
            },
            PrimaryButtonText = "Okay",
            XamlRoot = XamlRoot
        }.ShowAsync();
    }
    public static UIElement Generate(string PageName, string? PageDescription = null, Action? OnExecute = null, params IParameterFromUI[] Parameters)
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
                confirmbtn.IsEnabled = Parameters.All(x => x.ResultReady);
            };
        }

        verticalstack.Children.Add(confirmbtn);
        confirmbtn.Click += delegate
        {
            if (Parameters.All(x => x.ResultReady))
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
                child.Measure(new Size(availableSize.Width, availableSize.Height - UsedHeight));
                UsedHeight += child.DesiredSize.Height + ElementPadding;
            }
            UsedHeight -= ElementPadding;
            return new Size(availableSize.Width, UsedHeight);
        }
        protected override Size ArrangeOverride(Size finalSize)
        {
            double UsedHeight = 0;
            foreach (var child in Children)
            {
                child.Measure(new Size(finalSize.Width, finalSize.Height - UsedHeight));
                child.Arrange(new Rect(0, UsedHeight, finalSize.Width, child.DesiredSize.Height));
                UsedHeight += child.DesiredSize.Height + ElementPadding;
            }
            UsedHeight -= ElementPadding;
            return new Size(finalSize.Width, UsedHeight);
        }
    }
}