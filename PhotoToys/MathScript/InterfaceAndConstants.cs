using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenCvSharp;
using PhotoToys;
using Range = System.Range;

namespace MathScript;

interface IToken
{

}
interface ISimpleToken : IToken
{

}
interface IValueToken : IToken
{

}
interface INativeToken : IToken
{

}
interface INumberValueToken : IValueToken, INativeToken
{
    double Number { get; set; }
    int NumberAsInt => (int)Math.Round(Number);
}
interface IMatValueToken : IValueToken, INativeToken, IDisposable
{
    MatType Type { get; }
    Mat Mat { get; set; }
}
interface IRangeValueToken : IValueToken, INativeToken
{
    System.Range Range { get; set; }
}
interface INothingValueToken : IValueToken, INativeToken
{

}
interface IBoolean : IValueToken, INativeToken
{
    bool Value { get; }
}
public struct BooleanToken : IBoolean {
    public bool Value { get; set; }
    public static BooleanToken True => new BooleanToken { Value = true };
    public static BooleanToken False => new BooleanToken { Value = false };
}
interface IOperableToken : IToken
{

}
interface ICompoundToken : IToken
{

}
public enum SystemTokenType
{
    Dot
}
public enum OperatorTokenType
{
    Power,
    Times,
    Divide,
    Mod,
    Plus,
    Minus,
    Equal,
    GreaterThan,
    GreaterThanOrEqual,
    LessThan,
    LessThanOrEqual,
    Range,
    Assign
}
public enum BracketTokenType
{
    OpenBracket,
    CloseBracket,
    OpenIndexBracket,
    CloseIndexBracket
}
struct SystemToken : ISimpleToken
{
    public SystemTokenType TokenType;
    public override string ToString()
    {
        return TokenType switch
        {
            SystemTokenType.Dot => ".",
            _ => TokenType.ToString()
        };
    }
}
struct OperatorToken : ISimpleToken
{
    public OperatorTokenType TokenType;
    public override string ToString()
    {
        return TokenType switch
        {
            OperatorTokenType.Plus => "+",
            OperatorTokenType.Minus => "-",
            OperatorTokenType.Times => "*",
            OperatorTokenType.Divide => "/",
            OperatorTokenType.Mod => "%",
            OperatorTokenType.Power => "^",
            OperatorTokenType.Range => "..",
            OperatorTokenType.Assign => "=",
            _ => TokenType.ToString()
        };
    }
}
struct BracketToken : ISimpleToken
{
    public BracketTokenType TokenType;
    public override string ToString()
    {
        return TokenType switch
        {
            BracketTokenType.OpenBracket => "(",
            BracketTokenType.CloseBracket => ")",
            BracketTokenType.OpenIndexBracket => "[",
            BracketTokenType.CloseIndexBracket => "]",
            _ => TokenType.ToString()
        };
    }
}
struct CommaToken : ISimpleToken
{
    public override string ToString()
    {
        return ",";
    }
}
struct VariableNameReferenceToken : ISimpleToken, IValueToken
{
    public string Text;
    public Environment Environment;
    public IValueToken SetValue(IValueToken Value)
    {
        Environment.Values[Text] = Value;
        return Value;
    }
    public override string ToString()
    {
        return $"VariableRef({Text})";
    }
}
struct NameToken : ISimpleToken, IValueToken
{
    public string Text;
    public Environment Environment;
    public IValueToken GetValue()
    {
        return Environment.GetValue(Text);
    }
    public IFunction GetFunction()
    {
        return Environment.GetFunction(Text);
    }
    public override string ToString()
    {
        return $"var({Text})";
    }
}
struct ErrorToken : IToken, ISimpleToken, IValueToken, IFunction
{
    public string Message;

    public IValueToken Invoke(IList<IValueToken> Parameters)
    {
        return this;
    }

