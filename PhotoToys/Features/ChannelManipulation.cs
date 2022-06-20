using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OpenCvSharp;
using PhotoToys.Parameters;
using System;
using System.Linq;

namespace PhotoToys.Features;

class ChannelManipulation : Category
{
    public override string Name { get; } = nameof(ChannelManipulation).ToReadableName();
    public override string Description { get; } = "Manipulate image channels!";
    public override Feature[] Features { get; } = new Feature[]
    {
        new ExtractChannel(),
        new CombineChannel(),
        new SwapChannel(),
        new ReplaceAlphaChannel()
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
    enum OutputMode
    {
        Grayscale = 0,
        PadColor = 1
    }
    public override string Name { get; } = nameof(ExtractChannel).ToReadableName();
    public override string Description { get; } = "Extract Red, Green, Blue, or Opacity/Alpha Channel from the image as Grayscale Image";
    public override UIElement UIContent { get; }
    public ExtractChannel()
    {
        UIContent = SimpleUI.GenerateLIVE(
            PageName: Name,
            PageDescription: Description,
            Parameters: new IParameterFromUI[]
            {
                new ImageParameter().Assign(out var ImageParam),
                new SelectParameter<ChannelName>("Channel to extract", Enum.GetValues<ChannelName>()).Assign(out var ChannelParam),
                new SelectParameter<OutputMode>("Output Mode", Enum.GetValues<OutputMode>(), 0, x => x == OutputMode.PadColor ? "Color (Pad other channel)" : x.ToString()).Assign(out var PadParam)
            },
            OnExecute: async (MatImage) =>
            {
                var original = ImageParam.Result;
                var channel = ChannelParam.Result;
                var pad = PadParam.Result == OutputMode.PadColor;
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

                if (UIContent != null) output.ImShow(MatImage);
            }
        );
    }
}
class CombineChannel : Feature
{
    enum ChannelName : int
    {
        Default = 0,
        Red = 3,
        Green = 2,
        Blue = 1,
        Alpha = 4
    }
    public override string Name { get; } = nameof(CombineChannel).ToReadableName();
    public override string Description { get; } = "Combine Images representing Red, Green, Blue, or Opacity/Alpha Channel into one colored image";
    public override UIElement UIContent { get; }
    public CombineChannel()
    {
        UIContent = SimpleUI.GenerateLIVE(
            PageName: Name,
            PageDescription: Description,
            Parameters: new IParameterFromUI[]
            {
                new ImageParameter(Name: "Image for new Image's Red Channel").Assign(out var ImageR),
                new SelectParameter<ChannelName>(Name: "Channel to extract", Enum.GetValues<ChannelName>(), 0,
                    x => x == ChannelName.Default ? "Default (Convert Color to Grayscale)" : x.ToString()
                ).Assign(out var ImageRChannel),
                new ImageParameter(Name: "Image for new Image's Green Channel").Assign(out var ImageG),
                new SelectParameter<ChannelName>(Name: "Channel to extract", Enum.GetValues<ChannelName>(), 0,
                    x => x == ChannelName.Default ? "Default (Convert Color to Grayscale)" : x.ToString()
                ).Assign(out var ImageGChannel),
                new ImageParameter(Name: "Image for new Image's Blue Channel").Assign(out var ImageB),
                new SelectParameter<ChannelName>(Name: "Channel to extract", Enum.GetValues<ChannelName>(), 0,
                    x => x == ChannelName.Default ? "Default (Convert Color to Grayscale)" : x.ToString()
                ).Assign(out var ImageBChannel),
                new ImageParameter(Name: "Image for new Image's Alpha Channel").Assign(out var ImageA),
                new SelectParameter<ChannelName>(Name: "Channel to extract", Enum.GetValues<ChannelName>(), 0,
                    x => x == ChannelName.Default ? "Default (Convert Color to Grayscale)" : x.ToString()
                ).Assign(out var ImageAChannel)
            },
            OnExecute: (MatImage) =>
            {
                using var t = new ResourcesTracker();
                static Mat GetChannel(ChannelName Channel, Mat m)
                {
                    if (Channel == ChannelName.Default)
                        return m.ToGray();
                    else
                        return m.ExtractChannel((int)Channel - 1);
                }
                Mat output = new();
                Cv2.Merge(new Mat[]
                {
                    t.T(GetChannel(ImageBChannel.Result, ImageB.Result)),
                    t.T(GetChannel(ImageGChannel.Result, ImageG.Result)),
                    t.T(GetChannel(ImageRChannel.Result, ImageR.Result)),
                    t.T(GetChannel(ImageAChannel.Result, ImageA.Result))
                }, output);

                if (UIContent != null) output.ImShow(MatImage);
            }
        );
    }
}
class SwapChannel : Feature
{
    enum ChannelName : int
    {
        Red = 2,
        Green = 1,
        Blue = 0,
        Alpha = 3
    }
    public override string Name { get; } = nameof(SwapChannel).ToReadableName();
    public override string Description { get; } = "Switching Red, Green, Blue, or Opacity/Alpha Channel in the same image";
    public override UIElement UIContent { get; }
    public SwapChannel()
    {
        UIContent = SimpleUI.GenerateLIVE(
            PageName: Name,
            PageDescription: Description,
            Parameters: new IParameterFromUI[]
            {
                new ImageParameter(Name: "Image for new Image's Red Channel").Assign(out var Image),
                new SelectParameter<ChannelName>(Name: "Channel Red", Enum.GetValues<ChannelName>(), 2).Assign(out var ImageRChannel),
                new SelectParameter<ChannelName>(Name: "Channel Green", Enum.GetValues<ChannelName>(), 1).Assign(out var ImageGChannel),
                new SelectParameter<ChannelName>(Name: "Channel Blue", Enum.GetValues<ChannelName>(), 0).Assign(out var ImageBChannel),
                new SelectParameter<ChannelName>(Name: "Channel Alpha", Enum.GetValues<ChannelName>(), 3).Assign(out var ImageAChannel)
            },
            OnExecute: (MatImage) =>
            {
                using var t = new ResourcesTracker();

                static Mat GetChannel(ChannelName Channel, Mat m)
                {
                    return m.ExtractChannel((int)Channel);
                }
                Mat output = new();
                Mat original = Image.Result;
                Cv2.Merge(new Mat[]
                {
                    t.T(GetChannel(ImageBChannel.Result, original)),
                    t.T(GetChannel(ImageGChannel.Result, original)),
                    t.T(GetChannel(ImageRChannel.Result, original)),
                    t.T(GetChannel(ImageAChannel.Result, original))
                }, output);

                if (UIContent != null) output.ImShow(MatImage);
            }
        );
    }
}
class ReplaceAlphaChannel : Feature
{
    enum ChannelName : int
    {
        Default = 0,
        Red = 3,
        Green = 2,
        Blue = 1,
        Alpha = 4
    }
    public override string Name { get; } = nameof(ReplaceAlphaChannel).ToReadableName();
    public override string Description { get; } = "Replace Opacity/Alpha Channel of one image with another image's channel";
    public override UIElement UIContent { get; }
    public ReplaceAlphaChannel()
    {
        UIContent = SimpleUI.GenerateLIVE(
            PageName: Name,
            PageDescription: Description,
            Parameters: new IParameterFromUI[]
            {
                new ImageParameter(Name: "Original Image").Assign(out var Image),
                new ImageParameter(Name: "Image for new Image's Alpha Channel").Assign(out var ImageA),
                new SelectParameter<ChannelName>(Name: "Channel to extract for new Image's Alpha Channel", Enum.GetValues<ChannelName>(), 0,
                    x => x == ChannelName.Default ? "Default (Convert Color to Grayscale)" : x.ToString()
                ).Assign(out var ImageAChannel)
            },
            OnExecute: (MatImage) =>
            {
                using var t = new ResourcesTracker();
                static Mat GetChannel(ChannelName Channel, Mat m)
                {
                    if (Channel == ChannelName.Default)
                        return m.ToGray();
                    else
                        return m.ExtractChannel((int)Channel - 1);
                }
                var original = Image.Result;
                var output = original.ToBGR(out var a).InsertAlpha(GetChannel(ImageAChannel.Result, ImageA.Result));
                a?.Dispose();

                if (UIContent != null) output.ImShow(MatImage);
            }
        );
    }
}
