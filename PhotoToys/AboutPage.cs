using System;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media;
using System.Windows.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.ApplicationModel;
using System.Runtime.InteropServices;

namespace PhotoToys
{
    class AboutPage : Grid
    {
        public AboutPage()
        {
            var version = Package.Current.Id.Version;
            Children.AddRange(new UIElement[]
            {
                new Grid
                {
                    Margin = new Thickness(0, 30, 0, 0),
                    VerticalAlignment = VerticalAlignment.Top,
                    ColumnDefinitions = {
                        new ColumnDefinition { Width = GridLength.Auto },
                        new ColumnDefinition(),
                    },
                    Children =
                    {
                        new Image
                        {
                            VerticalAlignment = VerticalAlignment.Top,
                            Source = new BitmapImage(new Uri("ms-appx:///Assets/PhotoToys.png")),
                            Width = 100,
                            Height = 100,
                        },
                        new SimpleUI.FluentVerticalStack(4)
                        {
                            Margin = new Thickness(20, 0, 0, 0),
                            VerticalAlignment = VerticalAlignment.Top,
                            Children =
                            {
                                new TextBlock
                                {
                                    Text = "PhotoToys (Beta)"
                                },
                                new TextBlock
                                {
                                    Text = "By Get"
                                },
                                new TextBlock
                                {
                                    Text = $"Version: {version.Major}.{version.Minor}.{version.Build}.{version.Revision}"
                                },
                                new TextBlock
                                {
                                    VerticalAlignment = VerticalAlignment.Stretch,
                                    Text = "PhotoToys is open-sourced on Github"
                                },
                                new StackPanel
                                {
                                    Orientation = Orientation.Horizontal,
                                    Children = {
                                        
                                        new HyperlinkButton
                                        {
                                            Content = "Go to Github Repository",
                                            NavigateUri = new Uri("https://github.com/Get0457/PhotoToys")
                                        },
                                    }
                                }
                            }
                        }.Edit(x => SetColumn(x, 1))
                    }
                }
            });
        }
    }
}
