using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OpenCvSharp;
namespace PhotoToys.Parameters;

class LocationPickerParameter<MatDisplayerType> : ParameterFromUI<Point> where MatDisplayerType : IMatDisplayer, new()
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
    public static LocationPickerParameter<MatDisplayerType> CreateWithImageParameter(string Name, ImageParameter ImageParameter, double MatDisplayerHeight = 500)
    {
        var locaPicker = new LocationPickerParameter<MatDisplayerType>(Name: Name, MatDisplayerHeight: MatDisplayerHeight);
        void Proc()
        {
            var resultReady = ImageParameter.ResultReady;
            if (resultReady)
                locaPicker.Mat = ImageParameter.Result.InplaceInsertAlpha(ImageParameter.AlphaResult);
            else
                locaPicker.Mat = null;
            locaPicker.UI.Visibility = resultReady ? Visibility.Visible : Visibility.Collapsed;
        }
        Proc();
        ImageParameter.ParameterValueChanged += Proc;
        return locaPicker;
    }
    public LocationPickerParameter(string Name, double MatDisplayerHeight = 500)
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
        async Task Process(Windows.Foundation.Point pt, bool final = false)
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
            if (final)
            {
                _Result = cvpt;
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
            await Process(pt.Position);
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
            await Process(pt.Position, final: true);
        };
        UI = SimpleUI.GenerateVerticalParameter(Name: Name,
            MatDisplayer.UIElement,
            tb
        );
    }
    void MatUpdate()
    {
        _Result = null;
        var oldmat = MatDisplayer.Mat;
        MatDisplayer.Mat = Mat_;
        oldmat?.Dispose();
        ParameterReadyChanged?.Invoke();
        ParameterValueChanged?.Invoke();
    }
    Point? _Result;
    public override Point Result => _Result ?? throw new InvalidOperationException();

    public override string Name { get; }

    public override FrameworkElement UI { get; }

    public override bool ResultReady => Mat is not null && _Result is not null;

    public override event Action? ParameterReadyChanged;
    public override event Action? ParameterValueChanged;
}
