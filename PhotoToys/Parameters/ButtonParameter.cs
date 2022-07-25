using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;

namespace PhotoToys.Parameters;

class ButtonParameter : ParameterFromUI
{
    public ButtonParameter(string Name, string ButtonText, Func<bool>? OnClick)
    {
        this.Name = Name;
        var Button = new Button
        {
            Content = ButtonText
        };
        Button.Click += delegate
        {
            if (OnClick?.Invoke() ?? true)
                ParameterValueChanged?.Invoke();
        };
        UI = SimpleUI.GenerateSimpleParameter(Name: Name, Element: Button);
        ParameterReadyChanged?.Invoke();
        ParameterValueChanged?.Invoke();
    }
    public override string Name { get; }

    public override FrameworkElement UI { get; }

    public override bool ResultReady => true;

    public override event Action? ParameterReadyChanged;
    public override event Action? ParameterValueChanged;
}
