using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoToys.Parameters;

public abstract class ParameterFromUI
{
    public abstract event Action? ParameterReadyChanged;
    public abstract event Action? ParameterValueChanged;
    public abstract string Name { get; }
    public abstract FrameworkElement UI { get; }
    public abstract bool ResultReady { get; }
    public ParameterFromUI AddDependency<T>(ParameterFromUI<T> param, Func<T, bool> on, bool onNoResult = true)
    {
        void Update()
        {
            if (param.ResultReady)
                UI.Visibility = on.Invoke(param.Result) ? Visibility.Visible : Visibility.Collapsed;
            else
                UI.Visibility = onNoResult ? Visibility.Visible : Visibility.Collapsed;
        }
        param.ParameterValueChanged += Update;
        Update();
        return this;
    }
}
public abstract class ParameterFromUI<T> : ParameterFromUI
{
    public abstract T Result { get; }
}
//public class VoidParameter : ParameterFromUI
//{
//    public string Name { get; }

//    public FrameworkElement UI { get; }

//    public bool ResultReady => true;

//    public event Action? ParameterReadyChanged;
//    public event Action? ParameterValueChanged;
//    public VoidParameter(string Name, FrameworkElement uI)
//    {
//        this.Name = Name;
//        UI = uI;
//        ParameterReadyChanged?.Invoke();
//    }
//}