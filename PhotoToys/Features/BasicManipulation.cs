using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OpenCvSharp;
using PhotoToys.Parameters;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Linq;
using System.Threading.Tasks;
using static PTMS.OpenCvExtension;
using DynamicLanguage;
using static DynamicLanguage.Extension;
namespace PhotoToys.Features.BasicManipulation;

class BasicManipulation : Category
{
    [DisplayText(
        DefaultEN: "Basic Manipulation",
        Thai: "การแต่งรูปขั้นพื้นฐาน",
        Sinhala: "මූලික හැසිරවීම"
    )]
    public override string Name { get; } = GetDisplayText<BasicManipulation>(nameof(Name));
    [DisplayText(
        DefaultEN: "Apply basic image manipulation techniques!",
        Thai: "แต่งรูปโดยใช้เทคนิกการแต่งรูปขั้นพื้นฐาน",
        Sinhala: "මූලික රූප හැසිරවීමේ ක්‍රම යොදන්න"
    )]
    public override string Description { get; } = GetDisplayText<BasicManipulation>(nameof(Description));
    public override IconElement? Icon { get; } = new SymbolIcon(Symbol.Edit);
    public override Feature[] Features { get; } = new Feature[]
    {
        new HSVManipulation(),
        new ImageBlending(),
        new Crop(),
        new Border(),
        new PaintBucket(),
        new ImageTransformation(),
        new PerspectiveTransform()
    };
}
class HSVManipulation : Feature
{
    public override IEnumerable<string> Allias => new string[] {
        "HSV",
        "Hue",
        "Saturation",
        "Value",
        "Brightness",
        "Color",
        "Change Color"
    };
    [DisplayText("HSV Manipulation", Thai: "ปรับ HSV",Sinhala: "HSV හැසිරවීම")]
    public override string Name { get; } = GetDisplayText<HSVManipulation>(nameof(Name));
    public override string DefaultName { get; } = GetDefaultText<HSVManipulation>(nameof(Name));
    [DisplayText(
        DefaultEN: "Change Hue, Saturation, and Brightness of an image",
        Thai: "ปรับค่าสี (Hue) ค่าความอิ่มตัว (Saturation) และค่าความสว่าง (Brightness) ของรูป",
        Sinhala: "රූපයක පැහැය, සන්තෘප්තිය සහ දීප්තිය වෙනස් කරන්න"
    )]
    public override string Description { get; } = GetDisplayText<HSVManipulation>(nameof(Description));
    [DisplayText(
        DefaultEN: "No Change",
        Thai: "ไม่เปลี่ยนแปลง",
        Sinhala: "වෙනසක් නැත"
    )]
    static string NoChangeText { get; } = GetDisplayText<HSVManipulation>(nameof(NoChangeText));
    [DisplayText(
        DefaultEN: "NaN",
        Thai: "ไม่มีค่า (NaN)",
        Sinhala: "සංඛ්‍යාවක් නොවේ"
    )]
    static string NaNText { get; } = GetDisplayText<HSVManipulation>(nameof(NaNText));
    public static string ConvertAngle(double i) => i switch
    {
        > 0 => $"+{i:N0} (-{360 - i:N0})",
        < 0 => $"{i:N0} (+{360 + i:N0})",
        0 => NoChangeText,
        double.NaN => NaNText
    };
    static string Convert(double i) => i > 0 ? $"+{i:N0}" : i.ToString("N0");
    public HSVManipulation()
    {

    }
    [DisplayText(
        DefaultEN: "Hue Shift",
        Thai: "ปรับสี (Hue)",
        Sinhala: "වර්ණ මාරුව (Hue)"
    )]
    static string HueShiftText { get; } = GetDisplayText<HSVManipulation>(nameof(HueShiftText));
    [DisplayText(
        DefaultEN: "Saturation Shift",
        Thai: "ปรับค่าความอิ่มตัวของสี (Saturation)",
        Sinhala: "සංතෘප්ත මාරුව (Saturation)"
    )]
    static string SaturationShiftText { get; } = GetDisplayText<HSVManipulation>(nameof(SaturationShiftText));
    [DisplayText(
        DefaultEN: "Brightness Shift",
        Thai: "ปรับค่าความสว่าง (Brightness)",
        Sinhala: "දීප්තිය මාරුව (Brightness)"
    )]
    static string BrightnessShiftText { get; } = GetDisplayText<HSVManipulation>(nameof(BrightnessShiftText));

