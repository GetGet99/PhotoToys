using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
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
    public async Task<bool> IsReady(string _, StringTextBoxParameter _1) => await Task.Run(() => true);
    public StringTextBoxParameter(string Name, string Placeholder, bool LiveUpdate = false, string Default = "", Func<string, StringTextBoxParameter, Task<bool>>? IsReady = null, FontFamily? Font = null)
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
                    PlaceholderText = Placeholder,
                    MinWidth = 300,
                    IsSpellCheckEnabled = false,
                }.Assign(out TextBox).Edit(textbox => textbox.TextChanged += async delegate
                {
                    if (LiveUpdate)
                    {
                        if (Result != textbox.Text)
                        {
                            _ResultReady = await IsReady.Invoke(textbox.Text, this);
                            if (ResultReady)
                            {
                                _Result = textbox.Text;
                                ParameterReadyChanged?.Invoke();
                                ParameterValueChanged?.Invoke();
                            }
                        }
                    } else
                    {
                        var newResultReady = await IsReady.Invoke(textbox.Text, this);
                        ConfirmButton.IsEnabled = newResultReady;
                    }
                }),
                new Button
                {
                    Margin = new Thickness(10, 0, 0, 0),
                    Visibility = LiveUpdate ? Visibility.Collapsed : Visibility.Visible,
                    Content = "Confirm"
                }.Edit(x => x.Click += async delegate
                {
                    if (await IsReady.Invoke(TextBox.Text, this))
                    {
                        if (!_ResultReady)
                        {
                            _ResultReady = true;
                            ParameterReadyChanged?.Invoke();
                        }
                        _Result = TextBox.Text;
                        ParameterValueChanged?.Invoke();
                    }
                }).Assign(out ConfirmButton)
            }
        });
        TextBox.Text = Result;
        if (Font is not null)
            TextBox.FontFamily = Font;
    }
    public readonly TextBox TextBox;
    public readonly Button ConfirmButton;
    bool _ResultReady = false;
    public override bool ResultReady => _ResultReady;
    string _Result;
    public override string Result => _Result;

    public override string Name { get; }

    public override FrameworkElement UI { get; }
}