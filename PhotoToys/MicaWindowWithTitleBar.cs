using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using PInvoke;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;

namespace PhotoToys;

class MicaWindowWithTitleBar : MicaWindow
{
    public new string Title
    {
        get => TitleTextBlock.Text;
        set
        {
            base.Title = value;
            TitleTextBlock.Text = value;
        }
    }
    public new UIElement? Content
    {
        get => ContentContainerElement.Child;
        set => ContentContainerElement.Child = value;
    }
    TextBlock TitleTextBlock;
    Border ContentContainerElement;
    public MicaWindowWithTitleBar()
    {
        ExtendsContentIntoTitleBar = true;
        base.Content = new Grid
        {
            RowDefinitions = {
                new RowDefinition { Height = GridLength.Auto },
                new RowDefinition()
            },
            Children =
            {
                #region TitleBar
                new Border
                {
                    Height = 30,
                    Child = new Grid
                    {
                        ColumnDefinitions = {
                            new ColumnDefinition { Width = GridLength.Auto },
                            new ColumnDefinition()
                        },
                        Children =
                        {
                            new Border
                            {
                                Child = new Image
                                {
                                    Margin = new Thickness(8, 0, 0, 0),
                                    Width = 16,
                                    Height = 16,
                                    VerticalAlignment = VerticalAlignment.Center,
                                    Source = new BitmapImage(new Uri("ms-appx:///Assets/PhotoToys.png"))
                                }
                            }.Edit(x => x.PointerPressed += delegate
                            {
                                // Reference https://stackoverflow.com/questions/12761169/send-keys-through-sendinput-in-user32-dll
                                static void MultiKeyPress(params User32.VirtualKey[] keys){
                                    User32.INPUT[] inputs = new User32.INPUT[keys.Count() * 2];
                                    for(int a = 0; a < keys.Count(); ++a){
                                        for(int b = 0; b < 2; ++b){
                                            inputs[(b == 0) ? a : inputs.Count() - 1 - a].type = User32.InputType.INPUT_KEYBOARD;
                                            inputs[(b == 0) ? a : inputs.Count() - 1 - a].Inputs.ki = new User32.KEYBDINPUT {
                                                wVk = keys[a],
                                                wScan = 0,
                                                dwFlags = b is 0 ? 0 : User32.KEYEVENTF.KEYEVENTF_KEYUP,
                                                time = 0,
                                            };
                                        }
                                    }
                                    if (User32.SendInput(inputs.Length, inputs, Marshal.SizeOf(typeof(User32.INPUT))) == 0)
                                        throw new Exception();
                                }
                                MultiKeyPress(User32.VirtualKey.VK_MENU, User32.VirtualKey.VK_SPACE);
                            }),
                            new Border
                            {
                                Child = new StackPanel
                                {
                                    Orientation = Orientation.Horizontal,
                                    VerticalAlignment = VerticalAlignment.Center,
                                    Children =
                                    {
                                        new TextBlock
                                        {
                                            Text = "PhotoToys",
                                            Style = App.CaptionTextBlockStyle,
                                            Margin = new Thickness(4, 0, 0, 0)
                                        }.Assign(out TitleTextBlock)
                                    }
                                }
                            }.Edit(x => {
                                Grid.SetColumn(x, 1);
                                SetTitleBar(x);
                            })
                        }
                    }
                },
                #endregion
                new Border
                {

                }.Assign(out ContentContainerElement).Edit(x => Grid.SetRow(x, 1))
            }
        };

        Activated += OnWindowCreate;
    }
    IntPtr Handle => WinRT.Interop.WindowNative.GetWindowHandle(this);
    void OnWindowCreate(object _, WindowActivatedEventArgs _1)
    {
        var icon = User32.LoadImage(
            hInst: IntPtr.Zero,
            name: $@"{Package.Current.InstalledLocation.Path}\Assets\PhotoToys.ico".ToCharArray(),
            type: User32.ImageType.IMAGE_ICON,
            cx: 0,
            cy: 0,
            fuLoad: User32.LoadImageFlags.LR_LOADFROMFILE | User32.LoadImageFlags.LR_DEFAULTSIZE | User32.LoadImageFlags.LR_SHARED
        );
        var Handle = this.Handle;
        User32.SendMessage(Handle, User32.WindowMessage.WM_SETICON, (IntPtr)1, icon);
        User32.SendMessage(Handle, User32.WindowMessage.WM_SETICON, (IntPtr)0, icon);
        Activated -= OnWindowCreate;
    }
}
