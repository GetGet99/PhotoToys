using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OpenCvSharp;
using PhotoToys.Parameters;
using System;

namespace PhotoToys.Features;
class Analysis : Category
{
    public override string Name { get; } = nameof(Analysis).ToReadableName();
    public override string Description { get; } = "Analyze image by applying one of these feature extractor to see details of the image!";
    public override Feature[] Features { get; } = new Feature[]
    {
        new HistoramEqualization(),
        new EdgeDetection(),
        new HeatmapGeneration()
    };
}

class HistoramEqualization : Feature
{
    public override string Name { get; } = nameof(HistoramEqualization).ToReadableName();
    public override string Description { get; } = "Apply Histogram Equalization to see some details in the image. Keeps photo opacity the same";
    public override UIElement UIContent { get; }
    public HistoramEqualization()
    {
        UIContent = SimpleUI.Generate(
            PageName: Name,
            PageDescription: Description,
            Parameters: new ImageParameter().Assign(out var ImageParam),
            OnExecute: async delegate
            {
                using var t = new ResourcesTracker();
                var original = ImageParam.Result;
                // Reference: https://stackoverflow.com/a/38312281
                Mat output;
                Mat yuv;
                Mat? originalA = null;
                switch (original.Channels())
                {
                    case 1:
                        output = original.EqualizeHist();
                        if (UIContent != null) await output.ImShow("Result", XamlRoot: UIContent.XamlRoot);
                        return;
                    case 4:
                        originalA = t.T(original.ExtractChannel(3));
                        yuv = t.T(t.T(original.CvtColor(ColorConversionCodes.BGRA2BGR)).CvtColor(ColorConversionCodes.BGR2YUV));
                        break;
                    case 3:
                        yuv = t.T(original.CvtColor(ColorConversionCodes.BGR2YUV));
                        break;
                    default:
                        return;
                }
                output = t.NewMat();
                Cv2.Merge(new Mat[]
                {
                    t.T(t.T(yuv.ExtractChannel(0)).EqualizeHist()),
                    t.T(yuv.ExtractChannel(1)),
                    t.T(yuv.ExtractChannel(2))
                }, output);
                output = output.CvtColor(ColorConversionCodes.YUV2BGR);
                if (originalA != null)
                    output = t.T(output).InsertAlpha(originalA);

                if (UIContent != null) await output.ImShow("Result", XamlRoot: UIContent.XamlRoot);

                //# equalize the histogram of the Y channel
                //img_yuv[:,:, 0] = cv2.equalizeHist(img_yuv[:,:, 0])

                //# convert the YUV image back to RGB format
                //img_output = cv2.cvtColor(img_yuv, cv2.COLOR_YUV2BGR)

                //cv2.imshow('Color input image', img)
                //cv2.imshow('Histogram equalized', img_output)

                //cv2.waitKey(0)
            }
        );
    }
}
class EdgeDetection : Feature
{
    public override string Name { get; } = nameof(EdgeDetection).ToReadableName();
    public override string Description { get; } = "Apply Simple Edge Detection by finding standard deviation of the photo. Keeps photo opacity the same";
    public override UIElement UIContent { get; }
    public EdgeDetection()
    {
        UIContent = SimpleUI.Generate(
            PageName: Name,
            PageDescription: Description,
            Parameters: new IParameterFromUI[] {
                new ImageParameter().Assign(out var ImageParam),
                new IntSliderParameter(Name: "Kernal Size", 1, 11, 3, 1).Assign(out var KernalSizeParam),
                new CheckboxParameter(Name: "Output as Heatmap", Default: false).Assign(out var HeatmapParam),
                new SelectParameter<ColormapTypes>(Name: "Heatmap Colormap", Enum.GetValues<ColormapTypes>(), 2).Assign(out var ColormapTypeParam)
            },
            OnExecute: async delegate
            {
                using var t = new ResourcesTracker();
                var original = ImageParam.Result;
                var heatmap = HeatmapParam.Result;
                var colormap = ColormapTypeParam.Result;
                var kernalSize = new Size(KernalSizeParam.Result, KernalSizeParam.Result);
                Mat output;

                switch (original.Channels())
                {
                    case 1:
                        output = t.T(original.CvtColor(ColorConversionCodes.GRAY2BGR));
                        output = t.T(output.StdFilter(kernalSize));
                        output = output.AsBytes();
                        break;
                    case 4:
                        var originalA = original.ExtractChannel(3);
                        output = t.T(original.CvtColor(ColorConversionCodes.BGRA2BGR));
                        output = t.T(output.StdFilter(kernalSize));
                        output = t.T((t.T(output.ExtractChannel(0)) + t.T(output.ExtractChannel(1)) + t.T(output.ExtractChannel(2))).ToMat().NormalBytes());
                        if (heatmap)
                            output = t.T(output.Heatmap(colormap));
                        else
                            output = t.T(output.CvtColor(ColorConversionCodes.GRAY2BGR));
                        output = t.T(output.InsertAlpha(originalA));
                        break;
                    case 3:
                        output = t.T(original.StdFilter(kernalSize));
                        output = t.T((t.T(output.ExtractChannel(0)) + t.T(output.ExtractChannel(1)) + t.T(output.ExtractChannel(2))).ToMat()).NormalBytes();
                        break;
                    default:
                        return;
                }

                if (UIContent != null) await output.ImShow("Result", XamlRoot: UIContent.XamlRoot);
            }
        );
    }
}
class HeatmapGeneration : Feature
{
    public override string Name { get; } = nameof(HeatmapGeneration).ToReadableName();
    public override string Description { get; } = "Construct Heatmap from Grayscale Images";
    public override UIElement UIContent { get; }
    public HeatmapGeneration()
    {
        UIContent = SimpleUI.Generate(
            PageName: Name,
            PageDescription: Description,
            Parameters: new IParameterFromUI[] {
                new ImageParameter(Name: "Grayscale Image (Non-grayscale image will be converted to grayscale image)").Assign(out var ImageParam),
                new SelectParameter<ColormapTypes>(Name: "Mode", Enum.GetValues<ColormapTypes>(), 2).Assign(out var ColormapTypeParam)
            },
            OnExecute: async delegate
            {
                using var t = new ResourcesTracker();
                var original = ImageParam.Result;
                var colormap = ColormapTypeParam.Result;
                Mat output;

                switch (original.Channels())
                {
                    case 4:
                        var originalA = original.ExtractChannel(3);
                        output = t.T(t.T(original.CvtColor(ColorConversionCodes.BGRA2BGR)).CvtColor(ColorConversionCodes.BGR2GRAY));
                        output = t.T(output.Heatmap(colormap));
                        output = t.T(output.InsertAlpha(originalA));
                        break;
                    default:
                        return;
                }

                if (UIContent != null) await output.ImShow("Result", XamlRoot: UIContent.XamlRoot);
            }
        );
    }
}