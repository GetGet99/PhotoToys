using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PTMS;

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
                return new MatrixMatToken {
                    Mat = Number1.Number.ToScalar(Mat2.Mat.Channels()) + Mat2.Mat.AsDoubles().Track(out var tracker)
                }.DisposeTracker(tracker);
            else return new ErrorToken
            {
                Message = $"Operator '+' accepts [Mat/Number]? + [Mat/Number]. However, the second argument received is '{Item2}' which is neither of those."
            };
        }
        if (Item1 is IMatValueToken Mat1)
        {
            if (Item2 is INumberValueToken Number2)
                return new MatrixMatToken {
                    Mat = Mat1.Mat.AsDoubles().Track(out var tracker) + Number2.Number.ToScalar(Mat1.Mat.Channels())
                }.DisposeTracker(tracker);
            else if (Item2 is IMatValueToken Mat2)
            {
                if (Mat1.Mat.IsIdenticalInSizeAndChannel(Mat2.Mat))
                    return new MatrixMatToken {
                        Mat = Mat1.Mat.AsDoubles().Track(out var tracker) + Mat2.Mat.AsDoubles().Track(tracker)
                    }.DisposeTracker(tracker);
                else return new ErrorToken
                {
                    Message = $"Operator '+' (Overload [Mat] + [Mat]) the first item '{Item1}' and the second item '{Item2}' should have the identical size and channel."
                };
            }
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
                return new MatrixMatToken {
                    Mat = Number1.Number.ToScalar(Mat2.Mat.Channels()) - Mat2.Mat.AsDoubles().Track(out var tracker)
                }.DisposeTracker(tracker);
            else return new ErrorToken
            {
                Message = $"Operator '-' accepts [Mat/Number]? - [Mat/Number]. However, the second argument received is '{Item2}' which is neither of those."
            };
        }
        if (Item1 is IMatValueToken Mat1)
        {
            if (Item2 is INumberValueToken Number2)
                return new MatrixMatToken {
                    Mat = Mat1.Mat.AsDoubles().Track(out var tracker) - Number2.Number.ToScalar(Mat1.Mat.Channels())
                }.DisposeTracker(tracker);
            else if (Item2 is IMatValueToken Mat2)
            {
                if (Mat1.Mat.IsIdenticalInSizeAndChannel(Mat2.Mat))
                    return new MatrixMatToken {
                        Mat = Mat1.Mat.AsDoubles().Track(out var tracker) - Mat2.Mat.AsDoubles().Track(tracker)
                    }.DisposeTracker(tracker);
                else return new ErrorToken
                {
                    Message = $"Operator '-' (Overload [Mat] - [Mat]) the first item '{Item1}' and the second item '{Item2}' should have the identical size and channel."
                };
            }
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
                return new MatrixMatToken {
                    Mat = Number1.Number * Mat2.Mat.AsDoubles().Track(out var tracker)
                }.DisposeTracker(tracker);
            else return new ErrorToken
            {
                Message = $"Operator '*' accepts [Mat/Number] * [Mat/Number]. However, the second argument received is '{Item2}' which is neither of those."
            };
        }
        if (Item1 is IMatValueToken Mat1)
        {
            if (Item2 is INumberValueToken Number2)
                return new MatrixMatToken { Mat = Mat1.Mat * Number2.Number };
            else if (Item2 is IMatValueToken Mat2)
            {
                if (Mat1.Mat.IsIdenticalInSizeAndChannel(Mat2.Mat))
                    return new MatrixMatToken {
                        Mat = Mat1.Mat.Track(out var tracker).Mul(Mat2.Mat.Track(tracker))
                    }.DisposeTracker(tracker);
                else return new ErrorToken
                {
                    Message = $"Operator '*' (Overload [Mat] * [Mat]) the first item '{Item1}' and the second item '{Item2}' should have the identical size and channel."
                };
            }
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
                return new MatrixMatToken {
                    Mat = Number1.Number / Mat2.Mat.AsDoubles().Track(out var tracker)
                }.DisposeTracker(tracker);
            else return new ErrorToken
            {
                Message = $"Operator '/' accepts [Mat/Number] / [Mat/Number]. However, the second argument received is '{Item2}' which is neither of those."
            };
        }
        if (Item1 is IMatValueToken Mat1)
        {
            if (Item2 is INumberValueToken Number2)
                return new MatrixMatToken { Mat = Mat1.Mat / Number2.Number };
            else if (Item2 is IMatValueToken Mat2)
            {
                if (Mat1.Mat.IsIdenticalInSizeAndChannel(Mat2.Mat))
                    return new MatrixMatToken {
                        Mat = Mat1.Mat.AsDoubles().Track(out var tracker) / Mat2.Mat.AsDoubles().Track(tracker)
                    }.DisposeTracker(tracker);
                else return new ErrorToken
                {
                    Message = $"Operator '+' accepts (Overload [Mat] + [Mat]) the first item '{Item1}' and the second item '{Item2}' should have the identical size and channel."
                };
            }
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
                Message = $"Type Error: Operator '**' or '^' accepts [Number/Mat] ^ [Number] or ^[Number]. However, the second argument received is '{Item2}'"
            };
        }
        if (Item1 is IMatValueToken Mat1)
        {
            if (Item2 is INumberValueToken Number2)
                return new MatrixMatToken {
                    Mat = Mat1.Mat.AsDoubles().Track(out var tracker).Pow(Number2.Number)
                }.DisposeTracker(tracker);
            else return new ErrorToken
            {
                Message = $"Type Error: Operator '**' or '^' accepts [Number/Mat] ^ [Number] or ^[Number]. However, the second argument received is '{Item2}'"
            };
        } else if (Item1 is INothingValueToken)
        {
            if (Item2 is INumberValueToken Number2)
            {
                if (Number2.Number is < 0) return new ErrorToken
                {
                    Message = $"Out Of Range Error: Operator '**' or '^' (Overload ^[Number (>= 0)]) Recieved value less than 0, or '{Item2}'"
                };
                return new NumberToken { Number = -Number2.Number };
            }
            else return new ErrorToken
            {
                Message = $"Type Error: Operator '**' or '^' accepts [Number/Mat] ^[Number (>= 0)]. However, the argument received is '{Item2}'"
            };
        }
        return new ErrorToken
        {
            Message = $"Type Error: Operator '**' or '^' accepts [Number/Mat] ^ [Number (>= 0)]. However, the first argument received is '{Item1}'"
        };
    }
}
struct Range : IOperator
{
    public IValueToken Invoke(IValueToken Item1, IValueToken Item2) => Run(Item1, Item2);
    public static IValueToken Run(IValueToken Item1, IValueToken Item2)
    {
        if (Item1 is ErrorToken) return Item1;
        if (Item2 is ErrorToken) return Item2;
        Index num1, num2;
        static Index ToIndex(int i)
        {
            if (i < 0) return ^(-i);
            else return i;
        }
        if (Item1 is NothingToken) num1 = Index.Start;
        else if (Item1 is INumberValueToken Number1) num1 = ToIndex(Number1.NumberAsInt);
        else return new ErrorToken
        {
            Message = $"Operator '..' or ':' accepts [Number (Optional)]..[Number (Optional)]. However, the first argument received is '{Item1}' which is not [Number]."
        };

        if (Item2 is NothingToken) num2 = Index.End;
        else if (Item2 is INumberValueToken Number2) num2 = ToIndex(Number2.NumberAsInt);
        else return new ErrorToken
        {
            Message = $"Operator '..' or ':' accepts [Number (Optional)]..[Number (Optional)]. However, the second argument received is '{Item2}' which is not [Number]."
        };
        return new RangeToken { Range = num1..num2 };
    }
}
struct Equal : IOperator
{
    public IValueToken Invoke(IValueToken Item1, IValueToken Item2) => Run(Item1, Item2);
    public static IValueToken Run(IValueToken Item1, IValueToken Item2)
    {
        const string func1 = "[Number] == [Number] -> [Boolean]";
        const string func2 = "[Mat] == [Mat] -> [Mask Mat]";
        const string func3 = "[Mat] == [Number] -> [Mask Mat]";
        if (Item1 is ErrorToken) return Item1;
        if (Item2 is ErrorToken) return Item2;

        if (Item1 is INumberValueToken Number1)
        {
            if (Item2 is INumberValueToken Number2)
                return new BooleanToken
                {
                    Value = Number1.Number == Number2.Number
                };
            else return new ErrorToken
            {
                Message = $"Operator '==' (Overload: {func1})\n\nThe first argument received is '{Item1}' which is [Number] " +
                $"but the second argument received is {Item2} which is not [Number]."
            };
        }
        else if (Item1 is IMatValueToken Mat1)
        {
            if (Item2 is INumberValueToken Number2)
                return Mat1.Mat.Equals(Number2.Number).ToMat().GenerateMatToken(MatrixType.Mask);
            else if (Item2 is IMatValueToken Mat2)
                return Mat1.Mat.Equals(Mat2.Mat).ToMat().GenerateMatToken(MatrixType.Mask);
            else return new ErrorToken
            {
                Message = $"Operator '==' (Overload:\n{func2}\n{func3})\n\nThe first argument received is '{Item1}' which is [Mat] " +
                $"but the second argument received is {Item2} which is not [Number] or [Mat]."
            };
        }
        else return new ErrorToken
        {
            Message = $"Operator '==' accepts\n{func1}\n{func2}\n{func3}. However, the first argument received is '{Item1}' which is not [Number] or [Mat]."
        };
    }
}
struct NotEqual : IOperator
{
    public IValueToken Invoke(IValueToken Item1, IValueToken Item2) => Run(Item1, Item2);
    public static IValueToken Run(IValueToken Item1, IValueToken Item2)
    {
        const string func1 = "[Number] != [Number] -> [Boolean]";
        const string func2 = "[Mat] != [Mat] -> [Mask Mat]";
        const string func3 = "[Mat] != [Number] -> [Mask Mat]";
        if (Item1 is ErrorToken) return Item1;
        if (Item2 is ErrorToken) return Item2;

        if (Item1 is INumberValueToken Number1)
        {
            if (Item2 is INumberValueToken Number2)
                return new BooleanToken
                {
                    Value = Number1.Number != Number2.Number
                };
            else return new ErrorToken
            {
                Message = $"Operator '==' (Overload: {func1})\n\nThe first argument received is '{Item1}' which is [Number] " +
                $"but the second argument received is {Item2} which is not [Number]."
            };
        }
        else if (Item1 is IMatValueToken Mat1)
        {
            if (Item2 is INumberValueToken Number2)
                return Mat1.Mat.NotEquals(Number2.Number).ToMat().GenerateMatToken(MatrixType.Mask);
            else if (Item2 is IMatValueToken Mat2)
                return Mat1.Mat.NotEquals(Mat2.Mat).ToMat().GenerateMatToken(MatrixType.Mask);
            else return new ErrorToken
            {
                Message = $"Operator '==' (Overload:\n{func2}\n{func3})\n\nThe first argument received is '{Item1}' which is [Mat] " +
                $"but the second argument received is {Item2} which is not [Number] or [Mat]."
            };
        }
        else return new ErrorToken
        {
            Message = $"Operator '==' accepts\n{func1}\n{func2}\n{func3}. However, the first argument received is '{Item1}' which is not [Number] or [Mat]."
        };
    }
}
struct GreaterThan : IOperator
{
    public IValueToken Invoke(IValueToken Item1, IValueToken Item2) => Run(Item1, Item2);
    public static IValueToken Run(IValueToken Item1, IValueToken Item2)
    {
        const string func1 = "[Number] > [Number] -> [Boolean]";
        const string func2 = "[Mat] > [Mat] -> [Mask Mat]";
        const string func3 = "[Mat] > [Number] -> [Mask Mat]";
        if (Item1 is ErrorToken) return Item1;
        if (Item2 is ErrorToken) return Item2;

        if (Item1 is INumberValueToken Number1)
        {
            if (Item2 is INumberValueToken Number2)
                return new BooleanToken
                {
                    Value = Number1.Number > Number2.Number
                };
            else return new ErrorToken
            {
                Message = $"Operator '>' (Overload: {func1})\n\nThe first argument received is '{Item1}' which is [Number] " +
                $"but the second argument received is {Item2} which is not [Number]."
            };
        }
        else if (Item1 is IMatValueToken Mat1)
        {
            if (Item2 is INumberValueToken Number2)
                return Mat1.Mat.GreaterThan(Number2.Number).ToMat().GenerateMatToken(MatrixType.Mask);
            else if (Item2 is IMatValueToken Mat2)
                return Mat1.Mat.GreaterThan(Mat2.Mat).ToMat().GenerateMatToken(MatrixType.Mask);
            else return new ErrorToken
            {
                Message = $"Operator '>' (Overload:\n{func2}\n{func3})\n\nThe first argument received is '{Item1}' which is [Mat] " +
                $"but the second argument received is {Item2} which is not [Number] or [Mat]."
            };
        }
        else return new ErrorToken
        {
            Message = $"Operator '>' accepts\n{func1}\n{func2}\n{func3}. However, the first argument received is '{Item1}' which is not [Number] or [Mat]."
        };
    }
}
struct GreaterThanOrEqual : IOperator
{
    public IValueToken Invoke(IValueToken Item1, IValueToken Item2) => Run(Item1, Item2);
    public static IValueToken Run(IValueToken Item1, IValueToken Item2)
    {
        const string func1 = "[Number] >= [Number] -> [Boolean]";
        const string func2 = "[Mat] >= [Mat] -> [Mask Mat]";
        const string func3 = "[Mat] >= [Number] -> [Mask Mat]";
        if (Item1 is ErrorToken) return Item1;
        if (Item2 is ErrorToken) return Item2;

        if (Item1 is INumberValueToken Number1)
        {
            if (Item2 is INumberValueToken Number2)
                return new BooleanToken
                {
                    Value = Number1.Number >= Number2.Number
                };
            else return new ErrorToken
            {
                Message = $"Operator '>=' (Overload: {func1})\n\nThe first argument received is '{Item1}' which is [Number] " +
                $"but the second argument received is {Item2} which is not [Number]."
            };
        }
        else if (Item1 is IMatValueToken Mat1)
        {
            if (Item2 is INumberValueToken Number2)
                return Mat1.Mat.GreaterThanOrEqual(Number2.Number).ToMat().GenerateMatToken(MatrixType.Mask);
            else if (Item2 is IMatValueToken Mat2)
                return Mat1.Mat.GreaterThanOrEqual(Mat2.Mat).ToMat().GenerateMatToken(MatrixType.Mask);
            else return new ErrorToken
            {
                Message = $"Operator '>=' (Overload:\n{func2}\n{func3})\n\nThe first argument received is '{Item1}' which is [Mat] " +
                $"but the second argument received is {Item2} which is not [Number] or [Mat]."
            };
        }
        else return new ErrorToken
        {
            Message = $"Operator '>=' accepts\n{func1}\n{func2}\n{func3}. However, the first argument received is '{Item1}' which is not [Number] or [Mat]."
        };
    }
}
struct LessThan : IOperator
{
    public IValueToken Invoke(IValueToken Item1, IValueToken Item2) => Run(Item1, Item2);
    public static IValueToken Run(IValueToken Item1, IValueToken Item2)
    {
        const string func1 = "[Number] < [Number] -> [Boolean]";
        const string func2 = "[Mat] < [Mat] -> [Mask Mat]";
        const string func3 = "[Mat] < [Number] -> [Mask Mat]";
        if (Item1 is ErrorToken) return Item1;
        if (Item2 is ErrorToken) return Item2;

        if (Item1 is INumberValueToken Number1)
        {
            if (Item2 is INumberValueToken Number2)
                return new BooleanToken
                {
                    Value = Number1.Number < Number2.Number
                };
            else return new ErrorToken
            {
                Message = $"Operator '<' (Overload: {func1})\n\nThe first argument received is '{Item1}' which is [Number] " +
                $"but the second argument received is {Item2} which is not [Number]."
            };
        }
        else if (Item1 is IMatValueToken Mat1)
        {
            if (Item2 is INumberValueToken Number2)
                return Mat1.Mat.LessThan(Number2.Number).ToMat().GenerateMatToken(MatrixType.Mask);
            else if (Item2 is IMatValueToken Mat2)
                return Mat1.Mat.LessThan(Mat2.Mat).ToMat().GenerateMatToken(MatrixType.Mask);
            else return new ErrorToken
            {
                Message = $"Operator '<' (Overload:\n{func2}\n{func3})\n\nThe first argument received is '{Item1}' which is [Mat] " +
                $"but the second argument received is {Item2} which is not [Number] or [Mat]."
            };
        }
        else return new ErrorToken
        {
            Message = $"Operator '<' accepts\n{func1}\n{func2}\n{func3}. However, the first argument received is '{Item1}' which is not [Number] or [Mat]."
        };
    }
}
struct LessThanOrEqual : IOperator
{
    public IValueToken Invoke(IValueToken Item1, IValueToken Item2) => Run(Item1, Item2);
    public static IValueToken Run(IValueToken Item1, IValueToken Item2)
    {
        const string func1 = "[Number] <= [Number] -> [Boolean]";
        const string func2 = "[Mat] <= [Mat] -> [Mask Mat]";
        const string func3 = "[Mat] <= [Number] -> [Mask Mat]";
        if (Item1 is ErrorToken) return Item1;
        if (Item2 is ErrorToken) return Item2;

        if (Item1 is INumberValueToken Number1)
        {
            if (Item2 is INumberValueToken Number2)
                return new BooleanToken
                {
                    Value = Number1.Number <= Number2.Number
                };
            else return new ErrorToken
            {
                Message = $"Operator '<=' (Overload: {func1})\n\nThe first argument received is '{Item1}' which is [Number] " +
                $"but the second argument received is {Item2} which is not [Number]."
            };
        }
        else if (Item1 is IMatValueToken Mat1)
        {
            if (Item2 is INumberValueToken Number2)
                return Mat1.Mat.LessThanOrEqual(Number2.Number).ToMat().GenerateMatToken(MatrixType.Mask);
            else if (Item2 is IMatValueToken Mat2)
                return Mat1.Mat.LessThanOrEqual(Mat2.Mat).ToMat().GenerateMatToken(MatrixType.Mask);
            else return new ErrorToken
            {
                Message = $"Operator '<=' (Overload:\n{func2}\n{func3})\n\nThe first argument received is '{Item1}' which is [Mat] " +
                $"but the second argument received is {Item2} which is not [Number] or [Mat]."
            };
        }
        else return new ErrorToken
        {
            Message = $"Operator '<=' accepts\n{func1}\n{func2}\n{func3}. However, the first argument received is '{Item1}' which is not [Number] or [Mat]."
        };
    }
}
struct Assign : IOperator
{
    public IValueToken Invoke(IValueToken Item1, IValueToken Item2) => Run(Item1, Item2);
    public static IValueToken Run(IValueToken Item1, IValueToken Item2)
    {
        if (Item1 is ErrorToken) return Item1;
        if (Item2 is ErrorToken) return Item2;

        if (Item1 is not VariableNameReferenceToken varRef) return new ErrorToken
        {
            Message = $"Operator '=' accepts [VariableNameReference] = [Any]. However, the first argument received is '{Item1}' which is not [VariableNameReference]."
        };
        varRef.SetValue(Item2);
        return Item2;
    }
}