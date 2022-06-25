using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoToys.Parameters;

class StringTextBoxParameter : ParameterFromUI<string>
{
    public override event Action? ParameterReadyChanged;
    public override event Action? ParameterValueChanged;
    public async Task<bool> IsReady(string _) => await Task.Run(() => true);
    public StringTextBoxParameter(string Name, string Placeholder, bool LiveUpdate = false, string Default = "", Func<string, Task<bool>>? IsReady = null)
    {
        if (IsReady == null) IsReady = this.IsReady;
        this.Name = Name;
        _Result = Default;

        UI = SimpleUI.GenerateSimpleParameter(Name, new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Children =
            {
                new TextBox
                {
                    PlaceholderText = Placeholder
                }.Assign(out var textbox).Edit(textbox => textbox.TextChanged += async delegate
                {
                    if (LiveUpdate)
                    {
                        if (Result != textbox.Text)
                        {
                            _ResultReady = await IsReady.Invoke(textbox.Text);
                            if (ResultReady)
                            {
                                _Result = textbox.Text;
                                ParameterReadyChanged?.Invoke();
                                ParameterValueChanged?.Invoke();
                            }
                        }
                    } else
                    {
                        var newResultReady = await IsReady.Invoke(textbox.Text);
                        if (ResultReady != newResultReady)
                        {
                            _ResultReady = newResultReady;
                            ParameterReadyChanged?.Invoke();
                        }
                    }
                }),
                new Button
                {
                    Margin = new Thickness(10, 0, 0, 0),
                    Visibility = LiveUpdate ? Visibility.Collapsed : Visibility.Visible,
                    Content = "Confirm"
                }.Edit(x => x.Click += async delegate
                {
                    if (await IsReady.Invoke(textbox.Text))
                    {
                        _Result = textbox.Text;
                        ParameterValueChanged?.Invoke();
                    }
                })
            }
        });
        textbox.Text = Result;
    }
    bool _ResultReady = false;
    public override bool ResultReady => _ResultReady;
    string _Result;
    public override string Result => _Result;

    public override string Name { get; }

    public override FrameworkElement UI { get; }
}