    public override string ToString()
    {
        return Message;
    }
}
static partial class Extension
{
    public static IValueToken Evaluate(this IValueToken ValueToken)
    {
        if (ValueToken is ErrorToken) return ValueToken;
        if (ValueToken is NothingToken) return ValueToken;
        if (ValueToken is VariableNameReferenceToken) return ValueToken;
        if (ValueToken is GrouppedToken gt)
        {
            if (gt.Tokens.Count == 1)
            {
                if (gt.Tokens[0] is IValueToken valueToken)
                {
//                    if (valueToken is ParsedFunctionToken func)
//                        return func.Evaluate();
//                    if (valueToken is ParsedOperatorToken oper)
//                        return oper.Evaluate();
//                    if (valueToken is not (INumberValueToken or IMatValueToken))
//                    {
//#if DEBUG
//                        Debugger.Break();
//#endif
//                        return new ErrorToken {
//                            Message = $"Value & Internal Error: {valueToken} is recognized as value but not in a specified range of value. {MathParser.ErrorReport}"
//                        };
//                    }
                    return valueToken.Evaluate();
                }
#if DEBUG
                Debugger.Break();
#endif
                return new ErrorToken
                {
                    Message = $"Value & Internal Error: {gt.Tokens[0]} is not recognizeable {MathParser.ErrorReport}"
                };
            }
            else
            {
                if (gt.Tokens.Any(x => x is CommaToken))
                {
                    return new ErrorToken
                    {
                        Message = $"Syntax Error: The content inside the bracket is more than one value."
                    };
                }
#if DEBUG
                Debugger.Break();
#endif
                return new ErrorToken
                {
                    Message = $"Value & Internal Error: The content inside the bracket is more than one value. {MathParser.ErrorReport}"
                };
            }
        }
        else if (ValueToken is NameToken nt)
        {
            var value = nt.GetValue();
            if (value is ErrorToken)
                return value;
            if (value is ParsedFunctionToken func)
                return func.Evaluate();
            if (value is ParsedOperatorToken oper)
                return oper.Evaluate();
            if (value is not (INumberValueToken or IMatValueToken))
            {
#if DEBUG
                Debugger.Break();
#endif
                return new ErrorToken
                {
                    Message = $"Value & Internal Error: {ValueToken} is recognized as value but not in a specified range of value. {MathParser.ErrorReport}"
                };
            }
            return value;
        }
        else if (ValueToken is ParsedOperatorToken pot)
        {
            return pot.Evaluate();
        }
        else if (ValueToken is ParsedFunctionToken pft)
        {
            return pft.Evaluate();
        }
        //if (ValueToken is ParsedFunctionToken func1)
        //    return func1.Evaluate();
        //if (ValueToken is ParsedOperatorToken oper1)
        //    return oper1.Evaluate();
        else if (ValueToken is not (INumberValueToken or IMatValueToken))
        {
#if DEBUG
            Debugger.Break();
#endif
            return new ErrorToken
            {
                Message = $"Value & Internal Error: {ValueToken} is recognized as value but not in a specified range of value. {MathParser.ErrorReport}"
            };
        }
        return ValueToken;
    }
    public static IValueToken? GetImplicitFirstArument(this OperatorTokenType tt)
    {
        return tt switch
        {
            OperatorTokenType.Plus => new NumberToken { Number = 0 },
            OperatorTokenType.Minus => new NumberToken { Number = 0 },
            OperatorTokenType.Power => new NothingToken(),
            OperatorTokenType.Range => new NothingToken(),
            _ => null
        };
    }
    public static IValueToken? GetImplicitSecondArument(this OperatorTokenType tt)
    {
        return tt switch
        {
            OperatorTokenType.Range => new NothingToken(),
            _ => null
        };
    }
}
struct ParsedOperatorToken : IValueToken, IOperableToken
{
    public OperatorToken Operator;
    public IValueToken Value1;
    public IValueToken Value2;
    public IValueToken Evaluate() => Operator.TokenType switch
    {
        OperatorTokenType.Plus => Add.Run(Value1.Evaluate(), Value2.Evaluate()),
        OperatorTokenType.Minus => Subtract.Run(Value1.Evaluate(), Value2.Evaluate()),
        OperatorTokenType.Times => Multiply.Run(Value1.Evaluate(), Value2.Evaluate()),
        OperatorTokenType.Divide => Divide.Run(Value1.Evaluate(), Value2.Evaluate()),
        OperatorTokenType.Power => Power.Run(Value1.Evaluate(), Value2.Evaluate()),
        OperatorTokenType.Mod => Modulo.Run(Value1.Evaluate(), Value2.Evaluate()),
        OperatorTokenType.Range => Range.Run(Value1.Evaluate(), Value2.Evaluate()),
        OperatorTokenType.Assign => Assign.Run(Value1.Evaluate(), Value2.Evaluate()),
        _ => throw new ArgumentOutOfRangeException(nameof(Operator))
    };
    public override string ToString()
    {
        return $"({Value1} {Operator} {Value2})";
    }
}
struct ParsedFunctionToken : IValueToken, IOperableToken
{
    public NameToken FunctionName;
    public IValueToken[] Parameters;
    public override string ToString()
    {
        return $"{FunctionName.Text}({string.Join(", ", Parameters.AsEnumerable())})";
    }
    public IValueToken Evaluate()
    {
        return FunctionName.GetFunction().Invoke((from param in Parameters select param.Evaluate()).ToArray());
    }
}
struct GrouppedToken : ICompoundToken, IValueToken
{
    public bool HasComma => Tokens.Any(x => x is CommaToken);
    public bool IsEmpty => Tokens.Count == 0;
    public IList<IToken> Tokens;


