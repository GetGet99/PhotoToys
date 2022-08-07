using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OpenCvSharp;
using PhotoToys.Parameters;
using System;
using System.Linq;
using static PTMS.OpenCvExtension;
namespace PhotoToys.Features.Filter;

[DisplayName("Filter")]
[DisplayDescription("Apply Filter to enhance or change the look of the photo!")]
[DisplayIcon((Symbol)0xF0E2)] // Grid View
class Filter : Category
{
    public override IFeature[] Features { get; } = new IFeature[]
    {
        new Blurs(),
        new Grayscale(),
        new Invert(),
        new Pixelate(),
        new Sepia(),
        new Cartoon()
    };
}
[DisplayName("Blur Filters")]
[DisplayDescription("Apply different types of blur filters!")]
[DisplayIcon((Symbol)0xF0E2)] // Grid View
class Blurs : FeatureCategory
{
    public override Feature[] Features { get; } = new Feature[]
    {
        new MeanBlur(),
        new MedianBlur(),
        new GaussianBlur(),
        new BilateralBlur()
    };
}
[DisplayName("Grayscale")]
[DisplayDescription("Turns the into grayscale images")]
class Grayscale : Feature
{
    public override string Name { get; } = nameof(Grayscale).ToReadableName();
    public override string Description { get; } = "Turns photo into grayscale";
    public override IconElement? Icon { get; } = new SymbolIcon((Symbol)0xF0E2); // Grid View
    public Grayscale()
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
                new PercentSliderParameter("Intensity", 1.00).Assign(out var IntensityParm)
            },
            OnExecute: (MatImage) =>
            {
                using var tracker = new ResourcesTracker();
                var mat = ImageParam.Result.Track(tracker);
                var intensity = IntensityParm.Result;
                var graymat = mat.ToGray().Track(tracker).InplaceToBGR();
                Cv2.AddWeighted(graymat, intensity, mat, 1 - intensity, 0, dst: mat);
                var output = ImageParam.PostProcess(mat);

                output.ImShow(MatImage);
            }
        );
    }
}
class Invert : Feature
{
    public override string Name { get; } = nameof(Invert).ToReadableName();
    public override string Description { get; } = "Invert RGB Color of the photo";
    public override IconElement? Icon { get; } = new SymbolIcon((Symbol)0xF0E2); // Grid View
    public Invert()
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
                new PercentSliderParameter("Intensity", 1.00).Assign(out var IntensityParm)
            },
            OnExecute: (MatImage) =>
            {
                using var tracker = new ResourcesTracker();
                var original = ImageParam.Result.Track(tracker);
                var intensity = IntensityParm.Result;
                var output = (new Scalar(255, 255, 255) - original).Track(tracker).ToMat().Track(tracker);
                Cv2.AddWeighted(output, intensity, original, 1 - intensity, 0, output);
                output = ImageParam.PostProcess(output);
                output.ImShow(MatImage);
            }
        );
    }
}
class Sepia : Feature
{
    public override string Name { get; } = nameof(Sepia).ToReadableName();
    public override string Description { get; } = "Apply Sepia filter to the photo";
    public override IconElement? Icon { get; } = new SymbolIcon((Symbol)0xF0E2); // Grid View
    public Sepia()
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
                new PercentSliderParameter("Intensity", 1.00).Assign(out var IntensityParm)
            },
            OnExecute: (MatImage) =>
            {
                using var tracker = new ResourcesTracker();
                var original = ImageParam.Result.Track(tracker);
                var intensity = IntensityParm.Result;
                Mat output;

                output = original.SepiaFilter().InplaceAsBytes().Track(tracker);

                Cv2.AddWeighted(output, intensity, original, 1 - intensity, 0, output);

                output = ImageParam.PostProcess(output);

                output.ImShow(MatImage);
            }
        );
    }
}
class MeanBlur : Feature
{
    public override string Name { get; } = nameof(MeanBlur).ToReadableName();
    public override string Description { get; } = "Apply Mean Blur filter to the photo";
    public override IconElement? Icon { get; } = new SymbolIcon((Symbol)0xF0E2); // Grid View
    public MeanBlur()
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
                new IntSliderParameter("Kernal Size", Min: 1, Max: 101, StartingValue: 3).Assign(out var kernalSizeParam),
                new SelectParameter<BorderTypes>(Name: "Blur Border Mode", Enum.GetValues<BorderTypes>().Where(x => !(x == BorderTypes.Wrap || x == BorderTypes.Transparent)).Distinct().ToArray(), 3, x => (x == BorderTypes.Default ? "Default (Reflect101)" : x.ToString(), null)).Assign(out var BorderParam),
            },
            OnExecute: (MatImage) =>
            {
                using var tracker = new ResourcesTracker();
                var mat = ImageParam.Result.Track(tracker);
                var k = kernalSizeParam.Result;
                var kernalsize = new Size(k, k);
                Cv2.Blur(mat, mat, kernalsize, borderType: BorderParam.Result);
                var output = ImageParam.PostProcess(mat);
                output.ImShow(MatImage);
            }
        );
    }
}
class MedianBlur : Feature
{
    public override string Name { get; } = nameof(MedianBlur).ToReadableName();
    public override string Description { get; } = "Apply Meadian Blur filter to the photo";
    public override IconElement? Icon { get; } = new SymbolIcon((Symbol)0xF0E2); // Grid View
    public MedianBlur()
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
                new IntSliderParameter("Kernal Size", Min: 1, Max: 101, Step: 2, StartingValue: 3).Assign(out var kernalSizeParam),
            },
            OnExecute: (MatImage) =>
            {
                using var tracker = new ResourcesTracker();
                var mat = ImageParam.Result.Track(tracker);
                var k = kernalSizeParam.Result;
                Cv2.MedianBlur(mat, mat, k);
                var output = ImageParam.PostProcess(mat);
                output.ImShow(MatImage);
            }
        );
    }
}
class GaussianBlur : Feature
{
    public override string Name { get; } = nameof(GaussianBlur).ToReadableName();
    public override string Description { get; } = "Apply Gaussian Blur filter to the photo";
    public override IconElement? Icon { get; } = new SymbolIcon((Symbol)0xF0E2); // Grid View
    public GaussianBlur()
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
                new IntSliderParameter("Kernal Size", Min: 1, Max: 101, Step: 2, StartingValue: 3).Assign(out var kernalSizeParam),
                new DoubleSliderParameter("Standard Deviation X", Min: 0, Max: 30, Step: 0.01, StartingValue: 0, DisplayConverter: x => x == 0 ? "Default" : x.ToString("N2")).Assign(out var sigmaXParam),
                new DoubleSliderParameter("Standard Deviation Y", Min: 0, Max: 30, Step: 0.01, StartingValue: 0, DisplayConverter: x => x == 0 ? "Same as Standard Deviation X" : x.ToString("N2")).Assign(out var sigmaYParam),
                new SelectParameter<BorderTypes>(Name: "Blur Border Mode", Enum.GetValues<BorderTypes>().Where(x => x != BorderTypes.Transparent).Distinct().ToArray(), 4, x => (x == BorderTypes.Default ? "Default (Reflect101)"  : x.ToString(), null)).Assign(out var BorderParam)
            },
            OnExecute: (MatImage) =>
            {
                using var tracker = new ResourcesTracker();
                var mat = ImageParam.Result.Track(tracker);
                var k = kernalSizeParam.Result;
                var sigmaX = sigmaXParam.Result;
                var sigmaY = sigmaYParam.Result;
                var kernalsize = new Size(k, k);
                Cv2.GaussianBlur(mat, mat, kernalsize, sigmaX, sigmaY, borderType: BorderParam.Result);
                Mat output = ImageParam.PostProcess(mat);
                output.ImShow(MatImage);
            }
        );
    }
}
class BilateralBlur : Feature
{
    public override string Name { get; } = nameof(BilateralBlur).ToReadableName();
    public override string Description { get; } = "Apply Bilateral Filter to the photo to attempt to remove noises while keeping edges sharp";
    public override IconElement? Icon { get; } = new SymbolIcon((Symbol)0xF0E2); // Grid View
    public BilateralBlur()
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
                new DoubleSliderParameter("Diameter", Min: -0.01, Max: 100, Step: 0.01, StartingValue: -0.01, DisplayConverter: x => x == -0.01 ? "Default (Computed from Standard Deviation Space)" : x.ToString("N2")).Assign(out var kernalSizeParam),
                new DoubleSliderParameter("Standard Deviation Color", Min: 0, Max: 200, Step: 0.01, StartingValue: 0).Assign(out var sigmaXParam),
                new DoubleSliderParameter("Standard Deviation Space", Min: 0, Max: 200, Step: 0.01, StartingValue: 0).Assign(out var sigmaYParam),
                new SelectParameter<BorderTypes>(Name: "Blur Border Mode", Enum.GetValues<BorderTypes>().Where(x => x != BorderTypes.Transparent).Distinct().ToArray(), 4, x => (x == BorderTypes.Default ? "Default (Reflect101)"  : x.ToString(), null)).Assign(out var BorderParam)
            },
            OnExecute: (MatImage) =>
            {
                using var tracker = new ResourcesTracker();
                var mat = ImageParam.Result.Track(tracker);
                var d = (int)kernalSizeParam.Result;
                var sigmaColor = sigmaXParam.Result;
                var sigmaSpace = sigmaYParam.Result;
                mat = mat.BilateralFilter(d, sigmaColor, sigmaSpace, borderType: BorderParam.Result).Track(tracker);
                Mat output = ImageParam.PostProcess(mat);
                output.ImShow(MatImage);
            }
        );
    }
}
class Pixelate : Feature
{
    public override string Name { get; } = nameof(Pixelate).ToReadableName();
    public override string Description { get; } = "Apply Pixelate Filter to the photo";
    public override IconElement? Icon { get; } = new SymbolIcon((Symbol)0xF0E2); // Grid View
    public Pixelate()
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
                new PercentSliderParameter("Intensity", StartingValue: 0.10).Assign(out var IntensityParameter),
            },
            OnExecute: (MatImage) =>
            {
                using var tracker = new ResourcesTracker();
                var mat = ImageParam.Result.Track(tracker);
                var percent = 1 - Math.Pow(IntensityParameter.Result, 0.1);
                var width = mat.Width;
                var height = mat.Height;
                var pixelatewidth = (width * percent).Round();
                var pixelateheight = (height * percent).Round();
                if (pixelatewidth <= 0) pixelatewidth = 1;
                if (pixelateheight <= 0) pixelateheight = 1;

                var smallmat = mat.Resize(new Size(pixelatewidth, pixelateheight)).Track(tracker);

                mat = smallmat.Resize(new Size(width, height), interpolation: InterpolationFlags.Nearest).Track(tracker);

                Mat output = ImageParam.PostProcess(mat);
                output.ImShow(MatImage);
            }
        );
    }
}
class Cartoon : Feature
{
    public override string Name { get; } = nameof(Cartoon).ToReadableName();
    public override string Description { get; } = "Apply Cartoon Filter to the photo";
    public override IconElement? Icon { get; } = new SymbolIcon((Symbol)0xF0E2); // Grid View
    public Cartoon()
    {

    }
    enum EdgeModes
    {
        AdaptiveThreshold,
        StandardDeviation
    }
    protected override UIElement CreateUI()
    {
        return SimpleUI.GenerateLIVE(
            PageName: Name,
            PageDescription: Description,
            Parameters: new ParameterFromUI[]
            {
                new ImageParameter().Assign(out var ImageParam),
                new SelectParameter<EdgeModes>("Edge Mode", Enum.GetValues<EdgeModes>()).Assign(out var EdgeMode),
                new IntSliderParameter("Kernal Size", Min: 1, Max: 11, StartingValue: 3).Assign(out var StdKernalSize)
                .AddDependency(EdgeMode, x => x is EdgeModes.StandardDeviation),
                new PercentSliderParameter("Edge Level", 0.80).Assign(out var StdEdgeLevel)
                .AddDependency(EdgeMode, x => x is EdgeModes.StandardDeviation),
                new IntSliderParameter("Edge Noise Reduce", Min: -1, Max: 11, StartingValue: 1, Step: 2).Assign(out var EdgeNoiseReduce)
                .AddDependency(EdgeMode, x => x is EdgeModes.AdaptiveThreshold),
                new IntSliderParameter("Edge Detail", Min: 1, Max: 51, StartingValue: 9, Step: 2).Assign(out var Edge)
                .AddDependency(EdgeMode, x => x is EdgeModes.AdaptiveThreshold),
                new CheckboxParameter("Edge Only", false).Assign(out var EdgeOnly),
                new IntSliderParameter("Color Smoothness", Min: 1, Max: 50, StartingValue: 9).Assign(out var Smoothness)
                .AddDependency(EdgeOnly, x => !x),
                new PercentSliderParameter("Filter Intensity", 1.00).Assign(out var FilterIntensity)
            },
            OnExecute: (MatImage) =>
            {
                using var tracker = new ResourcesTracker();
                var original = ImageParam.Result.Track(tracker);
                var intensity = FilterIntensity.Result;
                // Reference: Modified from https://github.com/ethand91/opencv-cartoon-filter/blob/master/main.py

                var gray = original.ToGray().Track(tracker);
                var noiseReduce = EdgeNoiseReduce.Result;
                if (noiseReduce is >-1)
                {
                    Cv2.MedianBlur(gray, gray, ksize: noiseReduce);
                }
                Mat edges = EdgeMode.Result switch
                {
                    EdgeModes.AdaptiveThreshold => gray.AdaptiveThreshold(
                                                maxValue: 255,
                                                adaptiveMethod: AdaptiveThresholdTypes.MeanC,
                                                thresholdType: ThresholdTypes.Binary,
                                                blockSize: Edge.Result,
                                                c: 9
                                            ),
                    EdgeModes.StandardDeviation => original
                        .Track(tracker)
                        .StdFilter(new Size(StdKernalSize.Result, StdKernalSize.Result))
                        .Track(tracker)
                        .Magnitude()
                        .Normal1()
                        .Track(tracker)
                        .Apply(x =>
                        {
                            Mat m = new Mat(new Size(x.Width, x.Height), MatType.CV_8UC1, 255).Track(tracker);
                            m.SetTo(0, x.GreaterThan(1 - StdEdgeLevel.Result));
                            return m;
                        }),
                    _ => throw new Exception(),
                };
                Mat output = new Mat();
                if (EdgeOnly.Result)
                {
                    output = edges.ToBGR();
                } else
                {
                    var color = original.BilateralFilter(
                        d: Smoothness.Result,
                        sigmaColor: 200,
                        sigmaSpace: 200
                    ).Track(tracker);

                    Mat m = tracker.NewMat();
                    Cv2.BitwiseAnd(
                        src1: color,
                        src2: color,
                        dst: output,
                        mask: edges
                    );
                }

                Cv2.AddWeighted(output, intensity, original, 1 - intensity, 0, output);

                output = ImageParam.PostProcess(output);
                output.ImShow(MatImage);
            }
        );
    }
}
