using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OpenCvSharp;
namespace PhotoToys.Parameters;

class NLocationsPickerParameter<MatDisplayerType> : ParameterFromUI<Point[]> where MatDisplayerType : IMatDisplayer, new()
{
    public MatDisplayerType MatDisplayer { get; }
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
    public static NLocationsPickerParameter<MatDisplayerType> CreateWithImageParameter(string Name, int LocationAmount, ImageParameter ImageParameter, double MatDisplayerHeight = 500)
    {
        var locaPicker = new NLocationsPickerParameter<MatDisplayerType>(Name: Name, LocationAmount: LocationAmount,
            MatDisplayerHeight: MatDisplayerHeight);
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
    int Index = -1;
    public NLocationsPickerParameter(string Name, int LocationAmount, double MatDisplayerHeight = 500)
    {
        _Result = new Point?[LocationAmount];
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
            if (Index + 1 == LocationAmount)
            {
                Array.Fill(_Result, null);
                Index = -1;
            }
            await Task.Run(delegate
            {
                var pointlocX = Mat_.Width;
                var UIShowScale = Mat_.Width / uiw;
                pty = (int)(pt.Y * UIShowScale);
                ptx = (int)(pt.X * UIShowScale);
                cvpt = new Point(ptx, pty);
                var color = new Scalar(0, 0, 255, 255);
                NewMat = Mat_.Clone();
                foreach (var pt in _Result)
                {
                    if (pt is null) continue;
                    NewMat.Ellipse(pt.Value, new Size(15, 15), 0, 0, 360, color: color, thickness: 5, lineType: LineTypes.AntiAlias);
                    NewMat.Ellipse(pt.Value, new Size(3, 3), 0, 0, 360, color: color, thickness: 1, lineType: LineTypes.AntiAlias);
                }
                NewMat.Polylines(new IEnumerable<Point>[] {
                    (
                        from x in _Result
                        where x.HasValue
                        select x.Value
                    ).Append(cvpt)
                }, isClosed: true, color: color, thickness: 2);
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
                _Result[++Index] = cvpt;
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
            ui,
            tb
        );
    }
    void MatUpdate()
    {
        Array.Fill(_Result, null);
        Index = -1;
        var oldmat = MatDisplayer.Mat;
        MatDisplayer.Mat = Mat_;
        oldmat?.Dispose();
        ParameterReadyChanged?.Invoke();
        ParameterValueChanged?.Invoke();
    }
    Point?[] _Result;
    public override Point[] Result => _Result.Select(x => x ?? throw new InvalidOperationException()).ToArray();
    
    public override string Name { get; }

    public override FrameworkElement UI { get; }

    public override bool ResultReady => Mat is not null && _Result.All(x => x.HasValue);

    public override event Action? ParameterReadyChanged;
    public override event Action? ParameterValueChanged;
}
