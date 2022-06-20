using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoToys.Parameters;

class SelectParameter<T> : IParameterFromUI<T>
{
    public event Action? ParameterReadyChanged, ParameterValueChanged;
    public static object ConverterToDisplayDefault(T item) => item?.ToString()?.ToReadableName() ?? throw new NullReferenceException();
    public SelectParameter(string Name, IList<T> Items, int StartingIndex = 0, Func<T, object>? ConverterToDisplay = null)
    {
        if (ConverterToDisplay == null) ConverterToDisplay = ConverterToDisplayDefault;
        Debug.Assert(StartingIndex >= 0 && StartingIndex < Items.Count);
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
                    new ComboBox
                    {
                        
                    }
                    .Edit(x => Grid.SetColumn(x, 2))
                    .Edit(x => x.Items.AddRange(Items.Select(x => new ComboBoxItem
                    {
                        Content = ConverterToDisplay.Invoke(x),
                        Tag = x
                    })))
                    .Edit(x => x.SelectionChanged += delegate
                    {
                        if (x.SelectedValue is ComboBoxItem item && item.Tag is T selecteditem)
                        {
                            Result = selecteditem;
                            ParameterValueChanged?.Invoke();
                        }
                    })
                    .Edit(x => x.SelectedIndex = StartingIndex)
                }
            }
        };
        Result = Items[StartingIndex];
        ParameterReadyChanged?.Invoke();
    }
    public bool ResultReady => true;
    public T Result {get; private set; }

    public string Name { get; private set; }

    public FrameworkElement UI { get; }
}