    protected override UIElement CreateUI()
    {
        return SimpleUI.GenerateLIVE(
            PageName: Name,
            PageDescription: Description,
            Parameters: new ParameterFromUI[]
            {
                new ImageParameter().Assign(out var ImageParam),
                new DoubleSliderParameter(HueShiftText, -180, 180, 0, DisplayConverter: ConvertAngle).Assign(out var HueShiftParam),
                new DoubleSliderParameter(SaturationShiftText, -100, 100, 0, DisplayConverter: Convert).Assign(out var SaturationShiftParam),
                new DoubleSliderParameter(BrightnessShiftText, -100, 100, 0, DisplayConverter: Convert).Assign(out var BrightnessShiftParam)
            },
            OnExecute: (MatImage) =>
            {
                using var tracker = new ResourcesTracker();
                Mat image = ImageParam.Result.Track(tracker);
                double hue = HueShiftParam.Result;
                double sat = SaturationShiftParam.Result / 100d;
                double bri = BrightnessShiftParam.Result / 100d;
                Mat output = new Mat().Track(tracker);
                var originalchannelcount = image.Channels();

                image.InplaceCvtColor(ColorConversionCodes.BGR2HSV);

                var outhue = (
                        image.ExtractChannel(0).Track(tracker).AsDoubles().Track(tracker) + hue / 2
                    ).Track(tracker).ToMat().Track(tracker);
                Cv2.Subtract(outhue, 180d, outhue, outhue.GreaterThan(180).Track(tracker));
                Cv2.Add(outhue, 180d, outhue, outhue.LessThan(0).Track(tracker));

                var outsat = (
                        image.ExtractChannel(1).Track(tracker).AsDoubles().Track(tracker) + sat * 255
                    ).Track(tracker).ToMat().Track(tracker);
                outsat.SetTo(0, mask: outsat.LessThan(0).Track(tracker));
                outsat.SetTo(255, mask: outsat.GreaterThan(255).Track(tracker));

                var outbright = (
                    image.ExtractChannel(2).Track(tracker).AsDoubles().Track(tracker) + bri * 255
                ).Track(tracker).ToMat().Track(tracker);
                outbright.SetTo(0, mask: outbright.LessThan(0).Track(tracker));
                outbright.SetTo(255, mask: outbright.GreaterThan(255).Track(tracker));

                Cv2.Merge(new Mat[]
                {
                    outhue,
                    outsat,
                    outbright
                }, output);
                output = output.AsBytes().Track(tracker).InplaceCvtColor(ColorConversionCodes.HSV2BGR);
                
                output = ImageParam.PostProcess(output).Track(tracker);

                output.Clone().ImShow(MatImage);
            }
        );
    }
}
class ImageBlending : Feature
{
    enum ChannelName : int
    {
        Red = 2,
        Green = 1,
        Blue = 0,
        Alpha = 3
    }
    [DisplayText(
        DefaultEN: "Image Blending",
        Thai: "ผสมรูปภาพ (Image Blending)",
        Sinhala: "රූප මිශ්‍ර කිරීම"
    )]
    public override string Name { get; } = GetDisplayText<ImageBlending>(nameof(Name));
    public override string DefaultName { get; } = GetDefaultText<ImageBlending>(nameof(Name));

