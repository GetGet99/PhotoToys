using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoToys.Parameters;

class CheckboxParameter : IParameterFromUI<bool>
{
    public event Action? ParameterReadyChanged;
    public event Action? ParameterValueChanged;
    public CheckboxParameter(string Name, bool Default)
    {
        this.Name = Name;
        this.Result = Default;
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
                    )
                }
            }
        };

        ParameterReadyChanged?.Invoke();
        ParameterValueChanged?.Invoke();
    }
    public bool ResultReady => true;
    public bool Result {get; private set; }

    public string Name { get; private set; }

    public FrameworkElement UI { get; }
}