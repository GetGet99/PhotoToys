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
    public override IconElement? Icon { get; } = new SymbolIcon((Symbol)0xE81E); // MapLayers
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
        Red,
        Green,
        Blue,
        Alpha = 3
    }
    enum OutputMode
    {
        Grayscale = 0,
        PadColor = 1,
        PadColorCopyAlpha = 2
    }
    public override string Name { get; } = nameof(ExtractChannel).ToReadableName();
    public override string Description { get; } = "Extract Red, Green, Blue, or Opacity/Alpha Channel from the image as Grayscale Image";
    public override IconElement? Icon { get; } = new SymbolIcon((Symbol)0xEDE1); // Export
    public ExtractChannel()
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
                new ImageParameter(ColorChangable: false, AlphaRestoreChangable: false, AlphaMode: ImageParameter.AlphaModes.Include).Assign(out var ImageParam),
                new SelectParameter<ChannelName>("Channel to extract", Enum.GetValues<ChannelName>()).Assign(out var ChannelParam),
                new SelectParameter<OutputMode>("Output Mode", Enum.GetValues<OutputMode>(), 0, x => x switch {
                    OutputMode.PadColor => ("Color (Pad other channel)", null),
                    OutputMode.PadColorCopyAlpha => ("Color (Pad other channel) and copy old alpha", null),
                    _ => (x.ToString(), null)
                }).Assign(out var PadParam)
            },
            OnExecute: (MatImage) =>
            {
                using var tracker = new ResourcesTracker();
                var original = ImageParam.Result.Track(tracker);
                ChannelName channel = ChannelParam.Result;
                bool pad = PadParam.Result is OutputMode.PadColor or OutputMode.PadColorCopyAlpha;

                Mat output = (channel switch
                {
                    ChannelName.Blue => original.ExtractChannel(0),
                    ChannelName.Green => original.ExtractChannel(1),
                    ChannelName.Red => original.ExtractChannel(2),
                    ChannelName.Alpha => ImageParam.AlphaResult ?? throw new InvalidOperationException(),
                    _ => throw new InvalidOperationException()
                }).Track(tracker);

                if (pad)
                {
                    Mat[] mats = Enumerable.Repeat(false, 4).Select(_ => output.EmptyClone().Track(tracker)).ToArray();
                    mats[channel switch
                    {
                        ChannelName.Blue => 0,
                        ChannelName.Green => 1,
                        ChannelName.Red => 2,
                        ChannelName.Alpha => 3,
                        _ => throw new InvalidOperationException()
                    }] = output;
                    mats[3] = (output.EmptyClone().Track(tracker) + 255).Track(tracker);
                    if (PadParam.Result is OutputMode.PadColorCopyAlpha && channel is not ChannelName.Alpha)
                        mats[3] = (output.EmptyClone().Track(tracker) + 255).Track(tracker);
                    else
                        mats[3] = (mats[3] + 255).Track(tracker);
                    output = new Mat().Track(tracker);
                    Cv2.Merge(mats, output);
                }

                output.Clone().ImShow(MatImage);
            }
        );
    }
}
class CombineChannel : Feature
{
    //enum ChannelName : int
    //{
    //    Default = 0,
    //    Red = 3,
    //    Green = 2,
    //    Blue = 1,
    //    Alpha = 4
    //}
    public override string Name { get; } = nameof(CombineChannel).ToReadableName();
    public override string Description { get; } = "Combine Images representing Red, Green, Blue, or Opacity/Alpha Channel into one colored image";
    public override IconElement? Icon { get; } = new SymbolIcon((Symbol)0xF0E2); // Grid View
    public CombineChannel()
    {
        
    }
    protected override UIElement CreateUI()
    {
        return SimpleUI.GenerateLIVE(
            PageName: Name,
            PageDescription: Description,
            Parameters: new ParameterFromUI[]
            {
                new ImageParameter(Name: "Image for new Image's Red Channel", ColorMode: false, AlphaRestore: false, AlphaRestoreChangable: false).Assign(out var ImageR),
                //new SelectParameter<ChannelName>(Name: "Channel to extract", Enum.GetValues<ChannelName>(), 0,
                //    x => x == ChannelName.Default ? "Default (Convert Color to Grayscale)" : x.ToString()
                //).Assign(out var ImageRChannel),
                new ImageParameter(Name: "Image for new Image's Green Channel", ColorMode: false, AlphaRestore: false, AlphaRestoreChangable: false).Assign(out var ImageG),
                //new SelectParameter<ChannelName>(Name: "Channel to extract", Enum.GetValues<ChannelName>(), 0,
                //    x => x == ChannelName.Default ? "Default (Convert Color to Grayscale)" : x.ToString()
                //).Assign(out var ImageGChannel),
                new ImageParameter(Name: "Image for new Image's Blue Channel", ColorMode: false, AlphaRestore: false, AlphaRestoreChangable: false).Assign(out var ImageB),
                //new SelectParameter<ChannelName>(Name: "Channel to extract", Enum.GetValues<ChannelName>(), 0,
                //    x => x == ChannelName.Default ? "Default (Convert Color to Grayscale)" : x.ToString()
                //).Assign(out var ImageBChannel),
                new ImageParameter(Name: "Image for new Image's Alpha Channel", ColorMode: false, AlphaRestore: false, AlphaRestoreChangable: false).Assign(out var ImageA),
                //new SelectParameter<ChannelName>(Name: "Channel to extract", Enum.GetValues<ChannelName>(), 0,
                //    x => x == ChannelName.Default ? "Default (Convert Color to Grayscale)" : x.ToString()
                //).Assign(out var ImageAChannel)
            },
            OnExecute: (MatImage) =>
            {
                using var tracker = new ResourcesTracker();
                
                Mat output = new();
                Cv2.Merge(new Mat[]
                {
                    ImageB.Result.Track(tracker),
                    ImageG.Result.Track(tracker),
                    ImageR.Result.Track(tracker),
                    ImageA.Result.Track(tracker)
                }, output);

                output.ImShow(MatImage);
            }
        );
    }
}
class SwapChannel : Feature
{
    enum ChannelName : int
    {
        Red,
        Green,
        Blue,
        Alpha
    }
    public override string Name { get; } = nameof(SwapChannel).ToReadableName();
    public override string Description { get; } = "Switching Red, Green, Blue, or Opacity/Alpha Channel in the same image";
    public override IconElement? Icon { get; } = new SymbolIcon(Symbol.Switch);
    public SwapChannel()
    {
        
    }
    protected override UIElement CreateUI()
    {
        return SimpleUI.GenerateLIVE(
            PageName: Name,
            PageDescription: Description,
            Parameters: new ParameterFromUI[]
            {
                new ImageParameter(ColorChangable: false, AlphaRestoreChangable: false, AlphaMode: ImageParameter.AlphaModes.Include).Assign(out var Image),
                new SelectParameter<ChannelName>(Name: "Channel Red", Enum.GetValues<ChannelName>(), 2).Assign(out var ImageRChannel),
                new SelectParameter<ChannelName>(Name: "Channel Green", Enum.GetValues<ChannelName>(), 1).Assign(out var ImageGChannel),
                new SelectParameter<ChannelName>(Name: "Channel Blue", Enum.GetValues<ChannelName>(), 0).Assign(out var ImageBChannel),
                new SelectParameter<ChannelName>(Name: "Channel Alpha", Enum.GetValues<ChannelName>(), 3).Assign(out var ImageAChannel)
            },
            OnExecute: (MatImage) =>
            {
                using var tracker = new ResourcesTracker();

                static Mat GetChannel(ChannelName Channel, ImageParameter p)
                {
                    return Channel switch
                    {
                        ChannelName.Red => p.Result.ExtractChannel(0),
                        ChannelName.Blue => p.Result.ExtractChannel(1),
                        ChannelName.Green => p.Result.ExtractChannel(2),
                        ChannelName.Alpha => p.AlphaResult ?? throw new InvalidOperationException(),
                        _ => throw new InvalidOperationException()
                    };
                }
                Mat output = new();
                Cv2.Merge(new Mat[]
                {
                    GetChannel(ImageBChannel.Result, Image).Track(tracker),
                    GetChannel(ImageGChannel.Result, Image).Track(tracker),
                    GetChannel(ImageRChannel.Result, Image).Track(tracker),
                    GetChannel(ImageAChannel.Result, Image).Track(tracker)
                }, output);

                output.ImShow(MatImage);
            }
        );
    }
}
class ReplaceAlphaChannel : Feature
{
    public override string Name { get; } = nameof(ReplaceAlphaChannel).ToReadableName();
    public override string Description { get; } = "Replace Opacity/Alpha Channel of one image with another image's channel";
    public override IconElement? Icon { get; } = new SymbolIcon(Symbol.Sync);
    public ReplaceAlphaChannel()
    {
        
    }
    protected override UIElement CreateUI()
    {
        return SimpleUI.GenerateLIVE(
            PageName: Name,
            PageDescription: Description,
            Parameters: new ParameterFromUI[]
            {
                new ImageParameter(Name: "Original Image", AlphaRestore: false, AlphaRestoreChangable: false).Assign(out var Image),
                new ImageParameter(Name: "Image for new Image's Alpha Channel", ColorMode: false, AlphaRestore: false, AlphaRestoreChangable: false).Assign(out var ImageA),
            },
            OnExecute: (MatImage) =>
            {
                using var tracker = new ResourcesTracker();
                var original = Image.Result.Track(tracker);
                var output = original.InsertAlpha(ImageA.Result.Track(tracker));

                output.ImShow(MatImage);
            }
        );
    }
}
