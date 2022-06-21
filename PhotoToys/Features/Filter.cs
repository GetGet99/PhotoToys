using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OpenCvSharp;
using PhotoToys.Parameters;
using System;
using System.Linq;
namespace PhotoToys.Features;

class Filter : Category
{
    public override string Name { get; } = nameof(Filter).ToReadableName();
    public override string Description { get; } = "Apply Filter to enhance or change the look of the photo!";
    public override Feature[] Features { get; } = new Feature[]
    {
        new Blur(),
        new MedianBlur(),
        new GaussianBlur(),
        new Grayscale(),
        new Invert(),
        new Sepia()
    };
}
class Grayscale : Feature
{
    public override string Name { get; } = nameof(Grayscale).ToReadableName();
    public override string Description { get; } = "Turns photo into grayscale";
    
    public Grayscale()
    {
        
    }
    protected override UIElement CreateUI()
    {
        return SimpleUI.GenerateLIVE(
            PageName: Name,
            PageDescription: Description,
            Parameters: new IParameterFromUI[]
            {
                new ImageParameter().Assign(out var ImageParam),
                new PercentSliderParameter("Intensity", 1.00).Assign(out var IntensityParm)
            },
            OnExecute: (MatImage) =>
            {
                using var tracker = new ResourcesTracker();
                var original = ImageParam.Result.Track(tracker);
                var intensity = IntensityParm.Result;
                Mat output;
                output = original.ToGray(out var alpha).Track(tracker)
                    .ToBGR().Track(tracker).InsertAlpha(alpha);
                Cv2.AddWeighted(output, intensity, original, 1 - intensity, 0, output);

                output.ImShow(MatImage);
            }
        );
    }
}
class Invert : Feature
{
    public override string Name { get; } = nameof(Invert).ToReadableName();
    public override string Description { get; } = "Invert RGB Color of the photo";
    public Invert()
    {
        
    }
    protected override UIElement CreateUI()
    {
        return SimpleUI.GenerateLIVE(
            PageName: Name,
            PageDescription: Description,
            Parameters: new IParameterFromUI[]
            {
                new ImageParameter().Assign(out var ImageParam),
                new PercentSliderParameter("Intensity", 1.00).Assign(out var IntensityParm)
            },
            OnExecute: (MatImage) =>
            {
                using var tracker = new ResourcesTracker();
                var original = ImageParam.Result.Track(tracker);
                var intensity = IntensityParm.Result;
                var output = new Mat();
                Cv2.Merge(new Mat[] {
                    (255 - original.ExtractChannel(0).Track(tracker)).Track(tracker),
                    (255 - original.ExtractChannel(1).Track(tracker)).Track(tracker),
                    (255 - original.ExtractChannel(2).Track(tracker)).Track(tracker),
                    original.ExtractChannel(3).Track(tracker)
                }, output);
                Cv2.AddWeighted(output, intensity, original, 1 - intensity, 0, output);
                output.ImShow(MatImage);
            }
        );
    }
}
class Sepia : Feature
{
    public override string Name { get; } = nameof(Sepia).ToReadableName();
    public override string Description { get; } = "Apply Sepia filter to the photo";
    public Sepia()
    {
        
    }
    protected override UIElement CreateUI()
    {
        return SimpleUI.GenerateLIVE(
            PageName: Name,
            PageDescription: Description,
            Parameters: new IParameterFromUI[]
            {
                new ImageParameter().Assign(out var ImageParam),
                new PercentSliderParameter("Intensity", 1.00).Assign(out var IntensityParm)
            },
            OnExecute: (MatImage) =>
            {
                using var tracker = new ResourcesTracker();
                var original = ImageParam.Result.Track(tracker);
                var intensity = IntensityParm.Result;
                Mat output;

                var bgr = original.ToBGR(out var originalA);
                output = bgr.SepiaFilter().Track(tracker);
                output = output.AsBytes().Track(tracker);

                if (originalA != null)
                    output = output.InsertAlpha(originalA);

                Cv2.AddWeighted(output, intensity, original, 1 - intensity, 0, output);
                output.ImShow(MatImage);
            }
        );
    }
}
class Blur : Feature
{
    public override string Name { get; } = nameof(Blur).ToReadableName();
    public override string Description { get; } = "Apply Mean Blur filter to the photo";
    public Blur()
    {
        
    }
    protected override UIElement CreateUI()
    {
        return SimpleUI.GenerateLIVE(
            PageName: Name,
            PageDescription: Description,
            Parameters: new IParameterFromUI[]
            {
                new ImageParameter().Assign(out var ImageParam),
                new IntSliderParameter("Kernal Size", Min: 1, Max: 101, StartingValue: 3).Assign(out var kernalSizeParam),
                new SelectParameter<BorderTypes>(Name: "Blur Border Mode", Enum.GetValues<BorderTypes>().Where(x => !(x == BorderTypes.Wrap || x == BorderTypes.Transparent)).Distinct().ToArray(), 3, x => x == BorderTypes.Default ? "Default (Reflect101)" : x.ToString()).Assign(out var BorderParam)
            },
            OnExecute: (MatImage) =>
            {
                using var tracker = new ResourcesTracker();
                var original = ImageParam.Result.Track(tracker);
                var k = kernalSizeParam.Result;
                var kernalsize = new Size(k, k);
                Mat output = original.Blur(kernalsize, borderType: BorderParam.Result);
                output.ImShow(MatImage);
            }
        );
    }
}
class MedianBlur : Feature
{
    public override string Name { get; } = nameof(MedianBlur).ToReadableName();
    public override string Description { get; } = "Apply Meadian Blur filter to the photo";
    public MedianBlur()
    {
        
    }
    protected override UIElement CreateUI()
    {
        return SimpleUI.GenerateLIVE(
            PageName: Name,
            PageDescription: Description,
            Parameters: new IParameterFromUI[]
            {
                new ImageParameter().Assign(out var ImageParam),
                new IntSliderParameter("Kernal Size", Min: 1, Max: 101, Step: 2, StartingValue: 3).Assign(out var kernalSizeParam),
            },
            OnExecute: (MatImage) =>
            {
                using var tracker = new ResourcesTracker();
                var original = ImageParam.Result.Track(tracker);
                var k = kernalSizeParam.Result;
                Mat output = original.MedianBlur(k);
                output.ImShow(MatImage);
            }
        );
    }
}
class GaussianBlur : Feature
{
    public override string Name { get; } = nameof(GaussianBlur).ToReadableName();
    public override string Description { get; } = "Apply Gaussian Blur filter to the photo";
    public GaussianBlur()
    {
        
    }
    protected override UIElement CreateUI()
    {
        return SimpleUI.GenerateLIVE(
            PageName: Name,
            PageDescription: Description,
            Parameters: new IParameterFromUI[]
            {
                new ImageParameter().Assign(out var ImageParam),
                new IntSliderParameter("Kernal Size", Min: 1, Max: 101, Step: 2, StartingValue: 3).Assign(out var kernalSizeParam),
                new DoubleSliderParameter("Standard Deviation X", Min: 0, Max: 30, Step: 0.01, StartingValue: 0, DisplayConverter: x => x == 0 ? "Default" : x.ToString("N2")).Assign(out var sigmaXParam),
                new DoubleSliderParameter("Standard Deviation Y", Min: 0, Max: 30, Step: 0.01, StartingValue: 0, DisplayConverter: x => x == 0 ? "Same as Standard Deviation X" : x.ToString("N2")).Assign(out var sigmaYParam),
                new SelectParameter<BorderTypes>(Name: "Blur Border Mode", Enum.GetValues<BorderTypes>().Where(x => x != BorderTypes.Transparent).Distinct().ToArray(), 4, x => x == BorderTypes.Default ? "Default (Reflect101)" : x.ToString()).Assign(out var BorderParam)
            },
            OnExecute: (MatImage) =>
            {
                using var tracker = new ResourcesTracker();
                var original = ImageParam.Result.Track(tracker);
                var k = kernalSizeParam.Result;
                var sigmaX = sigmaXParam.Result;
                var sigmaY = sigmaYParam.Result;
                var kernalsize = new Size(k, k);
                Mat output = original.GaussianBlur(kernalsize, sigmaX, sigmaY, borderType: BorderParam.Result);
                output.ImShow(MatImage);
            }
        );
    }
}