    public override string ToString()
    {
        return Tokens.Count switch
        {
            > 1 => $"({string.Join(' ', Tokens)})",
            1 => string.Join(' ', Tokens),
            0 => "()",
            _ => throw new IndexOutOfRangeException()
        };
    }
}
struct IndexGrouppedToken : ICompoundToken
{
    //public IEnumerable<IToken> Tokens;
    //public override string ToString()
    //{
    //    return $"[{string.Join(' ', Tokens)}]";
    //}
}
struct NumberToken : ISimpleToken, INumberValueToken
{
    public double Number { get; set; }
    public override string ToString()
    {
        return Number.ToString();
    }
}
struct NothingToken : ISimpleToken, INothingValueToken
{
    public override string ToString()
    {
        return "<nothing>";
    }
}
interface IOperation : IToken
{

}
class Operation : IOperation
{

}
struct UnpharsedOperation : IOperation
{
    public UnpharsedOperation(string Text) => this.Text = Text;
    public string Text { get; set; }
}
enum MatType : byte
{
    Matrix, UnknownImage,
    BGR, BGRA,
    RGB, RGBA,
    HSV, HSVA,
    Gray, GrayA,
    Mask
}
struct MatrixMatToken : IMatValueToken
{
    public MatType Type => MatType.Matrix;
    public Mat Mat { get; set; }
    public override string ToString()
    {
        return FormatToString(Mat);
    }
    public static string FormatToString(Mat Mat, string MatType = "Mat")
    {
        return $@"[{Mat.Channels()}-channel {MatType} {Mat.Rows}x{Mat.Cols} ({(
            Mat.IsCompatableImage() ? "Image" :
            (Mat.IsCompatableNumberMatrix() ? "Matrix" : "Unknown")
        )})]";
    }
    public void Dispose() => Mat.Dispose();
}
interface IImageMatToken : IMatValueToken
{
    MatType AlphaType { get; }
    MatType NoAlphaType { get; }
    bool HasAlpha { get; }
    Mat GetBGRImage();
    Mat GetBGRAImage();
}
struct BGRImageMatToken : IImageMatToken
{
    public MatType Type => MatType.BGR;
    public MatType AlphaType => MatType.BGRA;
    public MatType NoAlphaType => MatType.BGR;
    public bool HasAlpha => false;
    public Mat Mat { get; set; }
    public override string ToString()
    {
        return MatrixMatToken.FormatToString(Mat, "BGRImage Mat");
    }
    public Mat GetBGRImage() => Mat;
    public Mat GetBGRAImage() => Mat.CvtColor(ColorConversionCodes.BGR2BGRA);
    public void Dispose() => Mat.Dispose();
}
struct UnknownImageMatToken : IImageMatToken
{
    public MatType Type => MatType.UnknownImage;
    public MatType AlphaType => MatType.UnknownImage;
    public MatType NoAlphaType => MatType.UnknownImage;
    public bool HasAlpha => false;
    public Mat Mat { get; set; }
    public override string ToString()
    {
        return MatrixMatToken.FormatToString(Mat, "BGRImage Mat");
    }
    public Mat GetBGRImage() => Mat;
    public Mat GetBGRAImage() => Mat;
    public void Dispose() => Mat.Dispose();
}
struct BGRAImageMatToken : IImageMatToken
{
    public MatType Type => MatType.BGRA;
    public MatType AlphaType => MatType.BGRA;
    public MatType NoAlphaType => MatType.BGR;
    public bool HasAlpha => true;
    public Mat Mat { get; set; }
    public override string ToString()
    {
        return MatrixMatToken.FormatToString(Mat, "BGRAImage Mat");
    }
    public Mat GetBGRImage() => Mat.CvtColor(ColorConversionCodes.BGRA2BGR);
    public Mat GetBGRAImage() => Mat;
    public void Dispose() => Mat.Dispose();
}
struct HSVImageMatToken : IImageMatToken
{
    public MatType Type => MatType.HSV;
    public MatType AlphaType => MatType.HSVA;
    public MatType NoAlphaType => MatType.HSV;
    public bool HasAlpha => false;
    public Mat Mat { get; set; }
    public override string ToString()
    {
        return MatrixMatToken.FormatToString(Mat, "HSVImage Mat");
    }
    public Mat GetBGRImage() => Mat.CvtColor(ColorConversionCodes.HSV2BGR);
    public Mat GetBGRAImage() => GetBGRImage().CvtColor(ColorConversionCodes.BGR2BGRA);
    public void Dispose() => Mat.Dispose();
}
struct HSVAImageMatToken : IImageMatToken
{
    public MatType Type => MatType.HSVA;
    public MatType AlphaType => MatType.HSVA;
    public MatType NoAlphaType => MatType.HSV;
    public bool HasAlpha => false;
    public Mat Mat { get; set; }
    public override string ToString()
    {
        return MatrixMatToken.FormatToString(Mat, "HSVAImage Mat");
    }
    public Mat GetBGRImage() => Mat.CvtColor(ColorConversionCodes.HSV2BGR);
    public Mat GetBGRAImage() => GetBGRImage().CvtColor(ColorConversionCodes.BGR2BGRA);
    public void Dispose() => Mat.Dispose();
}
struct RGBImageMatToken : IImageMatToken
{
    public MatType Type => MatType.RGB;
    public MatType AlphaType => MatType.RGBA;
    public MatType NoAlphaType => MatType.RGB;
    public bool HasAlpha => false;
    public Mat Mat { get; set; }
    public override string ToString()
    {
        return MatrixMatToken.FormatToString(Mat, "RGBImage Mat");
    }
    public Mat GetBGRImage() => Mat.CvtColor(ColorConversionCodes.RGB2BGR);
    public Mat GetBGRAImage() => Mat.CvtColor(ColorConversionCodes.RGB2BGRA);
    public void Dispose() => Mat.Dispose();
}
struct RGBAImageMatToken : IImageMatToken
{
    public MatType Type => MatType.RGBA;
    public MatType AlphaType => MatType.RGBA;
    public MatType NoAlphaType => MatType.RGB;
    public bool HasAlpha => true;
    public Mat Mat { get; set; }
    public override string ToString()
    {
        return MatrixMatToken.FormatToString(Mat, "RGBAImage Mat");
    }
    public Mat GetBGRImage() => Mat.CvtColor(ColorConversionCodes.RGB2BGR);
    public Mat GetBGRAImage() => Mat.CvtColor(ColorConversionCodes.RGB2BGRA);
    public void Dispose() => Mat.Dispose();
}
interface IGrayImageMatToken : IImageMatToken
{

}
struct GrayImageMatToken : IGrayImageMatToken
{
    public MatType Type => MatType.Gray;
    public MatType AlphaType => MatType.GrayA;
    public MatType NoAlphaType => MatType.Gray;
    public bool HasAlpha => false;
    public Mat Mat { get; set; }
    public override string ToString()
    {
        return MatrixMatToken.FormatToString(Mat, "GrayImage Mat");
    }
    public Mat GetBGRImage() => Mat.CvtColor(ColorConversionCodes.GRAY2BGR);
    public Mat GetBGRAImage() => Mat.CvtColor(ColorConversionCodes.GRAY2BGRA);
    public void Dispose() => Mat.Dispose();
}
struct MaskImageMatToken : IGrayImageMatToken
{
    public MatType Type => MatType.Mask;
    public MatType AlphaType => MatType.GrayA;
    public MatType NoAlphaType => MatType.Mask;
    public bool HasAlpha => false;
    public Mat Mat { get; set; }
    public override string ToString()
    {
        return MatrixMatToken.FormatToString(Mat, "MaskImage Mat");
    }
    public Mat GetBGRImage() => Mat.CvtColor(ColorConversionCodes.GRAY2BGR);
    public Mat GetBGRAImage() => Mat.CvtColor(ColorConversionCodes.GRAY2BGRA);
    public void Dispose() => Mat.Dispose();
}
struct GrayAImageMatToken : IImageMatToken
{
    public MatType Type => MatType.Gray;
    public MatType AlphaType => MatType.GrayA;
    public MatType NoAlphaType => MatType.Gray;
    public bool HasAlpha => false;
    public Mat Mat { get; set; }
    public override string ToString()
    {
        return MatrixMatToken.FormatToString(Mat, "GrayAlphaImage Mat");
    }
    public Mat GetBGRImage() =>
        Mat
        .ExtractChannel(0)
        .InplaceCvtColor(ColorConversionCodes.GRAY2BGR);
    public Mat GetBGRAImage() =>
        Mat
        .ExtractChannel(0)
        .InplaceInsertAlpha(
            Mat.ExtractChannel(1).Track(out var tracker)
        )
        .DisposeTracker(tracker);
    public void Dispose() => Mat.Dispose();
}
struct RangeToken : IRangeValueToken
{
    public System.Range Range { get; set; }
    public override string ToString()
    {
        return $"{Range.Start}..{Range.End}";
    }
}
interface IFunction
{
    IValueToken Invoke(IList<IValueToken> Parameters);
}
interface IOperator
{
    IValueToken Invoke(IValueToken Item1, IValueToken Item2);
}
