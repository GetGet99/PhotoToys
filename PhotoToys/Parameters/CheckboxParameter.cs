using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoToys.Parameters;
abstract class _CheckboxParameter : ParameterFromUI<bool>
{
    // Intermedite layer to make Result get set possible
    public override bool Result => GetResult();
    protected abstract bool GetResult();
}
class CheckboxParameter : _CheckboxParameter
{
    public override event Action? ParameterReadyChanged;
    public override event Action? ParameterValueChanged;
    bool InvisibleResult;
    CheckBox CheckBox;
    public CheckboxParameter(string Name, bool Default, bool? InvisibleResult = null)
    {
        if (InvisibleResult is null) this.InvisibleResult = Default;
        else this.InvisibleResult = InvisibleResult.Value;
        this.Name = Name;
        UI = new Border
        {
            CornerRadius = new CornerRadius(16),
            Padding = new Thickness(16),
            Style = App.CardBorderStyle,
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
                    new CheckBox
                    {
                        Padding = new Thickness(0),
                        MinWidth = 0,
                        IsChecked = Default
                    }
                    .Edit(
                        x =>
                        {
                            x.Checked += delegate
                            {
                                Result = true;
                                ParameterValueChanged?.Invoke();
                            };
                            x.Unchecked += delegate
                            {
                                Result = false;
                                ParameterValueChanged?.Invoke();
                            };
                            Grid.SetColumn(x, 2);
                        }
                    ).Assign(out CheckBox)
                }
            }
        };
        Result = Default;
        UI.RegisterPropertyChangedCallback(UIElement.VisibilityProperty, delegate
        {
            _Result = UI.Visibility == Visibility.Visible ? (CheckBox.IsChecked ?? false) : this.InvisibleResult;
            ParameterValueChanged?.Invoke();
        });
        ParameterReadyChanged?.Invoke();
        ParameterValueChanged?.Invoke();
    }
    public override bool ResultReady => true;
    public bool _Result;
    public new bool Result {
        get => _Result;
        set {
            CheckBox.IsChecked = value;
            _Result = value;
        }
    }
    protected override bool GetResult() => Result;


    public override string Name { get; }

    public override FrameworkElement UI { get; }
}