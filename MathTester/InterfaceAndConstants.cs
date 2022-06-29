using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenCvSharp;
using PhotoToys;
namespace MathTester;

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
    public Func<object, object>? Function;
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
    public IValueToken? GetValue()
    {
        return Environment.GetValue(Text);
    }
    public IFunction<IValueToken?>? GetFunction()
    {
        return Environment.GetFunction(Text);
    }
    public override string ToString()
    {
        return $"var({Text})";
    }
}
static partial class Extension
{
    public static IValueToken? Evaluate(this IValueToken? ValueToken)
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
                    if (valueToken is not (INumberValueToken or IMatValueToken or null))
                        Debugger.Break();
                    return valueToken;
                }
                Debugger.Break();
                return null;
            }
            else
            {
                Debugger.Break();
                return null;
            }
        }
        else if (ValueToken is NameToken nt)
        {
            var value = nt.GetValue();
            if (value is ParsedFunctionToken func)
                return func.Evaluate();
            if (value is ParsedOperatorToken oper)
                return oper.Evaluate();
            if (value is not (INumberValueToken or IMatValueToken or null))
                Debugger.Break();
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
        if (ValueToken is not (INumberValueToken or IMatValueToken or null))
            Debugger.Break();
        return ValueToken;
    }
}
struct ParsedOperatorToken : IValueToken, IOperableToken
{
    public OperatorToken Operator;
    public IValueToken Value1;
    public IValueToken Value2;
    public IValueToken? Evaluate() => Operator.TokenType switch
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
    public IValueToken? Evaluate()
    {
        if (FunctionName.GetFunction() is IFunction<IValueToken?> Function)
            return Function.Invoke((from param in Parameters select param.Evaluate()).ToArray());
        return null;
    }
}
struct GrouppedToken : ICompoundToken, IValueToken
{
    public bool HasComma => Tokens.Any(x => x is CommaToken);
    public bool IsEmpty => Tokens.Count == 0;
    public IList<IToken?> Tokens;


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
    public IEnumerable<IToken?> Tokens;
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
interface IFunction<T> where T : IValueToken?
{
    T Invoke(IList<IValueToken?> Parameters);
}
interface IOperator<T> where T : IValueToken?
{
    T Invoke(IValueToken? Item1, IValueToken? Item2);
}
struct Add : IOperator<IValueToken?>
{
    public IValueToken? Invoke(IValueToken? Item1, IValueToken? Item2) => Run(Item1, Item2);
    public static IValueToken? Run(IValueToken? Item1, IValueToken? Item2)
    {
        if (Item1 is INumberValueToken Number1)
        {
            if (Item2 is INumberValueToken Number2)
                return new NumberToken { Number = Number1.Number + Number2.Number };
            else if (Item2 is IMatValueToken Mat2)
                return new MatToken { Mat = Number1.Number + Mat2.Mat };
            else return null;
        }
        if (Item1 is IMatValueToken Mat1)
        {
            if (Item2 is INumberValueToken Number2)
                return new MatToken { Mat = Mat1.Mat + Number2.Number };
            else if (Item2 is IMatValueToken Mat2)
                return new MatToken { Mat = Mat1.Mat + Mat2.Mat };
            else return null;
        }
        return null;
    }
}
struct Subtract : IOperator<IValueToken?>
{
    public IValueToken? Invoke(IValueToken? Item1, IValueToken? Item2) => Run(Item1, Item2);
    public static IValueToken? Run(IValueToken? Item1, IValueToken? Item2)
    {
        if (Item1 is INumberValueToken Number1)
        {
            if (Item2 is INumberValueToken Number2)
                return new NumberToken { Number = Number1.Number - Number2.Number };
            else if (Item2 is IMatValueToken Mat2)
                return new MatToken { Mat = Number1.Number - Mat2.Mat };
            else return null;
        }
        if (Item1 is IMatValueToken Mat1)
        {
            if (Item2 is INumberValueToken Number2)
                return new MatToken { Mat = Mat1.Mat - Number2.Number };
            else if (Item2 is IMatValueToken Mat2)
                return new MatToken { Mat = Mat1.Mat - Mat2.Mat };
            else return null;
        }
        return null;
    }
}
struct Multiply : IOperator<IValueToken?>
{
    public IValueToken? Invoke(IValueToken? Item1, IValueToken? Item2) => Run(Item1, Item2);
    public static IValueToken? Run(IValueToken? Item1, IValueToken? Item2)
    {
        if (Item1 is INumberValueToken Number1)
        {
            if (Item2 is INumberValueToken Number2)
                return new NumberToken { Number = Number1.Number * Number2.Number };
            else if (Item2 is IMatValueToken Mat2)
                return new MatToken { Mat = Number1.Number * Mat2.Mat };
            else return null;
        }
        if (Item1 is IMatValueToken Mat1)
        {
            if (Item2 is INumberValueToken Number2)
                return new MatToken { Mat = Mat1.Mat * Number2.Number };
            else if (Item2 is IMatValueToken Mat2)
                return new MatToken { Mat = Mat1.Mat.Mul(Mat2.Mat) };
            else return null;
        }
        return null;
    }
}
struct Divide : IOperator<IValueToken?>
{
    public IValueToken? Invoke(IValueToken? Item1, IValueToken? Item2) => Run(Item1, Item2);
    public static IValueToken? Run(IValueToken? Item1, IValueToken? Item2)
    {
        if (Item1 is INumberValueToken Number1)
        {
            if (Item2 is INumberValueToken Number2)
                return new NumberToken { Number = Number1.Number / Number2.Number };
            else if (Item2 is IMatValueToken Mat2)
                return new MatToken { Mat = Number1.Number / Mat2.Mat };
            else return null;
        }
        if (Item1 is IMatValueToken Mat1)
        {
            if (Item2 is INumberValueToken Number2)
                return new MatToken { Mat = Mat1.Mat / Number2.Number };
            else if (Item2 is IMatValueToken Mat2)
                return new MatToken { Mat = Mat1.Mat / Mat2.Mat };
            else return null;
        }
        return null;
    }
}
struct Modulo : IOperator<IValueToken?>
{
    public IValueToken? Invoke(IValueToken? Item1, IValueToken? Item2) => Run(Item1, Item2);
    public static IValueToken? Run(IValueToken? Item1, IValueToken? Item2)
    {
        if (Item1 is INumberValueToken Number1 && Item2 is INumberValueToken Number2)
            return new NumberToken { Number = Number1.Number % Number2.Number };
        return null;
    }
}
struct Power : IOperator<IValueToken?>
{
    public IValueToken? Invoke(IValueToken? Item1, IValueToken? Item2) => Run(Item1, Item2);
    public static IValueToken? Run(IValueToken? Item1, IValueToken? Item2)
    {
        if (Item1 is INumberValueToken Number1)
        {
            if (Item2 is INumberValueToken Number2)
                return new NumberToken { Number = Math.Pow(Number1.Number, Number2.Number) };
            else return null;
        }
        if (Item1 is IMatValueToken Mat1)
        {
            if (Item2 is INumberValueToken Number2)
                return new MatToken { Mat = Mat1.Mat.Pow(Number2.Number) };
            else return null;
        }
        return null;
    }
}
struct Abs : IFunction<IValueToken?>
{
    public IValueToken? Invoke(IList<IValueToken?> Parameters)
    {
        if (Parameters.Count != 1) return null;
        var param = Parameters[0];
        if (param is INumberValueToken Number)
            return new NumberToken { Number = Math.Abs(Number.Number) };
        else if (param is IMatValueToken Mat)
            return new MatToken { Mat = Mat.Mat.Abs() };
        else return null;
    }
}
struct Clamp : IFunction<IValueToken?>
{
    public IValueToken? Invoke(IList<IValueToken?> Parameters)
    {
        if (Parameters.Count != 3) return null;
        var param = Parameters[0];
        var lowerBound = Parameters[1];
        var upperBound = Parameters[2];
        if (param is INumberValueToken Number)
        {
            if (lowerBound is not INumberValueToken Lower) return null;
            if (upperBound is not INumberValueToken Upper) return null;
            return new NumberToken { Number = Math.Clamp(Number.Number, Lower.Number, Upper.Number) };
        }
        else if (param is IMatValueToken Mat)
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
            return null;
        }
        else return null;
    }
}
struct Min : IFunction<IValueToken?>
{
    public IValueToken? Invoke(IList<IValueToken?> Parameters)
    {
        if (Parameters.Count == 0) return null;
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
                if (enumerator.Current is not INumberValueToken val) return null;
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
                if (enumerator.Current is not IMatValueToken val) return null;
                Cv2.Min(min, val.Mat, min);
            }
            return new MatToken { Mat = min };
        }
        return null;
    }
}
struct Max : IFunction<IValueToken?>
{
    public IValueToken? Invoke(IList<IValueToken?> Parameters)
    {
        if (Parameters.Count == 0) return null;
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
                if (enumerator.Current is not INumberValueToken val) return null;
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
                if (enumerator.Current is not IMatValueToken val) return null;
                Cv2.Max(max, val.Mat, max);
            }
            return new MatToken { Mat = max };
        }
        return null;
    }
}
class Environment
{
    public Environment()
    {

    }
    public Dictionary<string, IValueToken> Values { get; } = new();
    public Dictionary<string, IFunction<IValueToken?>> Functions { get; } = new();
    public IValueToken? GetValue(string Name)
    {
        return Values.GetValueOrDefault(Name);
    }
    public IFunction<IValueToken?>? GetFunction(string Name)
    {
        return Name.ToLower() switch
        {
            "abs" => new Abs(),
            "clamp" => new Clamp(),
            "min" => new Min(),
            "max" => new Max(),
            _ => Functions.GetValueOrDefault(Name)
        };
    }
}