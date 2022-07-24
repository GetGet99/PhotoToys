using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OpenCvSharp;
namespace PhotoToys.Parameters;

class RectLocationPickerParameter<MatDisplayerType> : ParameterFromUI<(int Top, int Bottom, int Left, int Right)> where MatDisplayerType : IMatDisplayer, new()
{
    MatDisplayerType MatDisplayer;
    Mat? Mat_;
    public Mat? Mat
    {
        get => Mat_;
        set
        {
            Mat_ = value;
            MatUpdate();
        }
    }
    public static RectLocationPickerParameter<MatDisplayerType> CreateWithImageParameter(string Name, ImageParameter ImageParameter, double MatDisplayerHeight = 500)
    {
        var locaPicker = new RectLocationPickerParameter<MatDisplayerType>(Name: Name, MatDisplayerHeight: MatDisplayerHeight);
        void Proc()
        {
            var resultReady = ImageParameter.ResultReady;
            locaPicker.UI.Visibility = resultReady ? Visibility.Visible : Visibility.Collapsed;
            if (resultReady)
                locaPicker.Mat = ImageParameter.Result.InplaceInsertAlpha(ImageParameter.AlphaResult);
            else
                locaPicker.Mat = null;
        }
        Proc();
        ImageParameter.ParameterValueChanged += Proc;
        return locaPicker;
    }
    public RectLocationPickerParameter(string Name, double MatDisplayerHeight = 500)
    {
        this.Name = Name;
        MatDisplayer = new MatDisplayerType
        {
            UIElement =
            {
                Height = MatDisplayerHeight
            },
            AllowTooltip = false,
            AllowDragDrop = false
        };
        MatDisplayer.MatImage.OverrideView = true;
        MatDisplayer.MatImage.ViewOverride += async newWindow =>
        {
            if (Mat_ is not null)
                await Mat_.ImShow("View", MatDisplayer.UIElement.XamlRoot, newWindow);
        };
        var tb = new TextBlock
        {
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        var ui = MatDisplayer.UIElement;
        async Task Process(Windows.Foundation.Point pt, bool first = false, bool last = false)
        {
            if (Mat_ is null) return;
            var uiw = ui.ActualWidth;
            var uih = ui.ActualHeight;
            double ptx = 0, pty = 0;
            Point cvpt = default;
            Mat? NewMat = null;
            await Task.Run(delegate
            {
                var pointlocX = Mat_.Width;
                var UIShowScale = Mat_.Width / uiw;
                pty = (int)(pt.Y * UIShowScale);
                ptx = (int)(pt.X * UIShowScale);
                cvpt = new Point(ptx, pty);
                var color = new Scalar(0, 0, 255, 255);
                NewMat = Mat_.Clone();
                if (_ResultStart is Point pt1)
                {
                    NewMat.Ellipse(pt1, new Size(15, 15), 0, 0, 360, color: color, thickness: 5, lineType: LineTypes.AntiAlias);
                    NewMat.Ellipse(pt1, new Size(3, 3), 0, 0, 360, color: color, thickness: 1, lineType: LineTypes.AntiAlias);
                    NewMat.Rectangle(pt1, cvpt, color: color, thickness: 1, lineType: LineTypes.AntiAlias);
                }
                NewMat.Ellipse(cvpt, new Size(15, 15), 0, 0, 360, color: color, thickness: 5, lineType: LineTypes.AntiAlias);
                NewMat.Ellipse(cvpt, new Size(3, 3), 0, 0, 360, color: color, thickness: 1, lineType: LineTypes.AntiAlias);
            });
            if (NewMat is not null)
            {
                var oldMat = MatDisplayer.Mat;
                MatDisplayer.Mat = NewMat;
                oldMat?.Dispose();
            }
            tb.Text = $"X = {ptx}, Y = {pty}";
            if (first || last)
            {
                if (first)
                    _ResultStart = cvpt;
                if (last)
                    _ResultEnd = cvpt;
                ParameterReadyChanged?.Invoke();
                ParameterValueChanged?.Invoke();
            }
        }
        var pressing = false;
        ui.PointerPressed += async (_, e) =>
        {
            pressing = true;
            MatDisplayer.AllowTooltip = true;
            var pt = e.GetCurrentPoint(ui);
            await Process(pt.Position, first: true);
        };
        ui.PointerMoved += async (_, e) =>
        {
            if (pressing)
            {
                var pt = e.GetCurrentPoint(ui);
                await Process(pt.Position);
            }
        };
        ui.PointerReleased += async (_, e) =>
        {
            pressing = false;
            MatDisplayer.AllowTooltip = false;
            var pt = e.GetCurrentPoint(ui);
            await Process(pt.Position, last: true);
        };
        UI = SimpleUI.GenerateVerticalParameter(Name: Name,
            MatDisplayer.UIElement,
            tb
        );
    }
    void MatUpdate()
    {
        _ResultStart = null;
        _ResultEnd = null;

        var oldmat = MatDisplayer.Mat;
        MatDisplayer.Mat = Mat_;
        oldmat?.Dispose();
        ParameterReadyChanged?.Invoke();
        ParameterValueChanged?.Invoke();
    }
    Point? _ResultStart;
    Point? _ResultEnd;
    public override (int Top, int Bottom, int Left, int Right) Result
    {
        get
        {
            var ResultStart = _ResultStart ?? throw new InvalidOperationException();
            var ResultEnd = _ResultEnd ?? throw new InvalidOperationException();

            var (Left, Right) = Extension.Order(ResultStart.X, ResultEnd.X);
            var (Top, Bottom) = Extension.Order(ResultStart.Y, ResultEnd.Y);

            return (Top, Bottom, Left, Right);
        }
    }

    public override string Name { get; }

    public override FrameworkElement UI { get; }

    public override bool ResultReady => !(Mat is null || _ResultStart is null || _ResultEnd is null);

    public override event Action? ParameterReadyChanged;
    public override event Action? ParameterValueChanged;
}
static partial class Extension
{
    public static (T Lower, T Upper) Order<T>(T Value1, T Value2) where T : IComparable<T>
    {
        switch (Value1.CompareTo(Value2))
        {
            case <= 0:
                return (Value1, Value2);
            case > 0:
                return (Value2, Value1);
        }
    }
}