using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using OpenCvSharp;
using Microsoft.UI.Xaml.Media;

namespace PhotoToys.Parameters;
class ColorPickerParameter : ParameterFromUI<Color>
{
    public override event Action? ParameterReadyChanged;
    public override event Action? ParameterValueChanged;
    public ColorPickerParameter(string Name, Color Default, bool AllowAlpha = true)
    {
        this.Name = Name;
        UI = SimpleUI.GenerateSimpleParameter(
            Name: Name,
            Element: 
            new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Children =
                {
                    new Button
                    {
                        Width = 30,
                        Height = 30,
                        Background = new SolidColorBrush(Default).Assign(out var brush),
                        //Content = "Select Color",
                        Flyout = new Flyout
                        {
                            Content = new ColorPicker
                            {
                                Color = Default,
                                IsColorPreviewVisible = true,
                                IsColorSliderVisible = true,
                                IsHexInputVisible = true,
                                IsAlphaEnabled = AllowAlpha,
                                IsAlphaTextInputVisible = AllowAlpha,
                            }.Edit(x => x.ColorChanged += delegate
                            {
                                var color = x.Color;
                                _Result = color;
                                brush.Color = color;
                                ValueChangedWhenSettled();
                            })
                        }
                    }
                }
            }
        );
        _Result = Default;
        ParameterReadyChanged?.Invoke();
        ParameterValueChanged?.Invoke();
    }
    DispatcherTimer? settledTimer;
    void ValueChangedWhenSettled()
    {
        if (settledTimer == null)
        {
            settledTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(.4)
            };
            settledTimer.Tick += delegate
            {
                settledTimer.Stop();
                ParameterValueChanged?.Invoke();
                settledTimer = null;
            };
            settledTimer.Start();
        }
        else if (settledTimer.IsEnabled)
            settledTimer.Start();   // resets the timer...
    }
    public override bool ResultReady => true;
    public Color _Result;
    public override Color Result => _Result;
    public Scalar ResultAsScaler => new(_Result.B, _Result.G, _Result.R, _Result.A);


    public override string Name { get; }

    public override FrameworkElement UI { get; }
}