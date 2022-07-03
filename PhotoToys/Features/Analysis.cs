using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OpenCvSharp;
using PhotoToys.Parameters;
using System;
using System.Collections.Generic;

namespace PhotoToys.Features;
class Analysis : Category
{
    public override string Name { get; } = nameof(Analysis).ToReadableName();
    public override string Description { get; } = "Analyze image by applying one of these feature extractor to see details of the image!";
    public override Feature[] Features { get; } = new Feature[]
    {
        new HistoramEqualization(),
        new EdgeDetection(),
        new HeatmapGeneration(),
        new Morphology()
    };
    public override IconElement? Icon { get; } = new SymbolIcon((Symbol)0xE9f5); // Processing
}

class HistoramEqualization : Feature
{
    public override string Name { get; } = nameof(HistoramEqualization).ToReadableName();
    public override IEnumerable<string> Allias => new string[] { "Detail", "Extract Feature", "Feature Extraction" };
    public override string Description { get; } = "Apply Histogram Equalization to see some details in the image. Keeps photo opacity the same";
    public HistoramEqualization()
    {

    }
    protected override UIElement CreateUI()
    {
        UIElement? Element = null;
        return Element = SimpleUI.GenerateLIVE(
            PageName: Name,
            PageDescription: Description,
            Parameters: new ImageParameter(OneChannelModeEnabled: true).Assign(out var ImageParam),
            OnExecute: (MatImage) =>
            {
                using var tracker = new ResourcesTracker();
                var mat = ImageParam.Result.Track(tracker);
                
                // Reference: https://stackoverflow.com/a/38312281
                var output = new Mat().Track(tracker);
                var arr = mat.InplaceCvtColor(ColorConversionCodes.BGR2YUV).Split().Track(tracker);
                Cv2.EqualizeHist(arr[0], arr[0]);
                Cv2.Merge(arr, output);
                output.InplaceCvtColor(ColorConversionCodes.YUV2BGR);
                output = ImageParam.PostProcess(output);

                output.ImShow(MatImage);
            }
        );
    }
}
class EdgeDetection : Feature
{
    public override string Name { get; } = nameof(EdgeDetection).ToReadableName();
    public override IEnumerable<string> Allias => new string[] { "Detect Edge", "Detecting Edge" };
    public override string Description { get; } = "Apply Simple Edge Detection by finding standard deviation of the photo. Keeps photo opacity the same";
    public EdgeDetection()
    {

    }
    protected override UIElement CreateUI()
    {
        return SimpleUI.GenerateLIVE(
            PageName: Name,
            PageDescription: Description,
            Parameters: new ParameterFromUI[] {
                new ImageParameter(OneChannelModeEnabled: true).Assign(out var ImageParam),
                new IntSliderParameter(Name: "Kernal Size", 1, 11, 3, 1).Assign(out var KernalSizeParam),
                new CheckboxParameter(Name: "Output as Heatmap", Default: false).Assign(out var HeatmapParam)
                    .AddDependency(ImageParam.OneChannelReplacement, x => !x, onNoResult: true),
                new SelectParameter<ColormapTypes>(Name: "Heatmap Colormap", Enum.GetValues<ColormapTypes>(), 2).Assign(out var ColormapTypeParam)
                    .AddDependency(HeatmapParam, x => x, onNoResult: true)
            },
            OnExecute: (MatImage) =>
            {
                using var tracker = new ResourcesTracker();
                Mat original = ImageParam.Result.Track(tracker);
                bool HeatmapMode = HeatmapParam.Result && !ImageParam.OneChannelReplacement.Result;
                ColormapTypes colormap = ColormapTypeParam.Result;
                Size kernalSize = new(KernalSizeParam.Result, KernalSizeParam.Result);
                Mat output;

                output = original.StdFilter(kernalSize).Track(tracker);
                output = 
                    (
                        (
                            output.ExtractChannel(0).Track(tracker) +
                            output.ExtractChannel(1).Track(tracker)
                        ).Track(tracker) +
                        output.ExtractChannel(2).Track(tracker)
                    ).Track(tracker);
                output = output.NormalBytes().Track(tracker);
                if (HeatmapMode)
                    output = output.Heatmap(colormap).Track(tracker);
                else
                    output = output.ToBGR().Track(tracker);

                output = ImageParam.PostProcess(output).Track(tracker);

                output.Clone().ImShow(MatImage);
            }
        );
    }
}
class HeatmapGeneration : Feature
{
    public override string Name { get; } = nameof(HeatmapGeneration).ToReadableName();
    public override string Description { get; } = "Construct Heatmap from Grayscale Images";
    public HeatmapGeneration()
    {

    }
    protected override UIElement CreateUI()
    {
        return SimpleUI.GenerateLIVE(
            PageName: Name,
            PageDescription: Description,
            Parameters: new ParameterFromUI[] {
                new ImageParameter(Name: "Grayscale Image", ColorMode: false).Assign(out var ImageParam),
                new SelectParameter<ColormapTypes>(Name: "Mode", Enum.GetValues<ColormapTypes>(), 2).Assign(out var ColormapTypeParam)
            },
            OnExecute: (MatImage) =>
            {
                using var tracker = new ResourcesTracker();
                var original = ImageParam.Result.Track(tracker);
                var colormap = ColormapTypeParam.Result;
                Mat output;

                output = original.Heatmap(colormap).Track(tracker);
                output = ImageParam.PostProcess(output);

                output.ImShow(MatImage);
            }
        );
    }
}
class Morphology : Feature
{
    enum ChannelName : int
    {
        Default = 0,
        ColorWithoutAlpha = 1,
        ConvertToGrayscale = 2,
        Red = 5,
        Green = 4,
        Blue = 3,
        Alpha = 6
    }
    public override string Name { get; } = nameof(Morphology).ToReadableName();
    public override IEnumerable<string> Allias => new string[] { $"{nameof(Morphology)}Ex", "Remove noise", "Detail", "Extract Feature", "Feature Extraction", "Detect Edge", "Detecting Edge" };
    public override string Description { get; } = "Apply morphological operations to remove noise, see more details, or extract feature";
    public Morphology()
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
                new IntSliderParameter(Name: "Kernal Size", 1, 100, 3).Assign(out var KernalSizeParam),
                new SelectParameter<MorphShapes>(Name: "Kernal Shape", Enum.GetValues<MorphShapes>()).Assign(out var KernalShapeParam),
                new SelectParameter<MorphTypes>(Name: "Morphology Type", Enum.GetValues<MorphTypes>(), ConverterToDisplay: x => (x.ToString(), x switch
                {
                    MorphTypes.Erode => "Remove noise from the image and make most of the element smaller",
                    MorphTypes.Dilate => "Enlarge the small details and make most of the element larger",
                    MorphTypes.Open => "Remove noise from the image while trying to maintain the same size",
                    MorphTypes.Close => "Fill in the hole while trying to maintain the same size",
                    _ => null
                })).Assign(out var MorphTypeParam)
                .Edit(x => x.ParameterValueChanged += delegate
                {
                    ImageParam.ColorMode = x.Result != MorphTypes.HitMiss;
                })
            },
            OnExecute: (MatImage) =>
            {

                using var tracker = new ResourcesTracker();
                Mat mat = ImageParam.Result.Track(tracker);
                int ks = KernalSizeParam.Result;
                MorphTypes mt = MorphTypeParam.Result;
                
                Cv2.MorphologyEx(mat, mat, mt,
                    Cv2.GetStructuringElement(KernalShapeParam.Result, new Size(ks, ks)).Track(tracker)
                );

                mat = ImageParam.PostProcess(mat);
                mat.ImShow(MatImage);
            }
        );
    }
}