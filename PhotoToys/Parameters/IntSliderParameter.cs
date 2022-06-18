using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoToys.Parameters;

class IntSliderParameter : IParameterFromUI<int>
{
    public event Action? ParameterReadyChanged;
    public IntSliderParameter(string Name, int Min, int Max, int StartingValue, int Step = 1, double SliderWidth = 300)
    {
        Debug.Assert(StartingValue >= Min && StartingValue <= Max);
        this.Name = Name;
        UI = new Border
        {
            CornerRadius = new CornerRadius(16),
            Padding = new Thickness(16),
            Style = App.LayeringBackgroundBorderStyle,
            Child = new Grid
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
                    new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Children =
                        {
                            new TextBlock
                            {
                                Text = Name,
                                VerticalAlignment = VerticalAlignment.Center,
                                Margin = new Thickness(0, 0, 10, 0)
                            }.Assign(out var ValueShow),
                            new Slider
                            {
                                Minimum = Min,
                                Maximum = Max,
                                StepFrequency = Step,
                                Value = StartingValue,
                                Width = SliderWidth
                            }.Edit(x => x.ValueChanged += delegate
                            {
                                Result = (int)Math.Round(x.Value);
                                ValueShow.Text = Result.ToString();
                            })
                        }
                    }.Edit(x => Grid.SetColumn(x, 2))
                }
            }
        };

        Result = StartingValue;
        ParameterReadyChanged?.Invoke();
        ValueShow.Text = Result.ToString();
    }
    public bool ResultReady => true;
    public int Result {get; private set; }

    public string Name { get; private set; }

    public FrameworkElement UI { get; }
}