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
class DoubleParameterWithBounds : ParameterDefinition<double>
{
    string Name;
    double Min, Max, StartingValue, Step, UISliderWidth;
    Func<double, string>? DisplayConverter;
    public DoubleParameterWithBounds(string Name, double Min, double Max, double StartingValue, double Step = 1, double UISliderWidth = 300, Func<double, string>? DisplayConverter = null)
    {
        this.Name = Name;
        this.Min = Min;
        this.Max = Max;
        this.StartingValue = StartingValue;
        this.Step = Step;
        this.UISliderWidth = UISliderWidth;
        this.DisplayConverter = DisplayConverter;
    }
    public static DoubleParameterWithBounds CreateAsPercentage(string Name, double StartingValue, double Step = 0.001, double UISliderWidth = 300)
        => new DoubleParameterWithBounds(Name: Name, Min: 0, Max: 100, StartingValue: StartingValue * 100, Step: Step * 100,
            UISliderWidth: UISliderWidth, DisplayConverter: x => $"{x:N1}%");

    Parameter CreateParameter(double value)
        => new(Name, value);

    IParameter<double> ParameterDefinition<double>.CreateParameter(double value)
        => CreateParameter(value);

    ParameterFromUI<double> ParameterDefinition<double>.CreateUserInterface()
        => CreateUserInterface();

    DoubleSliderParameter CreateUserInterface()
        => new DoubleSliderParameter(Name: Name, Min: Min, Max: Max, StartingValue: StartingValue, Step: Step, UISliderWidth: UISliderWidth);

    struct Parameter : IParameter<double>
    {
        public Parameter(string name, double value)
        {
            Value = value;
            Name = name;
        }

        public double Value { get; }

        public string Name { get; }

    }
    public class DoubleSliderParameter : ParameterFromUI<double>
    {
        public override event Action? ParameterReadyChanged, ParameterValueChanged;
        public DoubleSliderParameter(string Name, double Min, double Max, double StartingValue, double Step = 1, double UISliderWidth = 300, Func<double, string>? DisplayConverter = null)
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
                                new SimpleUI.Slider
                                {
                                    Minimum = 0, //Min,
                                    Maximum = Max - Min, //Max,
                                    StepFrequency = Step,
                                    Value = StartingValue - Min,
                                    Width = UISliderWidth,
                                    Margin = new Thickness(0, 0, 10, 0),
                                    ThumbToolTipValueConverter = new SimpleUI.Slider.Converter(Min, Max, DisplayConverter)
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

    //class PercentSliderParameter : DoubleSliderParameter
    //{
    //    public PercentSliderParameter(string Name, double StartingValue, double Step = 0.001, double SliderWidth = 300)
    //        : base(Name, 0, 100, StartingValue * 100, Step * 100, SliderWidth, x => $"{x:N1}%") { }
    //    public new double Result => base.Result / 100;
    //}
    //class IntSliderParameter : DoubleSliderParameter
    //{
    //    public IntSliderParameter(string Name, int Min, int Max, int StartingValue, int Step = 1, double SliderWidth = 300, Func<int, string>? DisplayFunc = null)
    //        : base(Name, Min, Max, StartingValue, Step, SliderWidth, x => DisplayFunc?.Invoke((int)x) ?? x.ToString("N0")) { }
    //    public new int Result => (int)base.Result;
    //}

}