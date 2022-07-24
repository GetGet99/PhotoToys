using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using OpenCvSharp;
using PhotoToys.Parameters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Microsoft.UI;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.Brushes;
namespace PhotoToys.Features.ImageGenerator;

class ImageGenerator : Category
{
    public override string Name { get; } = nameof(ImageGenerator).ToReadableName();
    public override string Description { get; } = "Generate Images!";
    public override IconElement? Icon { get; } = new SymbolIcon(Symbol.Add);
    public override Feature[] Features { get; } = new Feature[]
    {
        new SolidColor(),
        new Ellipse(),
        new Text()
    };
}
class SolidColor : Feature
{
    public override string Name { get; } = nameof(SolidColor).ToReadableName();
    public override string Description => "Generate the image of solid color (and/or border)";
    public override IconElement? Icon => new SymbolIcon((Symbol)0xE91b); // Photo
    protected override UIElement CreateUI()
    {
        UIElement? UIElement = null;
        UIElement = SimpleUI.GenerateLIVE(
            PageName: Name,
            PageDescription: Description,
            Parameters: new ParameterFromUI[]
            {
                new DoubleNumberBoxParameter("Width", 400).Assign(out var WidthParam),
                new DoubleNumberBoxParameter("Height", 300).Assign(out var HeightParam),
                new ColorPickerParameter("Background Color", Windows.UI.Color.FromArgb(255, 66, 66, 66)).Assign(out var BackgroundColorParam),
                new SimpleBorderParameter(DefaultColor: Windows.UI.Color.FromArgb(255, 66, 66, 66)).Assign(out var BorderParam)
            },
            AutoRunWhenCreate: true,
            OnExecute: x =>
            {
                var mat = new Mat(new Size(WidthParam.Result, HeightParam.Result),
                    MatType.CV_8UC4,
                    BackgroundColorParam.ResultAsScaler
                );

                if (BorderParam.IsEnabled)
                {
                    var oldmat = mat;
                    var (Top, Bottom, Left, Right) = BorderParam.Result;
                    mat = mat.CopyMakeBorder(
                        top: Top,
                        left: Left,
                        right: Right,
                        bottom: Bottom,
                        borderType: BorderTypes.Constant,
                        value: BorderParam.ColorAsScalar
                    );
                    oldmat.Dispose();
                }

                mat.ImShow(x);
            }
        );

        return UIElement;
    }
}
class Ellipse : Feature
{
    public override string Name { get; } = nameof(Ellipse).ToReadableName();
    public override string Description => "Generate the image of ellipse (and/or padding border)";
    public override IconElement? Icon => new SymbolIcon((Symbol)0xE91b); // Photo
    public static string ConvertAngle(double i) => i switch
    {
        > 0 => $"+{i:N0} (-{360 - i:N0})",
        < 0 => $"{i:N0} (+{360 + i:N0})",
        0 => "No Change",
        double.NaN => "NaN"
    };
    protected override UIElement CreateUI()
    {
        UIElement? UIElement = null;
        UIElement = SimpleUI.GenerateLIVE(
            PageName: Name,
            PageDescription: Description,
            Parameters: new ParameterFromUI[]
            {
                new DoubleNumberBoxParameter("Width", 400).Assign(out var WidthParam),
                new DoubleNumberBoxParameter("Height", 300).Assign(out var HeightParam),
                new DoubleSliderParameter("Start Angle", -180, 180, -180, DisplayConverter: ConvertAngle).Assign(out var StartAngleParam),
                new DoubleSliderParameter("End Angle", -180, 180, 180, DisplayConverter: ConvertAngle).Assign(out var EndAngleParam),
                new CheckboxParameter("Fill Mode", true).Assign(out var FillModeParam),
                new DoubleNumberBoxParameter("Thickness", 2, Min: 0).Assign(out var ThicknessParam)
                .AddDependency(FillModeParam, x => !x),
                new ColorPickerParameter("Color", Windows.UI.Color.FromArgb(255, 66, 66, 66)).Assign(out var ColorParam),
                new SimpleBorderParameter(DefaultColor: Colors.Transparent).Assign(out var BorderParam)
            },
            AutoRunWhenCreate: true,
            OnExecute: x =>
            {
                var size = new Size(WidthParam.Result, HeightParam.Result);
                if (!FillModeParam.Result)
                {
                    var halfthick = ThicknessParam.Result.Round() + 2;
                    size.Width += halfthick;
                    size.Height += halfthick;
                }
                var mat = new Mat(size,
                    MatType.CV_8UC4,
                    new Scalar(0, 0, 0, 0)
                );
                mat.Ellipse(
                    center: new Point(mat.Width / 2d, mat.Height / 2d),
                    axes: new Size(WidthParam.Result / 2d, HeightParam.Result / 2d),
                    angle: 0,
                    startAngle: StartAngleParam.Result + 180,
                    endAngle: EndAngleParam.Result + 180,
                    color: ColorParam.ResultAsScaler,
                    thickness: FillModeParam.Result ? -1 : (int)ThicknessParam.Result,
                    lineType: LineTypes.AntiAlias,
                    shift: 0
                );

                if (BorderParam.IsEnabled)
                {
                    var oldmat = mat;
                    var (Top, Bottom, Left, Right) = BorderParam.Result;
                    mat = mat.CopyMakeBorder(
                        top: Top,
                        left: Left,
                        right: Right,
                        bottom: Bottom,
                        borderType: BorderTypes.Constant,
                        value: BorderParam.ColorAsScalar
                    );
                    oldmat.Dispose();
                }

                mat.ImShow(x);
            }
        );

        return UIElement;
    }
}
class Text : Feature
{
    public override string Name { get; } = nameof(Text).ToReadableName();
    public override string Description => "Generate the image of text (and/or padding border)";
    public override IconElement? Icon => new SymbolIcon((Symbol)0xE91b); // Photo
    protected override UIElement CreateUI()
    {
        TextBlock TextBlock = new TextBlock();
        UIElement? UIElement = null;
        UIElement = SimpleUI.GenerateLIVE(
            PageName: Name,
            PageDescription: Description,
            Parameters: new ParameterFromUI[]
            {
                new StringTextBoxParameter("Text", "Type Text Here", LiveUpdate: true).Assign(out var TextParam),
                new DoubleNumberBoxParameter("Font Size", 32).Assign(out var FontSizeParam),
                new SelectParameter<string>("Font Family", CanvasTextFormat.GetSystemFontFamilies(), ConverterToDisplay: x => (x, x))
                .Assign(out var FontParam),
                new ColorPickerParameter("Text Color", Windows.UI.Color.FromArgb(255, 66, 66, 66)).Assign(out var TextColorParam),
                new SimpleBorderParameter(DefaultColor: Colors.White).Assign(out var BorderParam)
            },
            OnExecute: async x =>
            {
                var Text = TextParam.Result;
                if (Text.Length == 0) return;
                var FontSize = FontSizeParam.Result;
                var Font = FontParam.Result;
                var Color = TextColorParam.Result;
                var bytes = await Task.Run(async delegate
                {
                    var device = CanvasDevice.GetSharedDevice();
                    var canvas1 = new CanvasRenderTarget(device, 1, 1, 96);
                    var format = new CanvasTextFormat
                    {
                        FontSize = (float)FontSize,
                        FontFamily = Font,
                        WordWrapping = CanvasWordWrapping.NoWrap
                    };
                    Windows.Foundation.Rect bound;
                    using (canvas1)
                    {
                        using var DrawingSession1 = canvas1.CreateDrawingSession();
                        CanvasTextLayout textLayout = new(DrawingSession1, Text, format, 0f, 0f);
                        bound = textLayout.DrawBounds;
                    }
                    using (var canvas2 = new CanvasRenderTarget(device, (float)bound.Width, (float)bound.Height, 96))
                    {
                        using (var DrawingSession2 = canvas2.CreateDrawingSession())
                        {
                            CanvasTextLayout textLayout = new(DrawingSession2, Text, format, 0f, 0f);
                            //DrawingSession2.Clear(Colors.Red);
                            DrawingSession2.DrawTextLayout(textLayout, new System.Numerics.Vector2 { X = (float)-bound.Left, Y = (float)-bound.Top}, new CanvasSolidColorBrush(DrawingSession2, Color));
                        }

                        using var ms = new MemoryStream();
                        await canvas2.SaveAsync(ms.AsRandomAccessStream(), CanvasBitmapFileFormat.Png);
                        return ms.ToArray();
                    }
                });
                var mat = Mat.FromImageData(bytes, ImreadModes.Unchanged);

                if (UIElement is null) return;


                if (BorderParam.IsEnabled)
                {
                    var oldmat = mat;
                    var (Top, Bottom, Left, Right) = BorderParam.Result;
                    mat = mat.CopyMakeBorder(
                        top: Top,
                        left: Left,
                        right: Right,
                        bottom: Bottom,
                        borderType: BorderTypes.Constant,
                        value: BorderParam.ColorAsScalar
                    );
                    oldmat.Dispose();
                }

                mat.ImShow(x);
            }
        );

        return UIElement;
    }
}
