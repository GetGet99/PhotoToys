﻿using Microsoft.UI.Xaml;
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
    public static void ImShow(this OpenCvSharp.Mat M, MatImage MatImage) => MatImage.Mat = M;
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
                    new MatImage(DisableView: true)
                    {
                        Mat = M
                    }.Assign(out var matimg),
                    new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
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
                    .Edit(x => Grid.SetRow(x, 1))
                }
            },
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
    public static UIElement GenerateLIVE(string PageName, string? PageDescription = null, Action<MatImage>? OnExecute = null, params IParameterFromUI[] Parameters)
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

        var Result = new Border
        {
            CornerRadius = new CornerRadius(16),
            Padding = new Thickness(16),
            Style = App.LayeringBackgroundBorderStyle,
            Child = new Grid
            {
                RowDefinitions =
                {
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition { Height = GridLength.Auto },
                },
                Children =
                {
                    new TextBlock
                    {
                        Text = "Result",
                        VerticalAlignment = VerticalAlignment.Center,
                    },
                    new MatImage
                    {
                        UIElement =
                        {
                            Height = 300
                        }
                    }.Assign(out var MatImage).Edit(x => Grid.SetRow(x, 1))
                }
            }
        };

        foreach (var p in Parameters)
        {
            verticalstack.Children.Add(p.UI);
            p.ParameterValueChanged += delegate
            {
                if (Parameters.All(x => x.ResultReady))
                    OnExecute?.Invoke(MatImage);
            };
        }

        verticalstack.Children.Add(Result);
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