    public override IEnumerable<string> Allias => new string[] { "2 Images", "Blend Image" };
    [DisplayText(
        DefaultEN: "Blend two same-size images together",
        Thai: "ผสมรูปภาพ 2 รูปที่ขนาดเท่ากัน",
        Sinhala: "එකම ප්‍රමාණයේ පින්තූර දෙකක් එකට මිශ්‍ර කරන්න"
    )]
    public override string Description { get; } = GetDisplayText<ImageBlending>(nameof(Description));
    public ImageBlending()
    {

    }
    protected override UIElement CreateUI()
    {
        UIElement? Element = null;
        return Element = SimpleUI.GenerateLIVE(
            PageName: Name,
            PageDescription: Description,
            Parameters: new ParameterFromUI[]
            {
                new ImageParameter(GetDisplayText(new DisplayTextAttribute(
                    DefaultEN: "Image 1",
                    Thai: "รูปภาพที่ 1",
                    Sinhala: "පළමු රූපය"
                )), AlphaRestoreChangable: false, AlphaMode: ImageParameter.AlphaModes.Include).Assign(out var Image1Param),
                new ImageParameter(GetDisplayText(new DisplayTextAttribute(
                    DefaultEN: "Image 2",
                    Thai: "รูปภาพที่ 2",
                    Sinhala: "දෙවන රූපය"
                )), AlphaRestoreChangable: false, AlphaMode: ImageParameter.AlphaModes.Include).Assign(out var Image2Param),
                new CheckboxParameter(GetDisplayText(new DisplayTextAttribute(
                    DefaultEN: "Include ALpha",
                    Thai: "คำนวนความจาง/ความทึบ (Alpha) ด้วย",
                    Sinhala: "පාරාන්ධතාවය (Alpha) ගණනය කරන්න"
                )), true).Edit(x => x.ParameterValueChanged += delegate
                {
                    var val = x.Result;
                    Image1Param.AlphaRestoreParam.Result = val;
                    Image2Param.AlphaRestoreParam.Result = val;
                }),
                new PercentSliderParameter(GetDisplayText(new DisplayTextAttribute(
                    DefaultEN: "Percentage of Image 1",
                    Thai: "% ของ รูปภาพที่ 1",
                    Sinhala: "පළමු රූපයේ ප්‍රතිශතය"
                )), 0.5).Assign(out var Percent1Param)
            },
            OnExecute: async (MatImage) =>
            {
                using var tracker = new ResourcesTracker();
                var image1 = Image1Param.Result.Track(tracker);
                var image2 = Image2Param.Result.Track(tracker);
                var percent1 = Percent1Param.Result;
                Mat output = new();
                if (image1.Width != image2.Width || image1.Height != image2.Height)
                {
                    if (Element != null)
                        await new ContentDialog
                        {
                            Title = SystemLanguage.Error,
                            Content = GetDisplayText(new DisplayTextAttribute(
                                DefaultEN: "Both images must have the same size",
                                Thai: "รูปภาพทั้งสองต้องมีขนาเท่ากัน",
                                Sinhala: "රූප දෙකම එකම ප්‍රමාණයෙන් තිබිය යුතුය"
                            )),
                            XamlRoot = Element.XamlRoot,
                            PrimaryButtonText = SystemLanguage.Okay
                        }.ShowAsync();
                    return;
                }
                if (image1.Channels() != image2.Channels())
                {
                    if (Element != null)
                        await new ContentDialog
                        {
                            Title = SystemLanguage.Error,
                            Content = GetDisplayText(new DisplayTextAttribute(
                                DefaultEN: "Both images must have the same size",
                                Thai: "รูปภาพทั้งสองต้องมีขนาเท่ากัน",
                                Sinhala: "රූප දෙකම එකම ප්‍රමාණයෙන් තිබිය යුතුය"
                            )),
                            XamlRoot = Element.XamlRoot,
                            PrimaryButtonText = SystemLanguage.Okay
                        }.ShowAsync();
                    return;
                }
                image1.InplaceInsertAlpha(Image1Param.AlphaResult);
                image2.InplaceInsertAlpha(Image2Param.AlphaResult);

                Cv2.AddWeighted(image1, percent1, image2, 1 - percent1, 0, output);
                output.ImShow(MatImage);
            }
        );
    }
}
class Border : Feature
{
    [DisplayText(
        DefaultEN: "Border",
        Thai: "ใส่กรอป",
        Sinhala: "මායිම් කිරීම (Border)"
    )]
    public override string Name { get; } = GetDisplayText<Border>(nameof(Name));
    [DisplayText(
        DefaultEN: "Add the border to the image",
        Thai: "ใส่กรอปให้รูปภาพที่ต้องการ",
        Sinhala: "රූපයට මායිමක් එක් කරන්න"
    )]
    public override string Description { get; } = GetDisplayText<Border>(nameof(Description));
    public override IconElement? Icon => new SymbolIcon((Symbol)0xE91b); // Photo
    protected override UIElement CreateUI()
    {
        UIElement? UIElement = null;
        UIElement = SimpleUI.GenerateLIVE(
            PageName: Name,
            PageDescription: Description,
            Parameters: new ParameterFromUI[]
            {
                // ColorChangable: false, AlphaRestoreChangable: false
                new ImageParameter(AlphaMode: ImageParameter.AlphaModes.Include).Assign(out var imageParameter),
                new DoubleNumberBoxParameter("Top Border", 10).Assign(out var T),
                new DoubleNumberBoxParameter("Left Border", 10).Assign(out var L),
                new DoubleNumberBoxParameter("Right Border", 10).Assign(out var R),
                new DoubleNumberBoxParameter("Bottom Border", 10).Assign(out var B),
                new SelectParameter<BorderTypes>(Name: "Blur Border Mode", Enum.GetValues<BorderTypes>().Where(x => !(x is BorderTypes.Transparent or BorderTypes.Reflect101 or BorderTypes.Isolated)).Distinct().ToArray(), 0, x => (x == BorderTypes.Constant ? "Default (Color)" : x.ToString(), null)).Assign(out var Border),
                new ColorPickerParameter("Color", Windows.UI.Color.FromArgb(255, 66, 66, 66)).Assign(out var C).AddDependency(Border, x => x is BorderTypes.Constant)
            },
            OnExecute: async x =>
            {
                using var tracker = new ResourcesTracker();
                var output = await Task.Run(delegate
                {
                    return imageParameter.Result
                    .Track(tracker)
                    .InplaceInsertAlpha(imageParameter.AlphaResult)
                    .CopyMakeBorder(
                        (int)T.Result,
                        (int)L.Result,
                        (int)R.Result,
                        (int)B.Result,
                        Border.Result,
                        value: C.ResultAsScaler
                    );
                });
                output.ImShow(x);
            }
        );

        return UIElement;
    }
}
class PaintBucket : Feature
{
    public override string Name { get; } = nameof(PaintBucket).ToReadableName();
    public override string Description => "Apply Paint Bucket (Flood Fill) to a location";
    public override IconElement? Icon => new SymbolIcon((Symbol)0xE91b); // Photo
    protected override UIElement CreateUI()
    {
        UIElement? UIElement = null;
        
        UIElement = SimpleUI.GenerateLIVE(
            PageName: Name,
            PageDescription: Description,
            Parameters: new ParameterFromUI[]
            {
                new ImageParameter(AlphaMode: ImageParameter.AlphaModes.Include).Assign(out var imageParameter),
                LocationPickerParameter<MatImageDisplayer>.CreateWithImageParameter("Location", imageParameter).Assign(out var location),
                new DoubleSliderParameter("Accepted Difference", 0, 255, 0).Assign(out var Diff),
                new ColorPickerParameter("Color", Windows.UI.Color.FromArgb(255, 66, 66, 66)).Assign(out var C)
            },
            OnExecute: async x =>
            {
                using var tracker = new ResourcesTracker();
                var output = await Task.Run(delegate
                {
                    var image = imageParameter.Result
                    .Track(tracker);
                    var diff = Diff.Result;
                    var scalardiff = new Scalar(diff, diff, diff, diff);
                    var loca = location.Result;
                    //var Mask = new Mat();
                    var color = C.ResultAsScaler;
                    image.Track(tracker);
                    if (color.Val3 == 255)
                    {
                        image.FloodFill(loca, color, out _, diff, diff, FloodFillFlags.Link4);
                        return image.InplaceInsertAlpha(imageParameter.AlphaResult);
                    }
                    var colorMat = new Mat(image.Size(), MatType.CV_8UC4, color).Track(tracker);
                    var colors = colorMat.Split().Track(tracker);
                    var mask = new Mat();
                    image.FloodFill(mask, loca, color, out _, diff, diff, FloodFillFlags.MaskOnly);
                    colors[3].SetTo(0, (1 - mask[1..^1, 1..^1].Track(tracker)).Track(tracker));
                    var oute = imageParameter.PostProcess(AlphaComposite(colors,
                        image.InplaceInsertAlpha(imageParameter.AlphaResult).Track(tracker)
                        .ToBGRA().Track(tracker)
                        .Split().Track(tracker)
                    ));
                    return oute;
                });
                output.ImShow(x);
            }
        );

        return UIElement;
    }
    Mat AlphaComposite(Mat[] foreground, Mat[] background)
    {
        // Reference: https://stackoverflow.com/a/59211216
        var tracker = new ResourcesTracker();
        var newImg = new Mat[4];
        var alphaForeground = foreground[3].AsDoubles().Track(tracker) / 255d;
        var alphaBackground = background[3].AsDoubles().Track(tracker) / 255d;
        var invAlphaForeground = (1d - alphaForeground).Track(tracker);
        var invAlphaBackground = (1d - alphaBackground).Track(tracker);

        for (int i = 0; i < 3; i++)
            newImg[i] = 
                (
                    alphaForeground.Mul(
                        foreground[i].AsDoubles().Track(tracker)
                    ).Track(tracker)
                    + 
                    alphaBackground.Mul(
                        background[i].AsDoubles().Track(tracker)
                    ).Track(tracker)
                    .Mul(invAlphaForeground).Track(tracker)
                ).Track(tracker).ToMat().Track(tracker).Track(tracker);

        newImg[3] = (
            (1 - (invAlphaForeground.Mul(invAlphaBackground)).Track(tracker)).Track(tracker) * 255
        ).Track(tracker).ToMat().Track(tracker);

        var mat = new Mat();
        Cv2.Merge(newImg, mat);
        return mat.AsBytes();
    }
}
class ImageTransformation : Feature
{
    static string ConvertHSV(double i) => i switch
    {
        > 0 => $"+{i:N0} (-{360 - i:N0})",
        < 0 => $"{i:N0} (+{360 + i:N0})",
        0 => "No Change",
        double.NaN => "NaN"
    };
    public override string Name { get; } = nameof(ImageTransformation).ToReadableName();
    public override string Description => "Rotate and/or scale an image from center";
    public override IconElement? Icon => new SymbolIcon(Symbol.Rotate);
    protected override UIElement CreateUI()
    {
        UIElement? UIElement = null;
        UIElement = SimpleUI.GenerateLIVE(
            PageName: Name,
            PageDescription: Description,
            Parameters: new ParameterFromUI[]
            {
                new ImageParameter(AlphaMode: ImageParameter.AlphaModes.Include).Assign(out var imageParameter),
                new DoubleSliderParameter("Rotate", -180, 180, 0, DisplayConverter: ConvertHSV).Assign(out var RotateParam),
                new DoubleNumberBoxParameter("Scale Amount (% Difference = (New / Old) * 100)", 100).Assign(out var ScaleParam)
            },
            OnExecute: x =>
            {
                var tracker = new ResourcesTracker();
                imageParameter.Result
                .InplaceInsertAlpha(imageParameter.AlphaResult).Track(tracker)
                .RotateAndScale(RotateParam.Result, ScaleParam.Result / 100).ImShow(x);
            }
        );

        return UIElement;
    }
}
class PerspectiveTransform : Feature
{
    public override string Name { get; } = nameof(PerspectiveTransform).ToReadableName();
    public override string Description => "Apply Perspective Transform";
    public override IconElement? Icon => new SymbolIcon(Symbol.Rotate);
    protected override UIElement CreateUI()
    {
        UIElement? UIElement = null;
        UIElement = SimpleUI.GenerateLIVE(
            PageName: Name,
            PageDescription: Description,
            Parameters: new ParameterFromUI[]
            {
                new ImageParameter(AlphaMode: ImageParameter.AlphaModes.Include).Assign(out var imageParameter),
                new CheckboxParameter("Adjust Input Corners", true).Assign(out var EnableInputCornerParam),
                new NLocationsPickerParameter<MatImageDisplayer>("Input Corners (Select 4 points)", 4)
                .Edit(locaPicker =>
                {
                    void Proc()
                    {
                        var enable = imageParameter.ResultReady && EnableInputCornerParam.Result;
                        locaPicker.UI.Visibility = enable ? Visibility.Visible : Visibility.Collapsed;
                        if (enable)
                            locaPicker.Mat = imageParameter.Result.InplaceInsertAlpha(imageParameter.AlphaResult);
                        else
                            locaPicker.Mat = null;
                    }
                    Proc();
                    imageParameter.ParameterValueChanged += Proc;
                    EnableInputCornerParam.ParameterValueChanged += Proc;
                })
                .Assign(out var InputCornersParam),
                new CheckboxParameter("Automatically Calculate Output Size", true).Assign(out var AutoOutputSizeParam),
                new DoubleNumberBoxParameter("Output Image Width", 100, 1).Assign(out var WidthParam)
                .AddDependency(AutoOutputSizeParam, x => !x, false),
                new DoubleNumberBoxParameter("Output Image Height", 100, 1).Assign(out var HeightParam)
                .AddDependency(AutoOutputSizeParam, x => !x, false),
                new CheckboxParameter("Adjust Output Corners", false).Assign(out var EnableOutputCornerParam)
                .AddDependency(AutoOutputSizeParam, x => !x, false),
                new NLocationsPickerParameter<MatImageDisplayer>("Output Corners (Select 4 points)", 4)
                .Edit(x =>
                {
                    void Proc()
                    {
                        x.Mat = new Mat(new Size(WidthParam.Result, HeightParam.Result), MatType.CV_8UC3, 0);
                    }
                    WidthParam.ParameterValueChanged += Proc;
                    HeightParam.ParameterValueChanged += Proc;
                })
                .Assign(out var OutputCornerParam)
                .AddDependency(EnableOutputCornerParam, x => x, false),
            },
            OnExecute: x =>
            {
                var tracker = new ResourcesTracker();
                var image = imageParameter.Result.InplaceInsertAlpha(imageParameter.AlphaResult).Track(tracker);
                Point2f[] InputPoints =
                    EnableInputCornerParam.Result ?
                    OrderPoint(InputCornersParam.Result.Select(pt => new Point2f(pt.X, pt.Y)).ToArray()) :
                    new Point2f[]
                    {
                        new Point2f(0, 0),
                        new Point2f(image.Width, 0),
                        new Point2f(image.Width, image.Height),
                        new Point2f(0, image.Height)
                    };
                Size outputSize;
                if (AutoOutputSizeParam.Result)
                    // Reference: https://pyimagesearch.com/2014/08/25/4-point-opencv-getperspective-transform-example/
                    outputSize = new Size(
                        Math.Max(
                            InputPoints[2].DistanceTo(InputPoints[3]),
                            InputPoints[0].DistanceTo(InputPoints[1])
                        ),
                        Math.Max(
                            InputPoints[1].DistanceTo(InputPoints[2]),
                            InputPoints[0].DistanceTo(InputPoints[3])
                        )
                    );
                else outputSize = new Size(WidthParam.Result, HeightParam.Result);
                Point2f[] Middle = new Point2f[]
                {
                    new Point2f(0, 0),
                    new Point2f(outputSize.Width, 0),
                    new Point2f(outputSize.Width, outputSize.Height),
                    new Point2f(0, outputSize.Height)
                };

                var transform = Cv2.GetPerspectiveTransform(
                    InputPoints,
                    Middle
                ).Track(tracker);
                var output = image.WarpPerspective(transform, outputSize);

                if (EnableOutputCornerParam.Result)
                {
                    Point2f[] OutputPoints = OrderPoint(OutputCornerParam.Result.Select(pt => new Point2f(pt.X, pt.Y)).ToArray());

                    transform = Cv2.GetPerspectiveTransform(
                        Middle,
                        OutputPoints
                    ).Track(tracker);
                    output = output.Track(tracker).WarpPerspective(transform, outputSize);
                }
                output.ImShow(x);
            }
        );

        return UIElement;
    }
    static Point2f[] OrderPoint(IList<Point2f> Points)
    {
        // Reference: https://pyimagesearch.com/2016/03/21/ordering-coordinates-clockwise-with-python-and-opencv/
        Point2f[] pt = new Point2f[4];

        var XSorted = Points.OrderBy(pt => pt.X).ToArray();

        var SortedLeftMost = XSorted[..2].OrderBy(pt => pt.Y).ToArray();
        
        var TopLeft = pt[0] = SortedLeftMost[0];
        pt[3] = SortedLeftMost[1];

        var SortedRightMost = XSorted[2..].OrderBy(pt => TopLeft.DistanceTo(pt)).ToArray();
        pt[1] = SortedRightMost[0];
        pt[2] = SortedRightMost[1];

        return pt;
    }
}
class Crop : Feature
{
    public override string Name { get; } = nameof(Crop);
    public override string Description => "Crops the image";
    public override IconElement? Icon => new SymbolIcon(Symbol.Crop);
    protected override UIElement CreateUI()
    {
        UIElement? UIElement = null;
        UIElement = SimpleUI.GenerateLIVE(
            PageName: Name,
            PageDescription: Description,
            Parameters: new ParameterFromUI[]
            {
                // ColorChangable: false, AlphaRestoreChangable: false
                new ImageParameter(AlphaMode: ImageParameter.AlphaModes.Include).Assign(out var imageParameter),
                RectLocationPickerParameter<MatImageDisplayer>.CreateWithImageParameter("Crop Region (Drag to select)", imageParameter).Assign(out var rectParameter)
            },
            OnExecute: async x =>
            {
                using var tracker = new ResourcesTracker();
                var output = await Task.Run(delegate
                {
                    var (top, bottom, left, right) = rectParameter.Result;

                    return imageParameter.Result
                    .Track(tracker)
                    .InplaceInsertAlpha(imageParameter.AlphaResult)
                    .SubMat(
                        top..bottom,
                        left..right
                    );
                });
                output.ImShow(x);
            }
        );

        return UIElement;
    }
}
static partial class Extension
{
    //public static float DistanceTo(Point2f pt1, Point2f pt2)
    //    => MathF.Sqrt(MathF.Pow(pt1.X + pt2.X, 2) + MathF.Pow(pt1.Y + pt2.Y, 2));
    public static (int MinIndex, int MaxIndex, T MinValue, T MaxValue) MinMax<T>(this IEnumerable<T> Value) where T : IComparable<T>
    {
        var enumerator = Value.Enumerate().GetEnumerator();
        
        var (i, x) = enumerator.Current;
        T Max = x;
        int MaxIdx = i;
        T Min = x;
        int MinIdx = i;

        while (enumerator.MoveNext())
        {
            (i, x) = enumerator.Current;
            if (x.CompareTo(Max) is >0)
            {
                Max = x;
                MaxIdx = i;
            }
            if (x.CompareTo(Min) is <0)
            {
                Max = x;
                MinIdx = i;
            }
        }
        return (MinIdx, MaxIdx, Min, Max);
    }
}