using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoToys.Parameters;

interface IParameterFromUI
{
    event Action ParameterReadyChanged;
    string Name { get; }
    FrameworkElement UI { get; }
    bool ResultReady { get; }
}
interface IParameterFromUI<T> : IParameterFromUI
{
    T Result { get; }
}
