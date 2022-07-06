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
    public override IconElement? Icon { get; } = new SymbolIcon((Symbol)0xF0E2); // Grid View
    public override Feature[] Features { get; } = new Feature[]
    {
        new Blur(),
        new MedianBlur(),
        new GaussianBlur(),
        new BilateralBlur(),
        new Grayscale(),
        new Invert(),
        new Sepia()
    };
}
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
class Blur : Feature
{
    public override string Name { get; } = nameof(Blur).ToReadableName();
    public override string Description { get; } = "Apply Mean Blur filter to the photo";
    public override IconElement? Icon { get; } = new SymbolIcon((Symbol)0xF0E2); // Grid View
    public Blur()
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
                new SelectParameter<BorderTypes>(Name: "Blur Border Mode", Enum.GetValues<BorderTypes>().Where(x => !(x == BorderTypes.Wrap || x == BorderTypes.Transparent)).Distinct().ToArray(), 3, x => (x == BorderTypes.Default ? "Default (Reflect101)" : x.ToString(), null)).Assign(out var BorderParam)
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
