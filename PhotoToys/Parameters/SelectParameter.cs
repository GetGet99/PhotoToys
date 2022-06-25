using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoToys.Parameters;

class SelectParameter<T> : ParameterFromUI<T>
{
    ComboBox ComboBox;

    public ItemCollection Items => ComboBox.Items;
    public int SelectionIndex
    {
        get => ComboBox.SelectedIndex;
        set => ComboBox.SelectedIndex = value;
    }
    public override event Action? ParameterReadyChanged, ParameterValueChanged;
    Func<T, object> ConverterToDisplay;
    public static object ConverterToDisplayDefault(T item) => item?.ToString()?.ToReadableName() ?? throw new NullReferenceException();
    public SelectParameter(string Name, IList<T> Items, int StartingIndex = 0, Func<T, object>? ConverterToDisplay = null)
    {
        if (ConverterToDisplay == null) ConverterToDisplay = ConverterToDisplayDefault;
        this.ConverterToDisplay = ConverterToDisplay;
        Debug.Assert(StartingIndex >= 0 && StartingIndex < Items.Count);
        this.Name = Name;
        UI = SimpleUI.GenerateSimpleParameter(Name, new ComboBox
        {

        }
        .Assign(out ComboBox)
        .Edit(x => Grid.SetColumn(x, 2))
        .Edit(x => x.Items.AddRange(Items.Select(x => GenerateItem(x))))
        .Edit(x => x.SelectionChanged += delegate
        {
            if (x.SelectedValue is ComboBoxItem item && item.Tag is T selecteditem)
            {
                _Result = selecteditem;
                ParameterValueChanged?.Invoke();
            }
        })
        .Edit(x => x.SelectedIndex = StartingIndex));
        //    new Border
        //{
        //    CornerRadius = new CornerRadius(16),
        //    Padding = new Thickness(16),
        //    Style = App.LayeringBackgroundBorderStyle,
        //    Child = new Grid
        //    {
        //        ColumnDefinitions =
        //        {
        //            new ColumnDefinition { Width = GridLength.Auto },
        //            new ColumnDefinition(),
        //            new ColumnDefinition { Width = GridLength.Auto },
        //        },
        //        Children =
        //        {
        //            new TextBlock
        //            {
        //                Text = Name,
        //                VerticalAlignment = VerticalAlignment.Center,
        //            },
                    
        //        }
        //    }
        //};
        _Result = Items[StartingIndex];
        ParameterReadyChanged?.Invoke();
    }
    T _Result;
    public ComboBoxItem GenerateItem(T x)
        => new()
        {
            Content = ConverterToDisplay.Invoke(x),
            Tag = x
        };
    public override bool ResultReady => true;
    public override T Result => _Result;

    public override string Name { get; }

    public override FrameworkElement UI { get; }
}