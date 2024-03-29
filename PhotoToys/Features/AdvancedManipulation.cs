﻿using Microsoft.UI.Xaml;
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
using static DynamicLanguage.Extension;
using DynamicLanguage;
namespace PhotoToys.Features.AdvancedManipulation;
[DisplayName("Advanced Manipulation",Sinhala = "උසස් හැසිරවීම")]
[DisplayDescription("Apply advanced manipulation features",Sinhala = "උසස් හැසිරවීමේ විශේෂාංග යොදන්න")]
[DisplayIcon((Symbol)0xE950)] // Component Font Icon (looks like CPU)
class AdvancedManipulation : Category
{

    public override Feature[] Features { get; } = new Feature[]
    {
        new Mathematics(),
#if DEBUG
        new DebugFeature()
#endif
    };
}


[DisplayName(
    Default: "Mathematics",
    Sinhala = "ගණිතය"
)]
[DisplayDescription(
    Default: "Apply some mathematics functions to change the appearance of the image!",
    Sinhala = "රූපයේ පෙනුම වෙනස් කිරීමට ගණිතමය කාර්යයන් යොදන්න"
)]
[DisplayIcon(Symbol.Calculator)]
class Mathematics : Feature
{
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
                new ButtonParameter(GetDisplayText(new DisplayTextAttribute(
                    Default:"Environment")
                { 
                    Sinhala = "පරිසරය"
                }), 
                GetDisplayText(new DisplayTextAttribute(
                    Default:"Reset Runtime Environment")
                {
                    Sinhala = "ධාවන කාල පරිසරය (Runtime Environment) නැවත සකසන්න"
                }), OnClick: delegate
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
                new StringTextBoxParameter(
               GetDisplayText(new DisplayTextAttribute(
                    Default:"PTMS Expression (Use 'x' to refer to the image)")
                {
                    Sinhala = "PTMS ප්‍රකාශනය (රූපය ලෙස 'x' භාවිතා කරන්න)"
                }), 
                GetDisplayText(new DisplayTextAttribute(
                    Default:"Type Expression Here")
                {
                    Sinhala = "ප්‍රකාශනය මෙහි ටයිප් කරන්න"
                }), Font: new Microsoft.UI.Xaml.Media.FontFamily("Cascadia Mono"), IsReady: async (x, p) =>
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
                            Title = GetDisplayText(new DisplayTextAttribute("Parse Error") { Sinhala = "විග්‍රහ කිරීමේ දෝෂයකි" }),
                            Content = et.Message,
                            PrimaryButtonText = SystemLanguage.Okay
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
                                Title = DynamicLanguage.SystemLanguage.Error,
                                Content = et2.Message,
                                PrimaryButtonText = DynamicLanguage.SystemLanguage.Okay
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
                                Title = GetDisplayText(new DisplayTextAttribute("Result") { Sinhala = "ප්‍රතිඵලය" }),
                                Content = output.ToString(),
                                PrimaryButtonText = DynamicLanguage.SystemLanguage.Okay
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