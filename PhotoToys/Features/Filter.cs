using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OpenCvSharp;
using PhotoToys.Parameters;

namespace PhotoToys.Features;

class Filter : Category
{
    public override string Name { get; } = nameof(Filter).ToReadableName();
    public override string Description { get; } = "Apply Filter to enhance or change the look of the photo!";
    public override Feature[] Features { get; } = new Feature[]
    {
        new Grayscale(),
        new Invert(),
        new Sepia(),
    };
}
class Grayscale : Feature
{
    public override string Name { get; } = nameof(Grayscale).ToReadableName();
    public override string Description { get; } = "Remove color and opacity from the photo";
    public override UIElement UIContent { get; }
    public Grayscale()
    {
        UIContent = SimpleUI.Generate(
            PageName: Name,
            PageDescription: Description,
            Parameters: new ImageParameter().Assign(out var ImageParam),
            OnExecute: async delegate
            {
                var original = ImageParam.Result;
                Mat output;
                switch (original.Channels())
                {
                    case 1:
                        output = original.Clone();
                        return;
                    case 4:
                        output = original.CvtColor(ColorConversionCodes.BGRA2GRAY);
                        break;
                    case 3:
                        output = original.CvtColor(ColorConversionCodes.BGR2GRAY);
                        break;
                    default:
                        return;
                }

                if (UIContent != null) await output.ImShow("Result", XamlRoot: UIContent.XamlRoot);
            }
        );
    }
}
class Invert : Feature
{
    public override string Name { get; } = nameof(Invert).ToReadableName();
    public override string Description { get; } = "Invert RGB Color of the photo. Keeps photo opacity the same";
    public override UIElement UIContent { get; }
    public Invert()
    {
        UIContent = SimpleUI.Generate(
            PageName: Name,
            PageDescription: Description,
            Parameters: new ImageParameter().Assign(out var ImageParam),
            OnExecute: async delegate
            {
                var original = ImageParam.Result;
                using var t = new ResourcesTracker();
                var output = new Mat();
                if (original.Type() == MatType.CV_8UC3)
                    Cv2.Merge(new Mat[] {
                        t.T(255 - t.T(original.ExtractChannel(0))),
                        t.T(255 - t.T(original.ExtractChannel(1))),
                        t.T(255 - t.T(original.ExtractChannel(2)))
                    }, output);
                else if (original.Type() == MatType.CV_8UC4)
                    Cv2.Merge(new Mat[] {
                        t.T(255 - t.T(original.ExtractChannel(0))),
                        t.T(255 - t.T(original.ExtractChannel(1))),
                        t.T(255 - t.T(original.ExtractChannel(2))),
                        t.T(original.ExtractChannel(3))
                    }, output);
                else if (original.Type() == MatType.CV_8UC1 || original.Type() == MatType.CV_8U)
                    output = 255 - output;
                else
                {
                    original.MinMaxIdx(out var min, out var max);
                    output = 255 - original;
                }
                if (UIContent != null) await output.ImShow("Result", XamlRoot: UIContent.XamlRoot);
            }
        );
    }
}
class Sepia : Feature
{
    public override string Name { get; } = nameof(Sepia).ToReadableName();
    public override string Description { get; } = "Apply Sepia filter to the photo. Keeps photo opacity the same";
    public override UIElement UIContent { get; }
    public Sepia()
    {
        UIContent = SimpleUI.Generate(
            PageName: Name,
            PageDescription: Description,
            Parameters: new ImageParameter().Assign(out var ImageParam),
            OnExecute: async delegate
            {
                using var t = new ResourcesTracker();
                var original = ImageParam.Result;
                Mat output;
                
                switch (original.Channels())
                {
                    case 1:
                        output = t.T(original.CvtColor(ColorConversionCodes.GRAY2BGR));
                        output = t.T(output.SepiaFilter());
                        output = output.AsBytes();
                        break;
                    case 4:
                        var originalA = original.ExtractChannel(2);
                        output = t.T(original.CvtColor(ColorConversionCodes.BGRA2BGR));
                        output = t.T(output.SepiaFilter());
                        output = t.T(output.AsBytes());
                        output = output.InsertAlpha(originalA);
                        break;
                    case 3:
                        output = t.T(original.SepiaFilter());
                        output = output.AsBytes();
                        break;
                    default:
                        return;
                }

                if (UIContent != null) await output.ImShow("Result", XamlRoot: UIContent.XamlRoot);
            }
        );
    }
}