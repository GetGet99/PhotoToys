using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Controls.Primitives;
namespace PhotoToys.Parameters;

class DoubleSliderParameter : ParameterFromUI<double>
{
    public class Converter : IValueConverter
    {
        double Min, Max;
        Func<double, string> DisplayConverter;
        public Converter(double Min, double Max, Func<double, string> DisplayConverter)
        {
            this.Min = Min;
            this.Max = Max;
            this.DisplayConverter = DisplayConverter;
        }
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is not double Value) throw new ArgumentException();
            return DisplayConverter.Invoke(Value + Min);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
    public override event Action? ParameterReadyChanged, ParameterValueChanged;
    public DoubleSliderParameter(string Name, double Min, double Max, double StartingValue, double Step = 1, double SliderWidth = 300, Func<double, string>? DisplayConverter = null)
    {
        if (DisplayConverter == null) DisplayConverter = x => x.ToString("N4");
        Debug.Assert(StartingValue >= Min && StartingValue <= Max);
        this.Name = Name;
        UI = new Border
        {
            CornerRadius = new CornerRadius(16),
            Padding = new Thickness(16),
            Style = App.LayeringBackgroundBorderStyle,
            BorderThickness = new Thickness(1),
            BorderBrush = App.CardStrokeColorDefaultBrush,
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
                            new NewSlider
                            {
                                Minimum = 0, //Min,
                                Maximum = Max - Min, //Max,
                                StepFrequency = Step,
                                //TickFrequency = Step >= 1 ? Step : 0,
                                //TickPlacement = TickPlacement.Outside,
                                Value = StartingValue - Min,
                                Width = SliderWidth,
                                Margin = new Thickness(0, 0, 10, 0),
                                ThumbToolTipValueConverter = new Converter(Min, Max, DisplayConverter)
                            }.Edit(x =>
                            {
                                x.ValueChanged += delegate
                                {
                                    _Result = x.Value + Min;
                                    ValueShow.Text = DisplayConverter.Invoke(Result);
                                };
                                x.ValueChangedSettled += delegate
                                {
                                    ParameterValueChanged?.Invoke();
                                };
                            }).Assign(out var slider),
                            new Button
                            {
                                Content = new SymbolIcon(Symbol.Undo)
                            }.Edit(x => x.Click += delegate
                            {
                                slider.Value = StartingValue - Min;
                            })
                        }
                    }.Edit(x => Grid.SetColumn(x, 2))
                }
            }
        };

        _Result = StartingValue;
        ParameterReadyChanged?.Invoke();
        ParameterValueChanged?.Invoke();
        ValueShow.Text = DisplayConverter.Invoke(Result);
    }
    public override bool ResultReady => true;
    double _Result;
    public override double Result => _Result;

    public override string Name { get; }

    public override FrameworkElement UI { get; }
}

class PercentSliderParameter : DoubleSliderParameter
{
    public PercentSliderParameter(string Name, double StartingValue, double Step = 0.001, double SliderWidth = 300)
        : base(Name, 0, 100, StartingValue * 100, Step * 100, SliderWidth, x => $"{x:N1}%") { }
    public new double Result => base.Result / 100;
}