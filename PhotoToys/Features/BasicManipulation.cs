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

namespace PhotoToys.Features;

class BasicManipulation : Category
{
    public override string Name { get; } = nameof(BasicManipulation).ToReadableName();
    public override string Description { get; } = "Apply basic image manipulation techniques!";
    public override IconElement? Icon { get; } = new SymbolIcon(Symbol.Edit);
    public override Feature[] Features { get; } = new Feature[]
    {
        new HSVManipulation(),
        new ImageBlending(),
        new Border(),
        new PaintBucket()
    };
}
class HSVManipulation : Feature
{
    enum ChannelName : int
    {
        Red = 2,
        Green = 1,
        Blue = 0,
        Alpha = 3
    }
    public override string Name { get; } = $"HSV {nameof(HSVManipulation)[3..].ToReadableName()}";
    public override IEnumerable<string> Allias => new string[] { "HSV", "Hue", "Saturation", "Value", "Brightness", "Color", "Change Color" };
    public override string Description { get; } = "Change Hue, Saturation, and Brightness of an image";
    static string ConvertHSV(double i) => i switch
    {
        > 0 => $"+{i:N0} (-{360 - i:N0})",
        < 0 => $"{i:N0} (+{360 + i:N0})",
        0 => "No Change",
        double.NaN => "NaN"
    };
    static string Convert(double i) => i > 0 ? $"+{i:N0}" : i.ToString("N0");
    public HSVManipulation()
    {

    }
    protected override UIElement CreateUI()
    {
        return SimpleUI.GenerateLIVE(
            PageName: Name,
            PageDescription: Description,
            Parameters: new ParameterFromUI[]
            {
                new ImageParameter().Assign(out var ImageParam),
                new DoubleSliderParameter("Hue Shift", -180, 180, 0, DisplayConverter: ConvertHSV).Assign(out var HueShiftParam),
                new DoubleSliderParameter("Saturation Shift", -100, 100, 0, DisplayConverter: Convert).Assign(out var SaturationShiftParam),
                new DoubleSliderParameter("Brightness Shift", -100, 100, 0, DisplayConverter: Convert).Assign(out var BrightnessShiftParam)
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
    public override string Name { get; } = nameof(ImageBlending).ToReadableName();
    public override IEnumerable<string> Allias => new string[] { "2 Images", "Blend Image" };
    public override string Description { get; } = "Blend two images together";
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
                new ImageParameter("Image 1", AlphaRestoreChangable: false, AlphaMode: ImageParameter.AlphaModes.Include).Assign(out var Image1Param),
                new ImageParameter("Image 2", AlphaRestoreChangable: false, AlphaMode: ImageParameter.AlphaModes.Include).Assign(out var Image2Param),
                new CheckboxParameter("Include Alpha", true).Edit(x => x.ParameterValueChanged += delegate
                {
                    var val = x.Result;
                    Image1Param.AlphaRestoreParam.Result = val;
                    Image2Param.AlphaRestoreParam.Result = val;
                }),
                new PercentSliderParameter("Percentage of Image 1", 0.5).Assign(out var Percent1Param)
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
                            Title = "Error",
                            Content = "Both images must have the same size",
                            XamlRoot = Element.XamlRoot,
                            PrimaryButtonText = "Okay"
                        }.ShowAsync();
                    return;
                }
                if (image1.Channels() != image2.Channels())
                {
                    if (Element != null)
                        await new ContentDialog
                        {
                            Title = "Error",
                            Content = "Both images must have the same size",
                            XamlRoot = Element.XamlRoot,
                            PrimaryButtonText = "Okay"
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
    public override string Name { get; } = nameof(Border);
    public override string Description => "Add the border to the image";
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
                LocationPickerParameter<MatImageDefault>.CreateWithImageParameter("Location", imageParameter).Assign(out var location),
                new DoubleSliderParameter("Accepted Difference", 0, 255, 0).Assign(out var Diff),
                new ColorPickerParameter("Color", Windows.UI.Color.FromArgb(255, 66, 66, 66)).Assign(out var C)
            },
            OnExecute: async x =>
            {
                using var tracker = new ResourcesTracker();
                var output = await Task.Run(async delegate
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