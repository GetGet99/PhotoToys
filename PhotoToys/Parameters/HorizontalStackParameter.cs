using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoToys.Parameters;

class HorizontalStackParameter : IParameterFromUI
{
    public event Action? ParameterReadyChanged;
    IEnumerable<IParameterFromUI> Parameters;
    public HorizontalStackParameter(IEnumerable<IParameterFromUI> parameters, string Name = "")
    {
        Parameters = parameters;
        this.Name = Name;
        Grid Grid;
        UI = Grid = new Grid();

        foreach (var param in parameters)
        {
            Grid.ColumnDefinitions.Add(new ColumnDefinition());
            Grid.Children.Add(param.UI);
        }
        ParameterReadyChanged?.Invoke();
    }
    public bool ResultReady => Parameters.All(x => x.ResultReady);
    public int Result {get; private set; }

    public string Name { get; private set; }

    public FrameworkElement UI { get; }
}