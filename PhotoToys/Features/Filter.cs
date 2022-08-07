using DynamicLanguage;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OpenCvSharp;
using PhotoToys.Parameters;
using System;
using System.Linq;
using static PTMS.OpenCvExtension;
namespace PhotoToys.Features.Filter;

[DisplayName("Filter", Thai = "ฟิวเตอร์ (Filter)", Sinhala = "පෙරීම (Filter)")]
[DisplayDescription("Apply Filter to enhance or change the look of the photo!",
	Thai = "ใช้ฟิวเตอร์ต่างๆ เพื่อเปลี่ยนรูปร่างของรูป",
	Sinhala = "ඡායාරූපයේ පෙනුම වැඩි දියුණු කිරීමට හෝ වෙනස් කිරීමට පෙරහන (Filter) යොදන්න!"
)]
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
[DisplayName("Blur Filters", Thai = "ฟิวเตอร์เบลอ (Blur Filters)")]
[DisplayDescription("Apply different types of blur filters!", Thai = "เบลอภาพด้วยฟิวเตอร์หลายๆ แบบ")]
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
[DisplayName("Grayscale", Thai = "ขาวดำ")]
[DisplayDescription("Turns the into grayscale images", Thai = "ทำให้ภาพกลายเป็นภาพขาวดำ")]
[DisplayIcon((Symbol)0xF0E2)] // Grid View
class Grayscale : Feature
{
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
                new PercentSliderParameter(SystemLanguage.Intensity, 1.00).Assign(out var IntensityParm)
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
[DisplayName("Invert", Thai = "กลับสี (Invert)")]
[DisplayDescription("Invert RGB Color of the photo", Thai = "กลับสี RGB (Invert RGB)")]
[DisplayIcon((Symbol)0xF0E2)] // Grid View
class Invert : Feature
{
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
                new PercentSliderParameter(SystemLanguage.Intensity, 1.00).Assign(out var IntensityParm)
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
[DisplayName("Sepia", Thai = SystemLanguage.UseDefault)]
[DisplayDescription("Apply Sepia filter to the photo", Thai = "ใช้ฟิวเตอร์ Sepia")]
[DisplayIcon((Symbol)0xF0E2)] // Grid View
class Sepia : Feature
{
    public override string Name { get; } = nameof(Sepia).ToReadableName();
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
                new PercentSliderParameter(SystemLanguage.Intensity, 1.00).Assign(out var IntensityParm)
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

[DisplayName("Mean Blur", Thai = "เบลอ (แบบเฉลี่ย) (Mean Blur)")]
[DisplayDescription("Apply Mean Blur filter to the photo", Thai = "เบลอภาพด้วยฟิวเตอร์เบลอแบบค่าเฉลี่ย (Mean/Average)")]
[DisplayIcon((Symbol)0xF0E2)] // Grid View
class MeanBlur : Feature
{
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
                new IntSliderParameter(SystemLanguage.KernelSize, Min: 1, Max: 101, StartingValue: 3).Assign(out var kernalSizeParam),
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
[DisplayName("Median Blur", Thai = "เบลอ (แบบค่ามัธยฐาน) (Median Blur)")]
[DisplayDescription("Apply Meadian Blur filter to the photo", Thai = "เบลอภาพด้วยฟิวเตอร์เบลอแบบค่ามัธยฐาน (Median)")]
[DisplayIcon((Symbol)0xF0E2)] // Grid View
class MedianBlur : Feature
{
    protected override UIElement CreateUI()
    {
        return SimpleUI.GenerateLIVE(
            PageName: Name,
            PageDescription: Description,
            Parameters: new ParameterFromUI[]
            {
                new ImageParameter().Assign(out var ImageParam),
                new IntSliderParameter(SystemLanguage.KernelSize, Min: 1, Max: 101, Step: 2, StartingValue: 3).Assign(out var kernalSizeParam),
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
[DisplayName("Gaussian Blur", Thai = "เกาส์เซียน เบลอ (Gaussian Blur)")]
[DisplayDescription("Apply Gaussian Blur filter to the photo", Thai = "เบลอภาพด้วยฟิวเตอร์ เกาส์เซียน เบลอ (Gaussian Blur)")]
[DisplayIcon((Symbol)0xF0E2)] // Grid View
class GaussianBlur : Feature
{
    protected override UIElement CreateUI()
    {
        return SimpleUI.GenerateLIVE(
            PageName: Name,
            PageDescription: Description,
            Parameters: new ParameterFromUI[]
            {
                new ImageParameter().Assign(out var ImageParam),
                new IntSliderParameter(SystemLanguage.KernelSize, Min: 1, Max: 101, Step: 2, StartingValue: 3).Assign(out var kernalSizeParam),
                new DoubleSliderParameter($"{SystemLanguage.StandardDeviation} X", Min: 0, Max: 30, Step: 0.01, StartingValue: 0, DisplayConverter: x => x == 0 ? "Default" : x.ToString("N2")).Assign(out var sigmaXParam),
                new DoubleSliderParameter($"{SystemLanguage.StandardDeviation} Y", Min: 0, Max: 30, Step: 0.01, StartingValue: 0, DisplayConverter: x => x == 0 ? "Same as Standard Deviation X" : x.ToString("N2")).Assign(out var sigmaYParam),
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
[DisplayName("Bilateral Blur", Thai = "เบลอแบบทวิภาคี (Bilateral Blur)")]
[DisplayDescription("Apply Bilateral filter to the photo", Thai = "เบลอภาพด้วยฟิวเตอร์เบลอแบบทวิภาคี (Bilateral Blur)")]
[DisplayIcon((Symbol)0xF0E2)] // Grid View
class BilateralBlur : Feature
{
    protected override UIElement CreateUI()
    {
        return SimpleUI.GenerateLIVE(
            PageName: Name,
            PageDescription: Description,
            Parameters: new ParameterFromUI[]
            {
                new ImageParameter().Assign(out var ImageParam),
                new DoubleSliderParameter("Diameter", Min: -0.01, Max: 100, Step: 0.01, StartingValue: -0.01, DisplayConverter: x => x == -0.01 ? "Default (Computed from Standard Deviation Space)" : x.ToString("N2")).Assign(out var kernalSizeParam),
                new DoubleSliderParameter($"{SystemLanguage.StandardDeviation} Color", Min: 0, Max: 200, Step: 0.01, StartingValue: 0).Assign(out var sigmaXParam),
                new DoubleSliderParameter($"{SystemLanguage.StandardDeviation} Space", Min: 0, Max: 200, Step: 0.01, StartingValue: 0).Assign(out var sigmaYParam),
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
[DisplayName("Pixelate", Thai = SystemLanguage.UseDefault)]
[DisplayDescription("Apply Pixelate Filter to the photo", Thai = "ทำการเซ็นเซอร์ภาพ หรือทำให้ภาพชัดน้อยลงด้วยฟิวเตอร์ Pixelate")]
[DisplayIcon((Symbol)0xF0E2)] // Grid View
class Pixelate : Feature
{
    protected override UIElement CreateUI()
    {
        return SimpleUI.GenerateLIVE(
            PageName: Name,
            PageDescription: Description,
            Parameters: new ParameterFromUI[]
            {
                new ImageParameter().Assign(out var ImageParam),
                new PercentSliderParameter(SystemLanguage.Intensity, StartingValue: 0.10).Assign(out var IntensityParameter),
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
[DisplayName("Cartoon", Thai = "การ์ตูน (Cartoon)")]
[DisplayDescription("Apply Cartoon Filter to the photo", Thai = "ทำให้ภาพกลายเป็นการ์ตูนด้วยฟิวเตอร์การ์ตูน")]
[DisplayIcon((Symbol)0xF0E2)] // Grid View
class Cartoon : Feature
{
    enum EdgeModes
    {
        [DisplayText("Adaptive Threshold", Thai = SystemLanguage.UseDefault)]
        AdaptiveThreshold,
        [SystemLanguageLink(nameof(SystemLanguage.StandardDeviation))]
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
                new IntSliderParameter(SystemLanguage.KernelSize, Min: 1, Max: 11, StartingValue: 3).Assign(out var StdKernalSize)
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
