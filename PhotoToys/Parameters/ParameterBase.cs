using Microsoft.UI.Xaml;
using System;

namespace PhotoToys.Parameters;
public interface ParameterDefinition
{
    string Name { get; }
    ParameterFromUI CreateUserInterface();
}
public interface ParameterDefinition<T> : ParameterDefinition
{
    //IParameter<T> CreateParameter(T value);
}
public interface IParameter
{
    string Name { get; }
}
public interface IParameter<T> : IParameter
{
    T Value { get; }
}
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
    public abstract IParameter CreateParameter();
}
public abstract class ParameterFromUI<T> : ParameterFromUI
{
    public abstract T Result { get; }
}