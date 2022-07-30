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
using static PTMS.OpenCvExtension;
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
    PTMS.Environment SyntaxCheckEv { get; } = new();
    PTMS.Environment RuntimeEv { get; } = new();
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
                new ImageParameter(ColorChangable: false, AlphaRestoreChangable: false, AlphaMode: ImageParameter.AlphaModes.Include, MatrixMode: true)
                .Edit(imageParameter => imageParameter.ParameterValueChanged += delegate
                {
                    if (imageParameter.ResultReady)
                    {
                        if (RuntimeEv.Values.TryGetValue("x", out var value))
                            if (value is IDisposable disposable)
                                disposable.Dispose();
                        RuntimeEv.Values["x"] = PTMS.Extension.GenerateMatToken(imageParameter.Result, PTMS.MatrixType.BGRA);
                    }
                })
                .Assign(out var imageParameter),
                new ButtonParameter("Environment", "Reset Runtime Environment", OnClick: delegate
                {
                    var enu = from x in RuntimeEv.Values
                    let disposbale = x.Value as IDisposable
                    where disposbale is not null
                    select disposbale;

                    RuntimeEv.Values.Clear();
                    RuntimeEv.Functions.Clear();
                    if (imageParameter.ResultReady)
                    {
                        RuntimeEv.Values["x"] = PTMS.Extension.GenerateMatToken(imageParameter.Result, PTMS.MatrixType.BGRA);
                    }
                    return false;
                }),
                new StringTextBoxParameter("PTMS Expression (Use 'x' to refer to the image)", "Type Expression Here", Font: new Microsoft.UI.Xaml.Media.FontFamily("Cascadia Mono"), IsReady: async (x, p) =>
                {
                    return await Task.Run(() =>
                    {
                        var output = PTMS.PTMSParser.Parse(x, SyntaxCheckEv);
                        if (output is PTMS.ErrorToken et)
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
                }).Assign(out var exprTextParameter)
            },
            OnExecute: (async x =>
            {
                var tracker = new ResourcesTracker();
                var expr = PTMS.PTMSParser.Parse(exprTextParameter.Result, RuntimeEv);
                if (expr is PTMS.ErrorToken et)
                {
                    if (UIElement is not null)
                        await new ContentDialog
                        {
                            XamlRoot = UIElement.XamlRoot,
                            Title = "Parse Error",
                            Content = et.Message,
                            PrimaryButtonText = "Okay"
                        }.ShowAsync();
                } else if (expr is PTMS.IValueToken ValueToken)
                {
                    var output = PTMS.Extension.Evaluate(ValueToken);
                    if (output is PTMS.ErrorToken et2)
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
                    else if (output is PTMS.IMatValueToken mvt)
                    {
                        var mat = mvt.Mat;
                        switch (mvt.Type)
                        {
                            case PTMS.MatrixType.UnknownImage:
                                mat = mat.AsDoubles();
                                goto case PTMS.MatrixType.Matrix;
                            case PTMS.MatrixType.Matrix:
                                mat.ImShow(x);
                                break;
                            default:
                                if (mvt is PTMS.IImageMatToken imgtk)
                                {
                                    imgtk.GetBGRAImage().ImShow(x);
                                    break;
                                } else goto case PTMS.MatrixType.UnknownImage;
                        }
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
            })
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
    PTMS.Environment SyntaxCheckEv { get; } = new();
    PTMS.Environment RuntimeEv { get; } = new();
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