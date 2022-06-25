using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Data;

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
    public class Converter : IValueConverter
    {
        double Min, Max;
        Func<double, string> DisplayConverter;
        public Converter(double Min, double Max, Func<double, string> DisplayConverter)
        {
            this.Min = Min;
            this.Max = Max;
            this.DisplayConverter = DisplayConverter;
        }
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is not double Value) throw new ArgumentException();
            return DisplayConverter.Invoke(Value + Min);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}