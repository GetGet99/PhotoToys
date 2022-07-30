using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenCvSharp;
using Range = System.Range;

namespace PTMS;

public interface IToken
{

}
public interface ISimpleToken : IToken
{

}
public interface IValueToken : IToken
{

}
public interface INativeToken : IToken
{

}
public interface INumberValueToken : IValueToken, INativeToken
{
    double Number { get; set; }
    int NumberAsInt => (int)Math.Round(Number);
}
public interface IMatValueToken : IValueToken, INativeToken, IDisposable
{
    MatrixType Type { get; }
    Mat Mat { get; set; }
}
public interface IRangeValueToken : IValueToken, INativeToken
{
    System.Range Range { get; set; }
}
public interface INothingValueToken : IValueToken, INativeToken
{

}
public interface IBooleanToken : IValueToken, INativeToken
{
    bool Value { get; }
}
public struct BooleanToken : IBooleanToken {
    public bool Value { get; set; }
    public static BooleanToken True => new BooleanToken { Value = true };
    public static BooleanToken False => new BooleanToken { Value = false };
    public override string ToString() => Value.ToString();
}
public interface IOperableToken : IToken
{

}
public interface ICompoundToken : IToken
{

}
public enum SystemTokenType : byte
{
    Dot
}
public enum OperatorTokenType : byte
{
    Power,
    Times,
    Divide,
    Mod,
    Plus,
    Minus,
    Equal,
    NotEqual,
    GreaterThan,
    GreaterThanOrEqual,
    LessThan,
    LessThanOrEqual,
    Range,
    Assign
}
public enum BracketTokenType : byte
{
    OpenBracket,
    CloseBracket,
    OpenIndexBracket,
    CloseIndexBracket
}
public struct SystemToken : ISimpleToken
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
public struct OperatorToken : ISimpleToken
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
public struct ErrorToken : IToken, ISimpleToken, IValueToken, IFunction
{
    public bool EvaluateValue => false;
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
public static partial class Extension
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
                    Message = $"Value & Internal Error: {gt.Tokens[0]} is not recognizeable {PTMSParser.ErrorReport}"
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
                    Message = $"Value & Internal Error: The content inside the bracket is more than one value. {PTMSParser.ErrorReport}"
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
            if (value is not (INumberValueToken or IMatValueToken or IBooleanToken))
            {
#if DEBUG
                Debugger.Break();
#endif
                return new ErrorToken
                {
                    Message = $"Value & Internal Error: {ValueToken} is recognized as value but not in a specified range of value. {PTMSParser.ErrorReport}"
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
        else if (ValueToken is not (INumberValueToken or IMatValueToken or IBooleanToken))
        {
#if DEBUG
            Debugger.Break();
#endif
            return new ErrorToken
            {
                Message = $"Value & Internal Error: {ValueToken} is recognized as value but not in a specified range of value. {PTMSParser.ErrorReport}"
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
public struct ParsedOperatorToken : IValueToken, IOperableToken
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
        OperatorTokenType.Equal => Equal.Run(Value1.Evaluate(), Value2.Evaluate()),
        OperatorTokenType.NotEqual => NotEqual.Run(Value1.Evaluate(), Value2.Evaluate()),
        OperatorTokenType.GreaterThan => GreaterThan.Run(Value1.Evaluate(), Value2.Evaluate()),
        OperatorTokenType.GreaterThanOrEqual => GreaterThanOrEqual.Run(Value1.Evaluate(), Value2.Evaluate()),
        OperatorTokenType.LessThan => LessThan.Run(Value1.Evaluate(), Value2.Evaluate()),
        OperatorTokenType.LessThanOrEqual => LessThanOrEqual.Run(Value1.Evaluate(), Value2.Evaluate()),
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
        var func = FunctionName.GetFunction();
        if (func.EvaluateValue)
            return func.Invoke((from param in Parameters select param.Evaluate()).ToArray());
        else
            return func.Invoke(Parameters);
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
public interface IOperation : IToken
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
public enum MatrixType : byte
{
    Matrix, UnknownImage,
    BGR, BGRA,
    RGB, RGBA,
    HSV, HSVA,
    Gray, GrayA,
    Mask
}
public struct MatrixMatToken : IMatValueToken
{
    public MatrixType Type => MatrixType.Matrix;
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
public interface IImageMatToken : IMatValueToken
{
    MatrixType AlphaType { get; }
    MatrixType NoAlphaType { get; }
    bool HasAlpha { get; }
    Mat GetBGRImage();
    Mat GetBGRAImage();
}
public struct BGRImageMatToken : IImageMatToken
{
    public MatrixType Type => MatrixType.BGR;
    public MatrixType AlphaType => MatrixType.BGRA;
    public MatrixType NoAlphaType => MatrixType.BGR;
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
public struct UnknownImageMatToken : IImageMatToken
{
    public MatrixType Type => MatrixType.UnknownImage;
    public MatrixType AlphaType => MatrixType.UnknownImage;
    public MatrixType NoAlphaType => MatrixType.UnknownImage;
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
public struct BGRAImageMatToken : IImageMatToken
{
    public MatrixType Type => MatrixType.BGRA;
    public MatrixType AlphaType => MatrixType.BGRA;
    public MatrixType NoAlphaType => MatrixType.BGR;
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
public struct HSVImageMatToken : IImageMatToken
{
    public MatrixType Type => MatrixType.HSV;
    public MatrixType AlphaType => MatrixType.HSVA;
    public MatrixType NoAlphaType => MatrixType.HSV;
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
public struct HSVAImageMatToken : IImageMatToken
{
    public MatrixType Type => MatrixType.HSVA;
    public MatrixType AlphaType => MatrixType.HSVA;
    public MatrixType NoAlphaType => MatrixType.HSV;
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
    public MatrixType Type => MatrixType.RGB;
    public MatrixType AlphaType => MatrixType.RGBA;
    public MatrixType NoAlphaType => MatrixType.RGB;
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
    public MatrixType Type => MatrixType.RGBA;
    public MatrixType AlphaType => MatrixType.RGBA;
    public MatrixType NoAlphaType => MatrixType.RGB;
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
public interface IGrayImageMatToken : IImageMatToken
{

}
struct GrayImageMatToken : IGrayImageMatToken
{
    public MatrixType Type => MatrixType.Gray;
    public MatrixType AlphaType => MatrixType.GrayA;
    public MatrixType NoAlphaType => MatrixType.Gray;
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
    public MatrixType Type => MatrixType.Mask;
    public MatrixType AlphaType => MatrixType.GrayA;
    public MatrixType NoAlphaType => MatrixType.Mask;
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
    public MatrixType Type => MatrixType.Gray;
    public MatrixType AlphaType => MatrixType.GrayA;
    public MatrixType NoAlphaType => MatrixType.Gray;
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
public struct RangeToken : IRangeValueToken
{
    public System.Range Range { get; set; }
    public override string ToString()
    {
        return $"{Range.Start}..{Range.End}";
    }
}
public interface IFunction
{
    bool EvaluateValue { get; }
    IValueToken Invoke(IList<IValueToken> Parameters);
}
public interface IOperator
{
    IValueToken Invoke(IValueToken Item1, IValueToken Item2);
}
