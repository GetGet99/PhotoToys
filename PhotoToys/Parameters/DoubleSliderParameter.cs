﻿using Microsoft.UI.Xaml;
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
using Windows.Globalization.NumberFormatting;

namespace PhotoToys.Parameters;

class DoubleSliderParameter : ParameterFromUI<double>
{
    public readonly NewSlider Slider;
    public override event Action? ParameterReadyChanged, ParameterValueChanged;
    public DoubleSliderParameter(string Name, double Min, double Max, double StartingValue, double Step = 1, double SliderWidth = 300, Func<double, string>? DisplayConverter = null)
    {
        if (DisplayConverter == null) DisplayConverter = x => x.ToString("N4");
        Debug.Assert(StartingValue >= Min && StartingValue <= Max);
        this.Name = Name;
        UI = SimpleUI.GenerateSimpleParameter(
            Name: Name,
            Element: new StackPanel
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
                        IntermediateValue = StartingValue - Min,
                        ThumbToolTipValueConverter = new NewSlider.Converter(Min, Max, DisplayConverter)
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
                    }).Assign(out Slider),
                    new Button
                    {
                        Content = new SymbolIcon(Symbol.Undo)
                    }.Edit(x => x.Click += delegate
                    {
                        Slider.Value = StartingValue - Min;
                    })
                }
    }.Edit(x => Grid.SetColumn(x, 2))
        );

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
class DoubleNumberBoxParameter : ParameterFromUI<double>
{
    public override event Action? ParameterReadyChanged, ParameterValueChanged;
    public DoubleNumberBoxParameter(string Name, double StartingValue, double? Min = null, double? Max = null, int FractionDegit = 0, double NumberBoxMinWidth = 150)
    {
        Debug.Assert((!Min.HasValue || StartingValue >= Min.Value) && (!Max.HasValue || StartingValue <= Max.Value));
        this.Name = Name;
        UI = SimpleUI.GenerateSimpleParameter(
            Name: Name,
            Element: new NumberBox
            {
                NumberFormatter = new DecimalFormatter
                {
                    FractionDigits = FractionDegit,
                },
                Value = StartingValue,
                MinWidth = NumberBoxMinWidth
            }.Assign(out var NumberBox)
        );
        if (Min.HasValue)
            NumberBox.Minimum = Min.Value;
        if (Max.HasValue)
            NumberBox.Minimum = Max.Value;
        NumberBox.ValueChanged += delegate
        {
            _Result = NumberBox.Value;
            ParameterValueChanged?.Invoke();
        };
        _Result = StartingValue;
        ParameterReadyChanged?.Invoke();
        ParameterValueChanged?.Invoke();
    }
    public override bool ResultReady => true;
    double _Result;
    public override double Result => _Result;

    public override string Name { get; }

    public override FrameworkElement UI { get; }
}
