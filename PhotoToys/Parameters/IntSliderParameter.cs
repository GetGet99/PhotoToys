using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Input;
namespace PhotoToys.Parameters;

class IntSliderParameter : DoubleSliderParameter
{
    public IntSliderParameter(string Name, int Min, int Max, int StartingValue, int Step = 1, double SliderWidth = 300, Func<int, string>? DisplayFunc = null)
        : base(Name, Min, Max, StartingValue, Step, SliderWidth, x => DisplayFunc?.Invoke((int)x) ?? x.ToString("N0")) { }
    public new int Result => (int)base.Result;
}
class NewSlider : Slider
{
    public event Action? ValueChangedSettled;
    DispatcherTimer? settledTimer;
    public void RunWhenSettled()
    {
        if (settledTimer == null)
        {
            settledTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(.4)
            };
            settledTimer.Tick += delegate
            {
                settledTimer.Stop();
                ValueChangedSettled?.Invoke();
                settledTimer = null;
            };
            settledTimer.Start();
        }
        else if (settledTimer.IsEnabled)
            settledTimer.Start();   // resets the timer...
    }
    public NewSlider()
    {
        ValueChanged += (_, _) => RunWhenSettled();
    }
}