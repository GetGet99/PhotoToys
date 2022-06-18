using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OpenCvSharp;
using PhotoToys.Parameters;
using System;
using System.Linq;

namespace PhotoToys.Features;

class BasicManipulation : Category
{
    public override string Name { get; } = nameof(AdvancedManipulation).ToReadableName();
    public override string Description { get; } = "Apply Filter to enhance or change the look of the photo!";
    public override Feature[] Features { get; } = new Feature[]
    {
        new HSVSlider()
    };
}
class HSVSlider : Feature
{
    enum ChannelName : int
    {
        Red = 2,
        Green = 1,
        Blue = 0,
        Alpha = 3
    }
    public override string Name { get; } = "HSV Slider";
    public override string Description { get; } = "Change Hue, Saturation, and Brightness of an image";
    public override UIElement UIContent { get; }
    public HSVSlider()
    {
        UIContent = SimpleUI.Generate(
            PageName: Name,
            PageDescription: Description,
            Parameters: new IParameterFromUI[]
            {
                new ImageParameter().Assign(out var ImageParam),
                new SelectParameter<ChannelName>("Channel to extract", Enum.GetValues<ChannelName>()).Assign(out var ChannelParam),
                new CheckboxParameter("Pad other channel\n(Output the same image type)", false).Assign(out var PadParam)
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
                    ;
                }

                if (UIContent != null) await output.ImShow("Result", XamlRoot: UIContent.XamlRoot);
            }
        );
    }
}