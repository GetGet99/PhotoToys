using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OpenCvSharp;
using PhotoToys.Parameters;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace PhotoToys.Features;

class BasicManipulation : Category
{
    public override string Name { get; } = nameof(BasicManipulation).ToReadableName();
    public override string Description { get; } = "Apply basic image manipulation techniques!";
    public override Feature[] Features { get; } = new Feature[]
    {
        new HSVManipulation(),
        new ImageBlending()
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
    public override UIElement UIContent { get; }
    static string Convert(double i) => i > 0 ? $"+{i:N0}" : i.ToString("N0");
    public HSVManipulation()
    {
        UIContent = SimpleUI.GenerateLIVE(
            PageName: Name,
            PageDescription: Description,
            Parameters: new IParameterFromUI[]
            {
                new ImageParameter().Assign(out var ImageParam),
                new DoubleSliderParameter("Hue Shift", -180, 180, 0, DisplayConverter: Convert).Assign(out var HueShiftParam),
                new DoubleSliderParameter("Saturation Shift", -100, 100, 0, DisplayConverter: Convert).Assign(out var SaturationShiftParam),
                new DoubleSliderParameter("Brightness Shift", -100, 100, 0, DisplayConverter: Convert).Assign(out var BrightnessShiftParam)
            },
            OnExecute: (MatImage) =>
            {
                using var t = new ResourcesTracker();
                var image = ImageParam.Result;
                var hue = (double)HueShiftParam.Result;
                var sat = SaturationShiftParam.Result / 100d;
                var bri = BrightnessShiftParam.Result / 100d;
                Mat output = new Mat();
                var originalchannelcount = image.Channels();
                Mat? originalA = null;
                switch (originalchannelcount)
                {
                    case 1:
                        image = t.T(image.CvtColor(ColorConversionCodes.GRAY2BGR));
                        goto case 3;
                    case 4:
                        originalA = t.T(image.ExtractChannel(3));
                        image = t.T(image.CvtColor(ColorConversionCodes.BGRA2BGR));
                        goto case 3;
                    case 3:
                        image = t.T(image.CvtColor(ColorConversionCodes.BGR2HSV));
                        break;
                }
                var outhue = t.T(t.T(t.T(image.ExtractChannel(0)).AsDoubles()) + hue / 2).ToMat();
                Cv2.Subtract(outhue, 180d, outhue, t.T(outhue.GreaterThan(180)));
                Cv2.Add(outhue, 180d, outhue, t.T(outhue.LessThan(0)));

                var outsat = t.T(t.T(t.T(image.ExtractChannel(1)).AsDoubles()) + sat * 255).ToMat();
                outsat.SetTo(0, mask: t.T(outhue.LessThan(0)));
                outsat.SetTo(255, mask: t.T(outhue.GreaterThan(255)));
                var outbright = t.T(t.T(t.T(image.ExtractChannel(2)).AsDoubles()) + bri * 2552).ToMat();
                outbright.SetTo(0, mask: t.T(outhue.LessThan(0)));
                outbright.SetTo(255, mask: t.T(outhue.GreaterThan(255)));


                Cv2.Merge(new Mat[]
                {
                    outhue,
                    outsat,
                    outbright
                }, output);
                output = t.T(output).AsBytes();
                output = t.T(output).CvtColor(ColorConversionCodes.HSV2BGR);
                if (originalchannelcount == 4)
                {
                    Debug.Assert(originalA != null);
                    output = t.T(output).InsertAlpha(originalA);
                }

                if (UIContent != null) output.ImShow(MatImage);
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
    public override UIElement UIContent { get; }
    public ImageBlending()
    {
        UIContent = SimpleUI.GenerateLIVE(
            PageName: Name,
            PageDescription: Description,
            Parameters: new IParameterFromUI[]
            {
                new ImageParameter("Image 1").Assign(out var Image1Param),
                new ImageParameter("Image 2").Assign(out var Image2Param),
                new PercentSliderParameter("Percentage of Image 1", 0.5).Assign(out var Percent1Param)
            },
            OnExecute: async (MatImage) =>
            {
                using var t = new ResourcesTracker();
                var image1 = Image1Param.Result;
                var image2 = Image2Param.Result;
                var percent1 = Percent1Param.Result;
                var channel1 = image1.Channels();
                var channel2 = image2.Channels();
                Mat output = new();
                if (image1.Width != image2.Width || image1.Height != image2.Height)
                {
                    if (UIContent != null)
                        await new ContentDialog
                        {
                            Title = "Error",
                            Content = "Both images must have the same size",
                            XamlRoot = UIContent.XamlRoot,
                            PrimaryButtonText = "Okay"
                        }.ShowAsync();
                    return;
                }
                if (channel1 != channel2)
                {
                    switch (Math.Max(channel1, channel2))
                    {
                        case 3:
                            if (channel1 == 1) image1 = t.T(image1.CvtColor(ColorConversionCodes.GRAY2BGR));
                            if (channel2 == 1) image2 = t.T(image2.CvtColor(ColorConversionCodes.GRAY2BGR));
                            break;
                        case 4:
                            if (channel1 == 1) image1 = t.T(image1.CvtColor(ColorConversionCodes.GRAY2BGRA));
                            else if (channel1 == 3) image1 = t.T(image1.CvtColor(ColorConversionCodes.BGR2BGRA));
                            if (channel2 == 1) image2 = t.T(image2.CvtColor(ColorConversionCodes.GRAY2BGRA));
                            else if (channel2 == 3) image2 = t.T(image2.CvtColor(ColorConversionCodes.BGR2BGRA));
                            break;
                    }
                }

                Cv2.AddWeighted(image1, percent1, image2, 1 - percent1, 0, output);
                if (UIContent != null) output.ImShow(MatImage);
            }
        );
    }
}
