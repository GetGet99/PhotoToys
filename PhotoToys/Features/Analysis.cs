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
            Parameters: new ImageParameter().Assign(out var ImageParam),
            OnExecute: (MatImage) =>
            {
                using var tracker = new ResourcesTracker();
                var original = ImageParam.Result.Track(tracker);
                
                // Reference: https://stackoverflow.com/a/38312281
                var bgr = original.ToBGR(out var originalA).Track(tracker);
                var yuv = original.CvtColor(ColorConversionCodes.BGR2YUV).Track(tracker);
                var output = new Mat().Track(tracker);
                Cv2.Merge(new Mat[]
                {
                        yuv.ExtractChannel(0).Track(tracker).EqualizeHist().Track(tracker),
                        yuv.ExtractChannel(1).Track(tracker),
                        yuv.ExtractChannel(2).Track(tracker)
                }, output);
                output = output.CvtColor(ColorConversionCodes.YUV2BGR).Track(tracker);
                if (originalA != null)
                    output = output.InsertAlpha(originalA.Track(tracker)).Track(tracker);

                output.Clone().ImShow(MatImage);
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
            Parameters: new IParameterFromUI[] {
                new ImageParameter().Assign(out var ImageParam),
                new IntSliderParameter(Name: "Kernal Size", 1, 11, 3, 1).Assign(out var KernalSizeParam),
                new CheckboxParameter(Name: "Output as Heatmap", Default: false).Assign(out var HeatmapParam),
                new SelectParameter<ColormapTypes>(Name: "Heatmap Colormap", Enum.GetValues<ColormapTypes>(), 2).Assign(out var ColormapTypeParam)
            },
            OnExecute: (MatImage) =>
            {
                using var tracker = new ResourcesTracker();
                Mat original = ImageParam.Result.Track(tracker);
                bool heatmap = HeatmapParam.Result;
                ColormapTypes colormap = ColormapTypeParam.Result;
                Size kernalSize = new(KernalSizeParam.Result, KernalSizeParam.Result);
                Mat output;

                var bgr = original.ToBGR(out var originalA).Track(tracker);
                output = bgr.StdFilter(kernalSize).Track(tracker);
                output = 
                    (
                        (
                            output.ExtractChannel(0).Track(tracker) +
                            output.ExtractChannel(1).Track(tracker)
                        ).Track(tracker) +
                        output.ExtractChannel(2).Track(tracker)
                    ).Track(tracker);
                output = output.NormalBytes().Track(tracker);
                if (heatmap)
                    output = output.Heatmap(colormap).Track(tracker);
                else
                    output = output.ToBGR().Track(tracker);
                if (originalA != null)
                    output = output.InsertAlpha(originalA.Track(tracker)).Track(tracker);

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
            Parameters: new IParameterFromUI[] {
                new ImageParameter(Name: "Grayscale Image (Non-grayscale image will be converted to grayscale image)").Assign(out var ImageParam),
                new SelectParameter<ColormapTypes>(Name: "Mode", Enum.GetValues<ColormapTypes>(), 2).Assign(out var ColormapTypeParam)
            },
            OnExecute: (MatImage) =>
            {
                using var tracker = new ResourcesTracker();
                var original = ImageParam.Result.Track(tracker);
                var colormap = ColormapTypeParam.Result;
                Mat output;

                var gray = original.ToGray(out var originalA).Track(tracker);
                output = gray.Heatmap(colormap).Track(tracker);
                if (originalA != null)
                    output = output.InsertAlpha(originalA.Track(tracker)).Track(tracker);

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
            Parameters: new IParameterFromUI[]
            {
                new ImageParameter().Assign(out var ImageParam),
                new SelectParameter<ChannelName>(Name: "Color Channel", Enum.GetValues<ChannelName>()).Assign(out var ChannelParam),
                new IntSliderParameter(Name: "Kernal Size", 1, 100, 3).Assign(out var KernalSizeParam),
                new SelectParameter<MorphShapes>(Name: "Kernal Shape", Enum.GetValues<MorphShapes>()).Assign(out var KernalShapeParam),
                new SelectParameter<MorphTypes>(Name: "Morphology Type", Enum.GetValues<MorphTypes>()).Assign(out var MorphTypeParam)
            },
            OnExecute: (MatImage) =>
            {

                using var tracker = new ResourcesTracker();
                Mat original = ImageParam.Result.Track(tracker);
                int ks = KernalSizeParam.Result;
                MorphTypes mt = MorphTypeParam.Result;
                switch (ChannelParam.Result)
                {
                    case ChannelName.Default:
                        if (mt == MorphTypes.HitMiss) goto case ChannelName.ConvertToGrayscale;
                        break;
                    case ChannelName.ColorWithoutAlpha:
                        original = original.ToBGR().Track(tracker);
                        break;
                    case ChannelName.ConvertToGrayscale:
                        original = original.ToGray().Track(tracker);
                        break;
                    default:
                        original = original.ExtractChannel((int)ChannelParam.Result - 3).Track(tracker);
                        break;
                }
                Mat output = original.MorphologyEx(mt,
                    Cv2.GetStructuringElement(KernalShapeParam.Result, new Size(ks, ks)).Track(tracker)
                );


                output.ImShow(MatImage);
            }
        );
    }
}