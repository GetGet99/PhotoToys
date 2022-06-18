using Microsoft.UI.Xaml;
using OpenCvSharp;
using PhotoToys.Parameters;
using System;
using System.Diagnostics;

namespace PhotoToys.Features;

class BasicManipulation : Category
{
    public override string Name { get; } = nameof(BasicManipulation).ToReadableName();
    public override string Description { get; } = "Apply basic image manipulation techniques!";
    public override Feature[] Features { get; } = new Feature[]
    {
        new HSVManipulation()
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
    public override string Description { get; } = "Change Hue, Saturation, and Brightness of an image";
    public override UIElement UIContent { get; }
    public HSVManipulation()
    {
        UIContent = SimpleUI.Generate(
            PageName: Name,
            PageDescription: Description,
            Parameters: new IParameterFromUI[]
            {
                new ImageParameter().Assign(out var ImageParam),
                new IntSliderParameter("Hue Shift", -180, 180, 0).Assign(out var HueShiftParam),
                new IntSliderParameter("Saturation Shift", -100, 100, 0).Assign(out var SaturationShiftParam),
                new IntSliderParameter("Brightness Shift", -100, 100, 0).Assign(out var BrightnessShiftParam)
            },
            OnExecute: async delegate
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

                if (UIContent != null) await output.ImShow("Result", XamlRoot: UIContent.XamlRoot);
            }
        );
    }
}