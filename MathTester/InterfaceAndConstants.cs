using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenCvSharp;
using PhotoToys;
namespace MathExpressionParser;

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
}
interface IMatValueToken : IValueToken, INativeToken
{
    Mat Mat { get; set; }
}
interface IOperableToken : IToken
{

}
interface ICompoundToken : IToken
{

}
public enum SystemTokenType
{
    Dot,
    Range
}
public enum OperatorTokenType
{
    Power,
    Times,
    Divide,
    Mod,
    Plus,
    Minus,
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
            SystemTokenType.Range => "..",
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
        if (ValueToken is GrouppedToken gt)
        {
            if (gt.Tokens.Count == 1)
            {
                if (gt.Tokens[0] is IValueToken valueToken)
                {
                    if (valueToken is ParsedFunctionToken func)
                        return func.Evaluate();
                    if (valueToken is ParsedOperatorToken oper)
                        return oper.Evaluate();
                    if (valueToken is not (INumberValueToken or IMatValueToken))
                    {
#if DEBUG
                        Debugger.Break();
#endif
                        return new ErrorToken {
                            Message = $"Value & Internal Error: {valueToken} is recognized as value but not in a specified range of value. {MathParser.ErrorReport}"
                        };
                    }
                    return valueToken;
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
        } else if (ValueToken is ParsedOperatorToken pot)
        {
            return pot.Evaluate();
        }
        else if (ValueToken is ParsedFunctionToken pft)
        {
            return pft.Evaluate();
        }
        if (ValueToken is ParsedFunctionToken func1)
            return func1.Evaluate();
        if (ValueToken is ParsedOperatorToken oper1)
            return oper1.Evaluate();
        if (ValueToken is not (INumberValueToken or IMatValueToken))
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
    public IEnumerable<IToken> Tokens;
    public override string ToString()
    {
        return $"[{string.Join(' ', Tokens)}]";
    }
}
struct NumberToken : ISimpleToken, INumberValueToken
{
    public double Number { get; set; }
    public override string ToString()
    {
        return Number.ToString();
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
struct MatToken : IMatValueToken
{
    public Mat Mat { get; set; }
    public override string ToString()
    {
        return Mat.ToString();
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
struct Add : IOperator
{
    public IValueToken Invoke(IValueToken Item1, IValueToken Item2) => Run(Item1, Item2);
    public static IValueToken Run(IValueToken Item1, IValueToken Item2)
    {
        if (Item1 is ErrorToken) return Item1;
        if (Item2 is ErrorToken) return Item2;
        if (Item1 is INumberValueToken Number1)
        {
            if (Item2 is INumberValueToken Number2)
                return new NumberToken { Number = Number1.Number + Number2.Number };
            else if (Item2 is IMatValueToken Mat2)
                return new MatToken { Mat = Number1.Number + Mat2.Mat };
            else return new ErrorToken
            {
                Message = $"Operator '+' accepts [Mat/Number]? + [Mat/Number]. However, the second argument received is '{Item2}' which is neither of those."
            };
        }
        if (Item1 is IMatValueToken Mat1)
        {
            if (Item2 is INumberValueToken Number2)
                return new MatToken { Mat = Mat1.Mat + Number2.Number };
            else if (Item2 is IMatValueToken Mat2)
                return new MatToken { Mat = Mat1.Mat + Mat2.Mat };
            else return new ErrorToken
            {
                Message = $"Operator '+' accepts [Mat/Number]? + [Mat/Number]. However, the second argument received is '{Item2}' which is neither of those."
            };
        }
        return new ErrorToken
        {
            Message = $"Operator '+' accepts [Mat/Number]? + [Mat/Number]. However, the first argument received is '{Item1}' which is neither of those."
        };
    }
}
struct Subtract : IOperator
{
    public IValueToken Invoke(IValueToken Item1, IValueToken Item2) => Run(Item1, Item2);
    public static IValueToken Run(IValueToken Item1, IValueToken Item2)
    {
        if (Item1 is ErrorToken) return Item1;
        if (Item2 is ErrorToken) return Item2;
        if (Item1 is INumberValueToken Number1)
        {
            if (Item2 is INumberValueToken Number2)
                return new NumberToken { Number = Number1.Number - Number2.Number };
            else if (Item2 is IMatValueToken Mat2)
                return new MatToken { Mat = Number1.Number - Mat2.Mat };
            else return new ErrorToken
            {
                Message = $"Operator '-' accepts [Mat/Number]? - [Mat/Number]. However, the second argument received is '{Item2}' which is neither of those."
            };
        }
        if (Item1 is IMatValueToken Mat1)
        {
            if (Item2 is INumberValueToken Number2)
                return new MatToken { Mat = Mat1.Mat - Number2.Number };
            else if (Item2 is IMatValueToken Mat2)
                return new MatToken { Mat = Mat1.Mat - Mat2.Mat };
            else return new ErrorToken
            {
                Message = $"Operator '-' accepts [Mat/Number]? - [Mat/Number]. However, the second argument received is '{Item2}' which is neither of those."
            };
        }
        return new ErrorToken
        {
            Message = $"Operator '-' accepts [Mat/Number]? - [Mat/Number]. However, the first argument received is '{Item1}' which is neither of those."
        };
    }
}
struct Multiply : IOperator
{
    public IValueToken Invoke(IValueToken Item1, IValueToken Item2) => Run(Item1, Item2);
    public static IValueToken Run(IValueToken Item1, IValueToken Item2)
    {
        if (Item1 is ErrorToken) return Item1;
        if (Item2 is ErrorToken) return Item2;
        if (Item1 is INumberValueToken Number1)
        {
            if (Item2 is INumberValueToken Number2)
                return new NumberToken { Number = Number1.Number * Number2.Number };
            else if (Item2 is IMatValueToken Mat2)
                return new MatToken { Mat = Number1.Number * Mat2.Mat };
            else return new ErrorToken
            {
                Message = $"Operator '*' accepts [Mat/Number] * [Mat/Number]. However, the second argument received is '{Item2}' which is neither of those."
            };
        }
        if (Item1 is IMatValueToken Mat1)
        {
            if (Item2 is INumberValueToken Number2)
                return new MatToken { Mat = Mat1.Mat * Number2.Number };
            else if (Item2 is IMatValueToken Mat2)
                return new MatToken { Mat = Mat1.Mat.Mul(Mat2.Mat) };
            else return new ErrorToken
            {
                Message = $"Operator '*' accepts [Mat/Number] * [Mat/Number]. However, the second argument received is '{Item2}' which is neither of those."
            };
        }
        return new ErrorToken
        {
            Message = $"Operator '*' accepts [Mat/Number] * [Mat/Number]. However, the first argument received is '{Item1}' which is neither of those."
        };
    }
}
struct Divide : IOperator
{
    public IValueToken Invoke(IValueToken Item1, IValueToken Item2) => Run(Item1, Item2);
    public static IValueToken Run(IValueToken Item1, IValueToken Item2)
    {
        if (Item1 is ErrorToken) return Item1;
        if (Item2 is ErrorToken) return Item2;
        if (Item1 is INumberValueToken Number1)
        {
            if (Item2 is INumberValueToken Number2)
                return new NumberToken { Number = Number1.Number / Number2.Number };
            else if (Item2 is IMatValueToken Mat2)
                return new MatToken { Mat = Number1.Number / Mat2.Mat };
            else return new ErrorToken
            {
                Message = $"Operator '/' accepts [Mat/Number] / [Mat/Number]. However, the second argument received is '{Item2}' which is neither of those."
            };
        }
        if (Item1 is IMatValueToken Mat1)
        {
            if (Item2 is INumberValueToken Number2)
                return new MatToken { Mat = Mat1.Mat / Number2.Number };
            else if (Item2 is IMatValueToken Mat2)
                return new MatToken { Mat = Mat1.Mat / Mat2.Mat };
            else return new ErrorToken
            {
                Message = $"Operator '/' accepts [Mat/Number] / [Mat/Number]. However, the second argument received is '{Item2}' which is neither of those."
            };
        }
        return new ErrorToken
        {
            Message = $"Operator '/' accepts [Mat/Number] / [Mat/Number]. However, the first argument received is '{Item1}' which is neither of those."
        };
    }
}
struct Modulo : IOperator
{
    public IValueToken Invoke(IValueToken Item1, IValueToken Item2) => Run(Item1, Item2);
    public static IValueToken Run(IValueToken Item1, IValueToken Item2)
    {
        if (Item1 is ErrorToken) return Item1;
        if (Item2 is ErrorToken) return Item2;
        if (Item1 is INumberValueToken Number1 && Item2 is INumberValueToken Number2)
            return new NumberToken { Number = Number1.Number % Number2.Number };
        return new ErrorToken
        {
            Message = $"Operator '%' accepts [Number] % [Number]. However, the argument received is {Item1} and {Item2}'"
        };
    }
}
struct Power : IOperator
{
    public IValueToken Invoke(IValueToken Item1, IValueToken Item2) => Run(Item1, Item2);
    public static IValueToken Run(IValueToken Item1, IValueToken Item2)
    {
        if (Item1 is ErrorToken) return Item1;
        if (Item2 is ErrorToken) return Item2;
        if (Item1 is INumberValueToken Number1)
        {
            if (Item2 is INumberValueToken Number2)
                return new NumberToken { Number = Math.Pow(Number1.Number, Number2.Number) };
            else return new ErrorToken
            {
                Message = $"Type Error: Operator '**' or '^' accepts [Number/Mat] ^ [Number]. However, the second argument received is '{Item2}'"
            };
        }
        if (Item1 is IMatValueToken Mat1)
        {
            if (Item2 is INumberValueToken Number2)
                return new MatToken { Mat = Mat1.Mat.Pow(Number2.Number) };
            else return new ErrorToken
            {
                Message = $"Type Error: Operator '**' or '^' accepts [Number/Mat] ^ [Number]. However, the second argument received is '{Item2}'"
            };
        }
        return new ErrorToken
        {
            Message = $"Type Error: Operator '**' or '^' accepts [Number/Mat] ^ [Number]. However, the first argument received is '{Item1}'"
        };
    }
}
struct Abs : IFunction
{
    public IValueToken Invoke(IList<IValueToken> Parameters)
    {
        if (Parameters.FirstOrDefault(a => a is ErrorToken, null) is ErrorToken et) return et;
        if (Parameters.Count != 1) return new ErrorToken
        {
            Message = $"Parameter Error: Function abs([Number] x) accept 1 paramter but {Parameters.Count} was/were given"
        };
        var x = Parameters[0];
        if (x is INumberValueToken Number)
            return new NumberToken { Number = Math.Abs(Number.Number) };
        else if (x is IMatValueToken Mat)
            return new MatToken { Mat = Mat.Mat.Abs() };
        else return new ErrorToken
        {
            Message = $"Type Error: Function abs([Number] x), x '{x}' should be [Number/Mat]"
        };
    }
}
struct Clamp : IFunction
{
    public IValueToken Invoke(IList<IValueToken> Parameters)
    {
        const string OverloadNumber = "Function clamp([Number] x, [Number] lowerBound, [Number] upperBound)";
        const string OverloadMatNumber = "Function clamp([Number] x, [Number] lowerBound, [Number] upperBound)";
        const string OverloadMatMat = "Function clamp([Number] x, [Number] lowerBound, [Number] upperBound)";
        if (Parameters.FirstOrDefault(a => a is ErrorToken, null) is ErrorToken et) return et;
        if (Parameters.Count != 3) return new ErrorToken
        {
            Message = $"Parameter Error: Function {OverloadNumber} | {OverloadMatNumber} | {OverloadMatMat}" +
            $"accept 3 paramters but {Parameters.Count} was/were given"
        };
        var x = Parameters[0];
        var lowerBound = Parameters[1];
        var upperBound = Parameters[2];
        if (x is INumberValueToken Number)
        {
            if (lowerBound is not INumberValueToken Lower) return new ErrorToken
            {
                Message = $"Type Error: Function {OverloadNumber}, {nameof(lowerBound)} '{lowerBound}' should be a number"
            };
            if (upperBound is not INumberValueToken Upper) return new ErrorToken
            {
                Message = $"Type Error: Function {OverloadNumber}, {nameof(upperBound)} '{upperBound}' should be a number'"
            };
            return new NumberToken { Number = Math.Clamp(Number.Number, Lower.Number, Upper.Number) };
        }
        else if (x is IMatValueToken Mat)
        {
            if (lowerBound is INumberValueToken Lower && upperBound is INumberValueToken Upper)
            {
                var ds = new ResourcesTracker();
                var mat = Mat.Mat;
                mat.SetTo(Lower.Number, mat.LessThan(Lower.Number).Track(ds));
                mat.SetTo(Upper.Number, mat.GreaterThan(Upper.Number).Track(ds));
                return new MatToken { Mat = mat };
            }
            if (lowerBound is IMatValueToken LowerMat && upperBound is IMatValueToken UpperMat)
            {
                var ds = new ResourcesTracker();
                var mat = Mat.Mat;
                var mask1 = mat.LessThan(LowerMat.Mat).Track(ds);
                var mask2 = mat.GreaterThan(UpperMat.Mat).Track(ds);
                Cv2.CopyTo(LowerMat.Mat, mat, mask1);
                Cv2.CopyTo(UpperMat.Mat, mat, mask2);
                return new MatToken { Mat = mat };
            }
            return new ErrorToken
            {
                Message = $"Type Error: {OverloadMatNumber} | {OverloadMatMat}, " +
                    $"{nameof(lowerBound)} '{lowerBound}' and {nameof(upperBound)} '{upperBound}' does not match any of the given overload"
            };
        }
        else return new ErrorToken
        {
            Message = $"Type Error: Function {OverloadNumber} | {OverloadMatNumber} | {OverloadMatMat}," +
            $"{nameof(x)} '{x}', {nameof(lowerBound)} '{lowerBound}', and {nameof(upperBound)} '{upperBound}' does not match any of the given overload"
        };
    }
}
struct Min : IFunction
{
    public IValueToken Invoke(IList<IValueToken> Parameters)
    {
        const string OverloadNumber = "min(params [Number] Xs)";
        const string OverloadOneMat = "max([Mat] x)";
        const string OverloadMultipleMat = "min(params [Mat] Xs)";
        if (Parameters.FirstOrDefault(x => x is ErrorToken, null) is ErrorToken et) return et;
        if (Parameters.Count == 0) return new ErrorToken
        {
            Message = $"Parameter Error: Function {OverloadNumber} | {OverloadOneMat} | {OverloadMultipleMat}, accepts 1 or more argument but {Parameters.Count} were given"
        };
        if (Parameters.Count == 1)
        {
            if (Parameters[0] is IMatValueToken MatToken)
            {
                Cv2.MinMaxLoc(MatToken.Mat, out double min, out _);
                return new NumberToken { Number = min };
            }
            return Parameters[0];
        }
        var enumerator = Parameters.GetEnumerator();
        enumerator.MoveNext();
        if (enumerator.Current is INumberValueToken token)
        {
            var min = token.Number;
            while (enumerator.MoveNext())
            {
                if (enumerator.Current is not INumberValueToken val) return new ErrorToken
                {
                    Message = $"Type Error: Function {OverloadNumber}, '{enumerator.Current}' should be Number"
                };
                if (min < val.Number) min = val.Number;
            }
            return new NumberToken { Number = min };
        }
        if (enumerator.Current is IMatValueToken mattoken)
        {
            var tracker = new ResourcesTracker();
            var min = mattoken.Mat.Clone().Track(tracker);
            while (enumerator.MoveNext())
            {
                if (enumerator.Current is not IMatValueToken val) return new ErrorToken
                {
                    Message = $"Type Error: Function {OverloadMultipleMat}, '{enumerator.Current}' should be Mat"
                };
                Cv2.Min(min, val.Mat, min);
            }
            return new MatToken { Mat = min };
        }
        return new ErrorToken
        {
            Message = $"Type Error: Function {OverloadNumber} | {OverloadOneMat} | {OverloadMultipleMat}, cannot find a matching overload for first parameter {Parameters[0]}"
        };
    }
}
struct Max : IFunction
{
    public IValueToken Invoke(IList<IValueToken> Parameters)
    {
        const string OverloadNumber = "max(params [Number] Xs)";
        const string OverloadOneMat = "max([Mat] x)";
        const string OverloadMultipleMat = "max(params [Mat] Xs)";
        if (Parameters.FirstOrDefault(x => x is ErrorToken, null) is ErrorToken et) return et;
        if (Parameters.Count == 0) return new ErrorToken
        {
            Message = $"Parameter Error: Function {OverloadNumber} | {OverloadOneMat} | {OverloadMultipleMat}, accepts 1 or more argument but {Parameters.Count} were given"
        };
        if (Parameters.Count == 1)
        {
            if (Parameters[0] is IMatValueToken MatToken)
            {
                Cv2.MinMaxLoc(MatToken.Mat, out _, out double max);
                return new NumberToken { Number = max };
            }

            return Parameters[0];
        }
        var enumerator = Parameters.GetEnumerator();
        enumerator.MoveNext();
        if (enumerator.Current is INumberValueToken token)
        {
            var max = token.Number;
            while (enumerator.MoveNext())
            {
                if (enumerator.Current is not INumberValueToken val) return new ErrorToken
                {
                    Message = $"Type Error: Function {OverloadNumber}, '{enumerator.Current}' should be Number"
                };
                if (max < val.Number) max = val.Number;
            }
            return new NumberToken { Number = max };
        }
        if (enumerator.Current is IMatValueToken mattoken)
        {
            var tracker = new ResourcesTracker();
            var max = mattoken.Mat.Clone().Track(tracker);
            while (enumerator.MoveNext())
            {
                if (enumerator.Current is not IMatValueToken val) return new ErrorToken
                {
                    Message = $"Type Error: Function {OverloadMultipleMat}, '{enumerator.Current}' should be Mat"
                };
                Cv2.Max(max, val.Mat, max);
            }
            return new MatToken { Mat = max };
        }
        return new ErrorToken
        {
            Message = $"Type Error: Function {OverloadNumber} | {OverloadOneMat} | {OverloadMultipleMat}, cannot find a matching overload for first parameter {Parameters[0]}"
        };
    }
}
class Environment
{
    public Environment()
    {

    }
    public Dictionary<string, IValueToken> Values { get; } = new();
    public Dictionary<string, IFunction> Functions { get; } = new();
    public IValueToken GetValue(string Name)
    {
        return Values.GetValueOrDefault(Name, new ErrorToken { Message = $"The name '{Name}' is not a valid value" });
    }
    public IFunction GetFunction(string Name)
    {
        return Name.ToLower() switch
        {
            "abs" => new Abs(),
            "clamp" => new Clamp(),
            "min" => new Min(),
            "max" => new Max(),
            _ => Functions.GetValueOrDefault(Name, new ErrorToken { Message = $"The name '{Name}' is not a valid function" })
        };
    }
}