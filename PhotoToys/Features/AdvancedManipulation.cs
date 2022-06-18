using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OpenCvSharp;
using PhotoToys.Parameters;
using System;
using System.Linq;

namespace PhotoToys.Features;

class AdvancedManipulation : Category
{
    public override string Name { get; } = nameof(AdvancedManipulation).ToReadableName();
    public override string Description { get; } = "Apply advanced image manipulation techniques!";
    public override Feature[] Features { get; } = new Feature[]
    {
        new ExtractChannel(),
        new ImageBlending()
    };
}
class ExtractChannel : Feature
{
    enum ChannelName : int
    {
        Red = 2,
        Green = 1,
        Blue = 0,
        Alpha = 3
    }
    public override string Name { get; } = nameof(ExtractChannel).ToReadableName();
    public override string Description { get; } = "Extract Red, Green, Blue, or Opacity/Alpha Channel from the image as Grayscale Image";
    public override UIElement UIContent { get; }
    public ExtractChannel()
    {
        UIContent = SimpleUI.Generate(
            PageName: Name,
            PageDescription: Description,
            Parameters: new IParameterFromUI[]
            {
                new ImageParameter().Assign(out var ImageParam),
                new SelectParameter<ChannelName>("Channel to extract", Enum.GetValues<ChannelName>()).Assign(out var ChannelParam),
                new CheckboxParameter("Pad other channel\n(Output the same image type)", Default: false).Assign(out var PadParam)
            },
            OnExecute: async delegate
            {
                var original = ImageParam.Result;
                var channel = ChannelParam.Result;
                var pad = PadParam.Result;
                var channelint = (int)channel;
                Mat output;
                var originalchannelcount = original.Channels();
                if (channelint + 1 > originalchannelcount)
                {
                    if (UIContent != null)
                        await new ContentDialog
                        {
                            Title = "Error",
                            Content = $"This image does not have {channel} channel",
                            XamlRoot = UIContent.XamlRoot,
                            PrimaryButtonText = "Okay"
                        }.ShowAsync();
                    return;
                }
                output = original.ExtractChannel(channelint);

                if (pad)
                {
                    using var t = new ResourcesTracker();
                    Mat[] mats = Enumerable.Repeat(false, originalchannelcount).Select(_ => t.T(t.T(output).EmptyClone())).ToArray();
                    mats[channelint] = output;
                    if (originalchannelcount == 4)
                        mats[3] = t.T(t.T(output.EmptyClone()) + 255);
                    output = new Mat();
                    Cv2.Merge(mats, output);
                }

                if (UIContent != null) await output.ImShow("Result", XamlRoot: UIContent.XamlRoot);
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
    public override string Description { get; } = "Blend two images together";
    public override UIElement UIContent { get; }
    public ImageBlending()
    {
        UIContent = SimpleUI.Generate(
            PageName: Name,
            PageDescription: Description,
            Parameters: new IParameterFromUI[]
            {
                new ImageParameter("Image 1").Assign(out var Image1Param),
                new ImageParameter("Image 2").Assign(out var Image2Param),
                new PercentSliderParameter("Percentage of Image 1", 0.5).Assign(out var Percent1Param)
            },
            OnExecute: async delegate
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
                if (UIContent != null) await output.ImShow("Result", XamlRoot: UIContent.XamlRoot);
            }
        );
    }
}