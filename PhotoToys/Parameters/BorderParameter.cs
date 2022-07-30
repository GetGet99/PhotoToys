using Microsoft.UI;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static PTMS.OpenCvExtension;
namespace PhotoToys.Parameters
{
    internal class SimpleBorderParameter : ParameterFromUI<(int Top, int Bottom, int Left, int Right)>
    {
        public SimpleBorderParameter(string Name = "Border", bool Color = true, Windows.UI.Color? DefaultColor = null)
        {
            if (DefaultColor is null) DefaultColor = Colors.Transparent;

            this.Name = Name;
            var UI = SimpleUI.GenerateVerticalParameter(Name,
                new SimpleUI.FluentVerticalStack
                {
                    Children =
                    {
                        new CheckboxParameter("Enable Border", false)
                        .Assign(out var BorderEnabledParam)
                        .Edit(x => x.ParameterValueChanged += () => IsEnabled = x.Result)
                        .UI,
                        new DoubleNumberBoxParameter("Border Top", 50)
                        .Assign(out var BorderTopParam)
                        .Edit(x => x.ParameterValueChanged += () => Top = x.Result.Round())
                        .AddDependency(BorderEnabledParam, x => x)
                        .UI,
                        new DoubleNumberBoxParameter("Border Bottom", 50)
                        .Assign(out var BorderBottomParam)
                        .Edit(x => x.ParameterValueChanged += () => Bottom = x.Result.Round())
                        .AddDependency(BorderEnabledParam, x => x)
                        .UI,
                        new DoubleNumberBoxParameter("Border Left", 50)
                        .Assign(out var BorderLeftParam)
                        .Edit(x => x.ParameterValueChanged += () => Left = x.Result.Round())
                        .AddDependency(BorderEnabledParam, x => x)
                        .UI,
                        new DoubleNumberBoxParameter("Border Right", 50)
                        .Assign(out var BorderRightParam)
                        .Edit(x => x.ParameterValueChanged += () => Right = x.Result.Round())
                        .AddDependency(BorderEnabledParam, x => x)
                        .UI,
                    }
                }.Assign(out var Container)
            );
            foreach (var x in new ParameterFromUI[] {
                BorderEnabledParam,
                BorderTopParam,
                BorderBottomParam,
                BorderLeftParam,
                BorderRightParam
            })
            {
                x.ParameterReadyChanged += () => ParameterReadyChanged?.Invoke();
                x.ParameterValueChanged += () =>
                {
                    ParameterValueChanged?.Invoke();
                    Container.InvalidateArrange();
                };
            }
            if (Color)
            {
                Container.Children.Add(new ColorPickerParameter("Color", DefaultColor.Value)
                    .Assign(out var BorderColorParam)
                    .Edit(x => x.ParameterValueChanged += () => this.Color = x.Result)
                        .AddDependency(BorderEnabledParam, x => x)
                    .UI
                );
                BorderColorParam.ParameterReadyChanged += () => ParameterReadyChanged?.Invoke();
                BorderColorParam.ParameterValueChanged += () => ParameterValueChanged?.Invoke();
            }

            this.UI = UI;
            ParameterReadyChanged?.Invoke();
            ParameterValueChanged?.Invoke();
        }
        public bool IsEnabled { get; private set; }
        int Top = 50, Bottom = 50, Left = 50, Right = 50;
        public override (int Top, int Bottom, int Left, int Right) Result => IsEnabled ? (Top, Bottom, Left, Right) : (0, 0, 0, 0);
        public Windows.UI.Color Color { get; private set; }
        public OpenCvSharp.Scalar ColorAsScalar => new OpenCvSharp.Scalar(Color.B, Color.G, Color.R, Color.A);


        public override string Name { get; }

        public override FrameworkElement UI { get; }

        public override bool ResultReady => true;

        public override event Action? ParameterReadyChanged;
        public override event Action? ParameterValueChanged;
    }
}
