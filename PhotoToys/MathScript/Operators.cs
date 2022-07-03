using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MathScript;

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

