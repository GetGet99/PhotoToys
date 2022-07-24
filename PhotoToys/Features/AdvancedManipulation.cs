using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OpenCvSharp;
using PhotoToys.Parameters;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Diagnostics;
using System.IO;
using Windows.Storage;
using System.Threading;
namespace PhotoToys.Features.AdvancedManipulation;
class AdvancedManipulation : Category
{

    public override string Name { get; } = nameof(AdvancedManipulation).ToReadableName();
    public override string Description { get; } = "Apply advanced manipulation features";
    public override Feature[] Features { get; } = new Feature[]
    {
        new Mathematics(),
#if DEBUG
        new DebugFeature()
#endif
    };
    public override IconElement? Icon => new SymbolIcon((Symbol)0xE950); // Component Font Icon (looks like CPU)
}
class Mathematics : Feature
{
    public override string Name { get; } = $"{nameof(Mathematics)} (Alpha)";
    public override string Description => "Applying some mathematics functions to change the appearance of the image";
    public override IconElement? Icon => new SymbolIcon(Symbol.Calculator);
    MathScript.Environment SyntaxCheckEv { get; } = new();
    MathScript.Environment RuntimeEv { get; } = new();
    enum ManipulationMode
    {
        Normalize
    }
    protected override UIElement CreateUI()
    {
        UIElement? UIElement = null;
        UIElement = SimpleUI.GenerateLIVE(
            PageName: Name,
            PageDescription: Description,
            MatDisplayer: new DoubleMatDisplayer(),
            Parameters: new ParameterFromUI[]
            {
                new ImageParameter(ColorChangable: false, AlphaRestoreChangable: false, AlphaMode: ImageParameter.AlphaModes.Include, MatrixMode: true).Assign(out var imageParameter),
                new StringTextBoxParameter("PTMS Expression (Use 'x' to refer to the image)", "Type Expression Here", Font: new Microsoft.UI.Xaml.Media.FontFamily("Cascadia Mono"), IsReady: async (x, p) =>
                {
                    return await Task.Run(() =>
                    {
                        var output = MathScript.MathParser.Parse(x, SyntaxCheckEv);
                        if (output is MathScript.ErrorToken et)
                        {
                            p.ConfirmButton.DispatcherQueue.TryEnqueue(delegate
                            {
                                ToolTipService.SetToolTip(p.TextBox, et.Message);
                            });
                            return false;
                        } else
                        {
                            p.ConfirmButton.DispatcherQueue.TryEnqueue(delegate
                            {
                                ToolTipService.SetToolTip(p.TextBox, null);
                            });
                            return true;
                        }
                    });
                }).Assign(out var exprTextParameter),
                new CheckboxParameter("Automatically convert output to images (if value is in range)", true).Assign(out var AutoConvertParam),
            },
            OnExecute: async x =>
            {
                var tracker = new ResourcesTracker();
                if (RuntimeEv.Values.TryGetValue("x", out var value))
                    if (value is MathScript.IMatValueToken mvt)
                        mvt.Mat.Dispose();
                RuntimeEv.Values["x"] = new MathScript.MatToken { Mat = imageParameter.Result };
                var expr = MathScript.MathParser.Parse(exprTextParameter.Result, RuntimeEv);
                if (expr is MathScript.ErrorToken et)
                {
                    if (UIElement is not null)
                        await new ContentDialog
                        {
                            XamlRoot = UIElement.XamlRoot,
                            Title = "Parse Error",
                            Content = et.Message,
                            PrimaryButtonText = "Okay"
                        }.ShowAsync();
                } else if (expr is MathScript.IValueToken ValueToken)
                {
                    var output = MathScript.Extension.Evaluate(ValueToken);
                    if (output is MathScript.ErrorToken et2)
                    {
                        if (UIElement is not null)
                            await new ContentDialog
                            {
                                XamlRoot = UIElement.XamlRoot,
                                Title = "Error",
                                Content = et2.Message,
                                PrimaryButtonText = "Okay"
                            }.ShowAsync();
                    }
                    else if (output is MathScript.IMatValueToken mvt)
                    {
                        var mat = mvt.Mat;
                        if (AutoConvertParam.Result && mat.IsCompatableNumberMatrix())
                        {
                            mat.MinMaxLoc(out double a, out double b);
                            if (mat.Channels() is 1 or 3 or 4 && Cv2.CheckRange(mat, true, out var _, minVal: 0, maxVal: 255.5))
                            {
                                var oldmat = mat;
                                mat = mat.AsBytes();
                                oldmat.Dispose();
                            }
                        }
                        mat.ImShow(x);
                        //mat.Dispose();
                    } else
                    {
                        if (UIElement is not null)
                            await new ContentDialog
                            {
                                XamlRoot = UIElement.XamlRoot,
                                Title = "Result",
                                Content = output.ToString(),
                                PrimaryButtonText = "Okay"
                            }.ShowAsync();
                    }
                }
            }
        );
        
        return UIElement;
    }
}
#if DEBUG
class DebugFeature : Feature
{
    public override string Name { get; } = $"{nameof(DebugFeature)}";
    public override string Description => "Feature Trial that is not yet public";
    public override IconElement? Icon => new SymbolIcon(Symbol.Calculator);
    MathScript.Environment SyntaxCheckEv { get; } = new();
    MathScript.Environment RuntimeEv { get; } = new();
    enum ManipulationMode
    {
        Normalize
    }
    protected override UIElement CreateUI()
    {
        UIElement? UIElement = null;
        UIElement = SimpleUI.GenerateLIVE(
            PageName: Name,
            PageDescription: Description,
            Parameters: new ParameterFromUI[]
            {
                new ImageParameter(ColorChangable: false, AlphaRestoreChangable: false, AlphaMode: ImageParameter.AlphaModes.Include).Assign(out var imageParameter),
                RectLocationPickerParameter<MatImageDisplayer>.CreateWithImageParameter("Location", imageParameter)
            },
            OnExecute: x =>
            {
                //var tracker = new ResourcesTracker();
                //if (UIElement is not null)
                //    await imageParameter.Result.InsertAlpha(imageParameter.AlphaResult).CopyMakeBorder(100, 100, 100, 100, BorderTypes.Constant, value: new Scalar(66, 66, 66, 255)).ImShow("Result", UIElement.XamlRoot);
            }
        );

        return UIElement;
    }
}
#endif