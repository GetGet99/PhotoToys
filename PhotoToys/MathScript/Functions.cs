using OpenCvSharp;
using PhotoToys;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Range = System.Range;

namespace MathScript;

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
            "rgbreplace" => new RGBReplace(),
            "alphareplace" => new AlphaReplace(),
            "getchannelcount" => new GetChannelCount(),
            "getchannel" => new GetChannel(),
            "getrgb" => new GetRGB(),
            "replacechannel" => new ReplaceChannel(),
            "normalizeto" => new NormalizeTo(),
            "toimage" => new ToImage(),
            "tomatrix" => new ToMatrix(),
            "submat" => new SubMat(),
            "medianblur" => new MedianBlur(),
            "blur" => new Blur(),
            _ => Functions.GetValueOrDefault(Name, new ErrorToken { Message = $"The name '{Name}' is not a valid function" })
        };
    }
}

struct Abs : IFunction
{
    public IValueToken Invoke(IList<IValueToken> Parameters)
    {
        const string fn = nameof(Abs);
        const string numberOverload = $"[Number] {fn}([Number] value)";
        const string matOverload = $"[n-Channel y*x Mat] {fn}abs([n-Channel y*x Mat] value)";
        if (Parameters.FirstOrDefault(a => a is ErrorToken, null) is ErrorToken et) return et;
        if (Parameters.Count != 1) return new ErrorToken
        {
            Message = $"Parameter Error: Function {numberOverload} | {matOverload} accept 1 paramter but {Parameters.Count} was/were given"
        };
        var value = Parameters[0];
        if (value is INumberValueToken Number)
            return new NumberToken { Number = Math.Abs(Number.Number) };
        else if (value is IMatValueToken Mat)
            return Mat.Mat.Abs().ToMat().GenerateMatToken();
        else return new ErrorToken
        {
            Message = $"Type Error: Function {numberOverload} | {matOverload}, value '{value}' should be [Number/Mat]"
        };
    }
}
struct Clamp : IFunction
{
    public IValueToken Invoke(IList<IValueToken> Parameters)
    {
        const string fn = nameof(Clamp);
        const string OverloadNumber = $"[Number] {fn}([Number] value, [Number] lowerBound, [Number] upperBound)";
        const string OverloadMatNumber = $"[n-Channel y*x Type1 Mat] {fn}([n-Channel y*x Type1 Mat] value, [Number] lowerBound, [Number] upperBound)";
        const string OverloadMatMat = $"[n-Channel y*x Type1 Mat] {fn}([n-Channel y*x Type1 Mat] value, [n-Channel y*x Type1 Mat] lowerBound, [n-Channel y*x Type1 Mat] upperBound)";
        if (Parameters.FirstOrDefault(a => a is ErrorToken, null) is ErrorToken et) return et;
        if (Parameters.Count != 3) return new ErrorToken
        {
            Message = $"Parameter Error: Function {OverloadNumber} | {OverloadMatNumber} | {OverloadMatMat}" +
            $"accept 3 paramters but {Parameters.Count} was/were given"
        };
        var value = Parameters[0];
        var lowerBound = Parameters[1];
        var upperBound = Parameters[2];
        if (value is INumberValueToken Number)
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
        else if (value is IMatValueToken Mat)
        {
            if (lowerBound is INumberValueToken Lower && upperBound is INumberValueToken Upper)
            {
                var ds = new ResourcesTracker();
                var mat = Mat.Mat;
                mat.SetTo(Lower.Number, mat.LessThan(Lower.Number).Track(ds));
                mat.SetTo(Upper.Number, mat.GreaterThan(Upper.Number).Track(ds));
                return mat.GenerateMatToken();
            }
            if (lowerBound is IMatValueToken LowerMat && upperBound is IMatValueToken UpperMat)
            {
                var ds = new ResourcesTracker();
                var mat = Mat.Mat;
                if (!LowerMat.Mat.IsIdenticalInSizeAndChannel(mat)) return new ErrorToken
                {
                    Message = $"Type Error: {OverloadMatMat}, " +
                    $"{nameof(lowerBound)} '{lowerBound}' and {nameof(value)} '{value}' are not identical in size and/or channel."
                };
                if (!UpperMat.Mat.IsIdenticalInSizeAndChannel(mat)) return new ErrorToken
                {
                    Message = $"Type Error: {OverloadMatMat}, " +
                    $"{nameof(upperBound)} '{upperBound}' and {nameof(value)} '{value}' are not identical in size and/or channel."
                };
                var mask1 = mat.LessThan(LowerMat.Mat).Track(ds);
                var mask2 = mat.GreaterThan(UpperMat.Mat).Track(ds);
                Cv2.CopyTo(LowerMat.Mat, mat, mask1);
                Cv2.CopyTo(UpperMat.Mat, mat, mask2);
                return mat.GenerateMatToken();
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
            $"{nameof(value)} '{value}', {nameof(lowerBound)} '{lowerBound}', and {nameof(upperBound)} '{upperBound}' does not match any of the given overload"
        };
    }
}
struct Min : IFunction
{
    public IValueToken Invoke(IList<IValueToken> Parameters)
    {
        const string fn = nameof(Min);
        const string OverloadNumber = $"[Number] {fn}(params [Number] Xs)";
        const string OverloadOneMat = $"[Number] {fn}([Mat] x)";
        const string OverloadMultipleMat = $"[n-Channel y*x Mat] {fn}(params [n-Channel y*x Mat] Xs)";
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
                if (!val.Mat.IsIdenticalInSizeAndChannel(min)) return new ErrorToken
                {
                    Message = $"Type Error: Function {OverloadMultipleMat} received a non-identical size and/or channel matrix (Current = '{enumerator.Current}' Previous = '{MatToken.FormatToString(min)}')"
                };
                Cv2.Min(min, val.Mat, min);
            }
            return min.GenerateMatToken();
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
        const string fn = nameof(Max);
        const string OverloadNumber = $"[Number] {fn}(params [Number] Xs)";
        const string OverloadOneMat = $"[Number] {fn}([Mat] x)";
        const string OverloadMultipleMat = $"[Type1 Mat] {fn}(params [n-Channel Type1 Mat y*x] Xs)";
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
                if (!val.Mat.IsIdenticalInSizeAndChannel(max)) return new ErrorToken
                {
                    Message = $"Type Error: Function {OverloadMultipleMat}, received a non-identical size and/or channel matrix. (Current = '{enumerator.Current}', Previous = '{MatToken.FormatToString(max)}')"
                };
                Cv2.Max(max, val.Mat, max);
            }
            return max.GenerateMatToken();
        }
        return new ErrorToken
        {
            Message = $"Type Error: Function {OverloadNumber} | {OverloadOneMat} | {OverloadMultipleMat}, cannot find a matching overload for first parameter {Parameters[0]}"
        };
    }
}
struct RGBReplace : IFunction
{
    public IValueToken Invoke(IList<IValueToken> Parameters)
    {
        const string func = $"[Mat] {nameof(RGBReplace)}([4-Channel y*x Mat] rgba, [3-Channel y*x Mat] rgb)";
        if (Parameters.FirstOrDefault(a => a is ErrorToken, null) is ErrorToken et) return et;
        if (Parameters.Count != 2) return new ErrorToken
        {
            Message = $"Parameter Error: {func} accept 2 paramters but {Parameters.Count} was/were given"
        };
        var rgba = Parameters[0];
        var rgb = Parameters[1];
        if (rgba is not IMatValueToken RGBAMat || RGBAMat.Mat.Channels() is not 4)
            return new ErrorToken
            {
                Message = $"Type Error: Function {func}, rgba '{rgba}' should be [4-Channel Mat]"
            };
        if (rgb is not IMatValueToken RGBMat || RGBMat.Mat.Channels() is not 3)
            return new ErrorToken
            {
                Message = $"Type Error: Function {func}, rgb '{rgb}' should be [3-Channel Mat]"
            };
        if (!RGBAMat.Mat.IsIdenticalInSize(RGBMat.Mat)) return new ErrorToken
        {
            Message = $"Type Error: Function {func}, {nameof(rgba)} '{rgba}' and {nameof(rgb)} '{rgb}' have non-identical size"
        };
        var RGBA = RGBAMat.Mat.Split();
        var RGB = RGBMat.Mat.Split();

        RGBA[0].Dispose();
        RGBA[0] = RGB[0];
        RGBA[1].Dispose();
        RGBA[1] = RGB[1];
        RGBA[2].Dispose();
        RGBA[2] = RGB[2];
        Mat m = new();
        Cv2.Merge(RGBA, m);
        RGBA.Dispose();
        RGB.Dispose();
        return new ImageMatToken { Mat = m };
    }
}
struct AlphaReplace : IFunction
{
    public IValueToken Invoke(IList<IValueToken> Parameters)
    {
        const string func = $"[Mat] {nameof(AlphaReplace)}([4-Channel Mat] rgba, [1-Channel Mat] alpha)";
        if (Parameters.FirstOrDefault(a => a is ErrorToken, null) is ErrorToken et) return et;
        if (Parameters.Count != 2) return new ErrorToken
        {
            Message = $"Parameter Error: {func} accept 2 paramters but {Parameters.Count} was/were given"
        };
        var rgba = Parameters[0];
        var alpha = Parameters[1];
        if (rgba is not IMatValueToken RGBAMat || RGBAMat.Mat.Channels() is not 4)
            return new ErrorToken
            {
                Message = $"Type Error: Function {func}, rgba '{rgba}' should be [4-Channel Mat]"
            };
        if (alpha is not IMatValueToken AlphaMat || AlphaMat.Mat.Channels() is not 1)
            return new ErrorToken
            {
                Message = $"Type Error: Function {func}, alpha '{alpha}' should be [1-Channel Mat]"
            };
        if (!RGBAMat.Mat.IsIdenticalInSize(AlphaMat.Mat)) return new ErrorToken
        {
            Message = $"Type Error: Function {func}, {nameof(rgba)} '{rgba}' and {nameof(alpha)} '{alpha}' have non-identical size"
        };
        var RGBA = RGBAMat.Mat.Split();

        RGBA[3].Dispose();
        RGBA[3] = AlphaMat.Mat;
        Mat m = new();
        Cv2.Merge(RGBA, m);
        RGBA.Dispose();
        return new ImageMatToken { Mat = m };
    }
}
struct GetChannelCount : IFunction
{
    public IValueToken Invoke(IList<IValueToken> Parameters)
    {
        const string func = $"[Number (=n)] {nameof(GetChannelCount)}([n-Channel Mat] mat)";
        if (Parameters.FirstOrDefault(a => a is ErrorToken, null) is ErrorToken et) return et;
        if (Parameters.Count != 1) return new ErrorToken
        {
            Message = $"Parameter Error: {func} accept 1 paramter but {Parameters.Count} was/were given"
        };
        var mat = Parameters[0];
        if (mat is not IMatValueToken Mat)
            return new ErrorToken
            {
                Message = $"Type Error: Function {func}, mat '{mat}' should be [Mat]"
            };
        return new NumberToken { Number = Mat.Mat.Channels() };
    }
}
struct GetRGB : IFunction
{
    public IValueToken Invoke(IList<IValueToken> Parameters)
    {
        const string func = $"[3-Channel y*x Mat] {nameof(GetRGB)}([{{3,4}}-Channel y*x Mat] mat)";
        if (Parameters.FirstOrDefault(a => a is ErrorToken, null) is ErrorToken et) return et;
        if (Parameters.Count != 1) return new ErrorToken
        {
            Message = $"Parameter Error: {func} accept 1 paramter but {Parameters.Count} was/were given"
        };
        var mat = Parameters[0];
        if (mat is not ImageMatToken Mat)
            return new ErrorToken
            {
                Message = $"Type Error: Function {func}, mat '{mat}' should be [Mat]"
            };
        return Mat.Mat.Channels() switch
        {
            3 => new ImageMatToken { Mat = Mat.Mat.Clone() },
            4 => new ImageMatToken { Mat = Mat.Mat.SubMat(chanRange: 0..3) },
            _ => new ErrorToken
            {
                Message = $"Type Error: Function {func}, mat '{mat}' should have 3 or 4 channel"
            }
        };
    }
}
struct GetChannel : IFunction
{
    public IValueToken Invoke(IList<IValueToken> Parameters)
    {
        const string func = $"[1-Channel y*x Type1 Mat] {nameof(GetChannel)}([n-Channel y*x Type1 Mat] mat, [Number (>= 0, < n)] channel)";
        if (Parameters.FirstOrDefault(a => a is ErrorToken, null) is ErrorToken et) return et;
        if (Parameters.Count != 2) return new ErrorToken
        {
            Message = $"Parameter Error: {func} accept 2 paramters but {Parameters.Count} was/were given"
        };
        var mat = Parameters[0];
        var channel = Parameters[1];
        if (mat is not IMatValueToken Mat)
            return new ErrorToken
            {
                Message = $"Type Error: Function {func}, mat '{mat}' should be [Mat]"
            };
        if (channel is not INumberValueToken Channel)
            return new ErrorToken
            {
                Message = $"Type Error: Function {func}, channel '{channel}' should be [Number]"
            };
        var idx = (int)Math.Round(Channel.Number);
        if (idx < 0)
            return new ErrorToken
            {
                Message = $"Channel Out Of Range Error: Function {func}, Channel '{idx}' (rounded from '{channel}') should be more than 0"
            };
        if (idx >= Mat.Mat.Channels())
            return new ErrorToken
            {
                Message = $"Channel Out Of Range Error: Function {func}, Channel '{idx}' (rounded from '{channel}') is out of bounds (the mat {mat} has {Mat.Mat.Channels()} channels.\n(Note: The first channel starts at channel 0, not 1)"
            };
        return Mat.Mat.ExtractChannel(idx).GenerateMatToken();
    }
}
struct ReplaceChannel : IFunction
{
    public IValueToken Invoke(IList<IValueToken> Parameters)
    {
        const string func = $"[n-Channel y*x Type1 Mat] {nameof(ReplaceChannel)}([n-Channel y*x Type1 Mat] mat, [Number (>= 0, < n)] channel, [1-Channel y*x Type1 Mat] replaceMat)";
        if (Parameters.FirstOrDefault(a => a is ErrorToken, null) is ErrorToken et) return et;
        if (Parameters.Count != 3) return new ErrorToken
        {
            Message = $"Parameter Error: {func} accept 3 paramters but {Parameters.Count} was/were given"
        };
        var mat = Parameters[0];
        var channel = Parameters[1];
        var replaceMat = Parameters[2];
        if (mat is not IMatValueToken Mat)
            return new ErrorToken
            {
                Message = $"Type Error: Function {func}, mat '{mat}' should be [Mat]"
            };
        if (channel is not INumberValueToken Channel)
            return new ErrorToken
            {
                Message = $"Type Error: Function {func}, channel '{channel}' should be [Number]"
            };
        if (replaceMat is not IMatValueToken ReplaceMat || ReplaceMat.Mat.Channels() is not 1)
            return new ErrorToken
            {
                Message = $"Type Error: Function {func}, replaceMat '{replaceMat}' should be [1-Channel Mat]"
            };
        var idx = (int)Math.Round(Channel.Number);
        if (idx < 0)
            return new ErrorToken
            {
                Message = $"Channel Out Of Range Error: Function {func}, Channel '{idx}' (rounded from '{channel}') should be more than 0"
            };
        if (idx >= Mat.Mat.Channels())
            return new ErrorToken
            {
                Message = $"Channel Out Of Range Error: Function {func}, Channel '{idx}' (rounded from '{channel}') is out of bounds (the mat {mat} has {Mat.Mat.Channels()} channels.\n(Note: The first channel starts at channel 0, not 1)"
            };
        if (Mat.Mat.IsIdenticalInSize(ReplaceMat.Mat)) return new ErrorToken
        {
            Message = $"Type Error: Function {func}, {nameof(mat)} '{mat}' and {nameof(replaceMat)} '{replaceMat}' have non-identical size"
        };
        var splittedMat = Mat.Mat.Split();
        splittedMat[idx] = ReplaceMat.Mat;
        Mat m = new();
        Cv2.Merge(splittedMat, m);
        splittedMat.Dispose();
        return m.GenerateMatToken();
    }
}
struct NormalizeTo : IFunction
{
    public IValueToken Invoke(IList<IValueToken> Parameters)
    {
        const string func = $"[n-Channel y*x Matrix Mat] {nameof(NormalizeTo)}([n-Channel y*x Mat] mat, [Number] normMax)";
        if (Parameters.FirstOrDefault(a => a is ErrorToken, null) is ErrorToken et) return et;
        if (Parameters.Count != 2) return new ErrorToken
        {
            Message = $"Parameter Error: {func} accept 2 paramters but {Parameters.Count} was/were given"
        };
        var mat = Parameters[0];
        var normMax = Parameters[1];
        if (mat is not IMatValueToken Mat)
            return new ErrorToken
            {
                Message = $"Type Error: Function {func}, mat '{mat}' should be [Mat]"
            };
        if (normMax is not INumberValueToken NormalizeMax)
            return new ErrorToken
            {
                Message = $"Type Error: Function {func}, normMax '{normMax}' should be [Number]"
            };
        var tracker = new ResourcesTracker();
        var max = Mat.Mat.Max();
        var m = Mat.Mat.AsDoubles().Track(tracker) * (NormalizeMax.Number / max);
        return new MatrixMatToken { Mat = m.Track(tracker).ToMat() };
    }
}
struct ToImage : IFunction
{
    public IValueToken Invoke(IList<IValueToken> Parameters)
    {
        const string func1 = $"[1-Channel y*x Image Mat] {nameof(ToImage)}([1-Channel y*x Mat] mat)";
        const string func3 = $"[3-Channel y*x Image Mat] {nameof(ToImage)}([3-Channel y*x Mat] mat)";
        const string func4 = $"[4-Channel y*x Image Mat] {nameof(ToImage)}([4-Channel y*x Mat] mat)";
        if (Parameters.FirstOrDefault(a => a is ErrorToken, null) is ErrorToken et) return et;
        if (Parameters.Count != 1) return new ErrorToken
        {
            Message = $"Parameter Error: {func1} | {func3} | {func4} accept 1 paramter but {Parameters.Count} was/were given"
        };
        var mat = Parameters[0];
        if (mat is not IMatValueToken Mat || Mat.Mat.Channels() is not (1 or 3 or 4))
            return new ErrorToken
            {
                Message = $"Type Error: Function {func1} | {func3} | {func4}, mat '{mat}' should be [{{1/3/4}}-Channel Mat]"
            };
        return new ImageMatToken { Mat = Mat.Mat.AsBytes() };
    }
}
struct ToMatrix : IFunction
{
    public IValueToken Invoke(IList<IValueToken> Parameters)
    {
        const string func1 = $"[n-Channel y*x Matrix Mat] {nameof(ToMatrix)}([n-Channel y*x Mat] mat)";
        if (Parameters.FirstOrDefault(a => a is ErrorToken, null) is ErrorToken et) return et;
        if (Parameters.Count != 1) return new ErrorToken
        {
            Message = $"Parameter Error: {func1} accept 1 paramter but {Parameters.Count} was/were given"
        };
        var mat = Parameters[0];
        if (mat is not IMatValueToken Mat)
            return new ErrorToken
            {
                Message = $"Type Error: Function {func1}, mat '{mat}' should be [n-Channel y*x Mat]"
            };
        return new MatrixMatToken { Mat = Mat.Mat.AsDoubles() };
    }
}
struct StdFilter : IFunction
{
    public IValueToken Invoke(IList<IValueToken> Parameters)
    {
        const string func1 = $"[n-Channel y*x Matrix Mat] {nameof(StdFilter)}([n-Channel y*x Mat] mat, [Number] kernalsize)";
        const string func2 = $"[n-Channel y*x Matrix Mat] {nameof(StdFilter)}([n-Channel y*x Mat] mat, [Number] kernalsizeX, [Number] kernalsizeY)";
        if (Parameters.FirstOrDefault(a => a is ErrorToken, null) is ErrorToken et) return et;
        if (Parameters.Count is not (2 or 3)) return new ErrorToken
        {
            Message = $"Parameter Error: {func1} | {func2} accept 2/3 paramter but {Parameters.Count} was/were given"
        };
        var mat = Parameters[0];
        if (mat is not IMatValueToken Mat)
            return new ErrorToken
            {
                Message = $"Type Error: Function {func1} | {func2}, mat '{mat}' should be [n-Channel y*x Mat]"
            };
        var ksx = Parameters[1];
        if (ksx is not INumberValueToken KSX)
            return new ErrorToken
            {
                Message = $"Type Error: Function {func1} | {func2}, kernalsize/kernalsizeX '{ksx}' should be [Number]"
            };
        var ksy = Parameters.Count is 2 ? ksx : Parameters[2];
        if (ksy is not INumberValueToken KSY)
            return new ErrorToken
            {
                Message = $"Type Error: Function {func2}, kernalsizeY '{ksy}' should be [Number]"
            };
        using var tracker = new ResourcesTracker();
        return new MatrixMatToken
        { Mat = Mat.Mat.AsDoubles().Track(tracker)
            .StdFilter(new Size(KSX.NumberAsInt, KSY.NumberAsInt)) };
    }
}
struct Blur : IFunction
{
    public IValueToken Invoke(IList<IValueToken> Parameters)
    {
        const string func1 = $"[n-Channel y*x Mat] {nameof(Blur)}([n-Channel y*x Image Mat] mat, [Number] kernalsize)";
        const string func2 = $"[n-Channel y*x Mat] {nameof(Blur)}([n-Channel y*x Image Mat] mat, [Number] kernalsizeX, [Number] kernalsizeY)";
        if (Parameters.FirstOrDefault(a => a is ErrorToken, null) is ErrorToken et) return et;
        if (Parameters.Count is not (2 or 3)) return new ErrorToken
        {
            Message = $"Parameter Error: {func1} | {func2} accept 2/3 paramter but {Parameters.Count} was/were given"
        };
        var mat = Parameters[0];
        if (mat is not ImageMatToken Mat)
            return new ErrorToken
            {
                Message = $"Type Error: Function {func1} | {func2}, mat '{mat}' should be [n-Channel y*x Image Mat]"
            };
        var ksx = Parameters[1];
        if (ksx is not INumberValueToken KSX)
            return new ErrorToken
            {
                Message = $"Type Error: Function {func1} | {func2}, kernalsize/kernalsizeX '{ksx}' should be [Number]"
            };
        var ksy = Parameters.Count is 2 ? ksx : Parameters[2];
        if (ksy is not INumberValueToken KSY)
            return new ErrorToken
            {
                Message = $"Type Error: Function {func2}, kernalsizeY '{ksy}' should be [Number]"
            };
        using var tracker = new ResourcesTracker();
        return new ImageMatToken
        {
            Mat = Mat.Mat.AsDoubles().Track(tracker)
            .Blur(new Size(KSX.NumberAsInt, KSY.NumberAsInt))
        };
    }
}
struct MedianBlur : IFunction
{
    public IValueToken Invoke(IList<IValueToken> Parameters)
    {
        const string func1 = $"[n-Channel y*x Mat] {nameof(MedianBlur)}([n-Channel y*x Image Mat] mat, [Odd Positive Number] kernalsize)";
        if (Parameters.FirstOrDefault(a => a is ErrorToken, null) is ErrorToken et) return et;
        if (Parameters.Count is not (2 or 3)) return new ErrorToken
        {
            Message = $"Parameter Error: {func1} accept 2/3 paramter but {Parameters.Count} was/were given"
        };
        var mat = Parameters[0];
        if (mat is not ImageMatToken Mat)
            return new ErrorToken
            {
                Message = $"Type Error: Function {func1}, mat '{mat}' should be [n-Channel y*x Mat]"
            };
        var ks = Parameters[1];
        if (ks is not INumberValueToken KS || KS.NumberAsInt is <0 || KS.NumberAsInt % 2 is 0)
            return new ErrorToken
            {
                Message = $"Type Error: Function {func1}, kernalsize '{ks}' should be [Odd Positive Number]"
            };
        using var tracker = new ResourcesTracker();
        return new ImageMatToken
        {
            Mat = Mat.Mat.MedianBlur(KS.NumberAsInt)
        };
    }
}
struct SubMat : IFunction
{
    enum ErrorType
    {
        OK,
        UnrecognizedType,
        OutOfRange
    }
    public IValueToken Invoke(IList<IValueToken> Parameters)
    {
        const string funcnum = $"[Number] {nameof(SubMat)}([nIn-Channel Mat] mat, [Number (>= 0, < y)] yIn, [Number (>= 0, < x)] xIn, [Number (>= 0, < nIn)] channelIn)";
        const string funcmat = $"[Type1 Mat] {nameof(SubMat)}([nIn-Channel Type1 Mat] mat, [Number (>= 0, < y) | Range (<= y) (Optional)] yIn, [Number (>= 0, <= x) | Range (<= x) (Optional)] xIn, [Number (>= 0, < nIn) | Range (<= nIn) (Optional)] channelIn)";
        if (Parameters.FirstOrDefault(a => a is ErrorToken, null) is ErrorToken et) return et;
        if (Parameters.Count is not (>= 2 and <= 4)) return new ErrorToken
        {
            Message = $"Parameter Error: {funcnum} | {funcmat} accept 2-4 paramters but {Parameters.Count} was/were given"
        };
        var mat = Parameters[0];
        var yIn = Parameters[1];
        var xIn = (Parameters.Count >= 3) ? Parameters[2] : new RangeToken { Range = .. };
        var channelIn = (Parameters.Count >= 4) ? Parameters[3] : new RangeToken { Range = .. };
        if (mat is not IMatValueToken Mat)
            return new ErrorToken
            {
                Message = $"Type Error: Function {funcnum}, mat '{mat}' should be [Mat]"
            };
        if (xIn is INumberValueToken xIn1 && yIn is INumberValueToken yIn1 && channelIn is INumberValueToken channelIn1)
        {
            var m = Mat.Mat;
            var channel = channelIn1.NumberAsInt;
            var y = yIn1.NumberAsInt;
            var x = xIn1.NumberAsInt;
            if (channel + 1 > m.Channels())
                return new ErrorToken
                {
                    Message = $"Channel Out Of Range Error: Function {funcnum}, {nameof(channel)} '{channel}' is out of range (mat = '{mat}', Expecting a value between 0 and {m.Channels() - 1})"
                };
            if (y + 1 > m.Rows)
                return new ErrorToken
                {
                    Message = $"Index Out Of Range Error: Function {funcnum}, {nameof(yIn)} '{yIn}' is out of range (mat = '{mat}', Expecting a value between 0 and {m.Rows - 1})"
                };
            if (x + 1 > m.Cols)
                return new ErrorToken
                {
                    Message = $"Index Out Of Range Error: Function {funcnum}, {nameof(xIn)} '{xIn}' is out of range (mat = '{mat}', Expecting a value between 0 and {m.Cols - 1})"
                };
            var tracker = new ResourcesTracker();
            return new NumberToken
            {
                Number = m.ExtractChannel(channel).Track(tracker).Get<double>(yIn1.NumberAsInt, xIn1.NumberAsInt)
            };
        }
        else
        {
            static ErrorToken? GetRange(IValueToken mat, IValueToken? ValueToken, string TokenName, int maxRange, bool IsChannel, out System.Range Range)
            {
                Range = default;
                if (ValueToken is null)
                {
                    Range = ..;
                    return null;
                }
                else if (ValueToken is INumberValueToken num)
                {
                    var n = num.NumberAsInt;
                    if (n < 0) n = maxRange - n;
                    if (n < 0) goto Error;
                    if (n < maxRange)
                    {
                        Range = n..(n + 1);
                        return null;
                    }
                Error:
                    return new ErrorToken
                    {
                        Message = $"{(IsChannel ? "Channel" : "Index")} Out Of Range Error: Function {funcmat}, {TokenName} '{ValueToken}' has the value of " +
                        $"{num.NumberAsInt}, which is out of range (Specified Range: -{maxRange} to {maxRange - 1})"
                    };
                }
                else if (ValueToken is IRangeValueToken range)
                {
                    var r = range.Range;
                    if (r.Start.IsFromEnd && r.Start.Value > maxRange) return new ErrorToken
                    {
                        Message = $"{(IsChannel ? "Channel" : "Index")} Out Of Range Error: Function {funcmat}, {TokenName} '{ValueToken}' has the start " +
                        $"value of ^{r.Start.Value}, which is out of range (Specified Range: ^{maxRange} to {maxRange - 1})"
                    };
                    if (r.End.IsFromEnd && r.End.Value > maxRange) return new ErrorToken
                    {
                        Message = $"{(IsChannel ? "Channel" : "Index")} Out Of Range Error: Function {funcmat}, {TokenName} '{ValueToken}' has the end " +
                        $"value of ^{r.End.Value}, which is out of range (Specified Range: ^{maxRange} to {maxRange - 1})"
                    };
                    if (r.Start.Value + 1 > maxRange) return new ErrorToken
                    {
                        Message = $"{(IsChannel ? "Channel" : "Index")} Out Of Range Error: Function {funcmat}, {TokenName} '{ValueToken}' has the start " +
                        $"value of {r.Start.Value}, which is out of range (Specified Range: ^{maxRange} to {maxRange - 1})"
                    };
                    Range = r;
                    if (!r.End.IsFromEnd && r.End.Value > maxRange) Range = Range.Start..;
                    if (r.End.IsFromEnd)
                    {
                        var a = maxRange - Range.End.Value;
                        if (a < 0)
                        {
#if DEBUG
                            Debugger.Break();
#endif
                            return new ErrorToken
                            {
                                Message = $"{(IsChannel ? "Channel" : "Index")} Out Of Range Internal Error: Function {funcmat}, {TokenName} '{ValueToken}' has the end " +
                                $"value of ^{r.End.Value}, which is out of range (Specified Range: ^{maxRange} - {maxRange - 1})\n" +
                                $"Although the error is valid, this message is not intented to be displayed by the developer.\n{MathParser.ErrorReport}"
                            };
                        }
                        Range = Range.Start..a;
                    }
                    if (r.Start.IsFromEnd)
                    {
                        var a = maxRange - Range.Start.Value;
                        if (a < 0)
                        {
#if DEBUG
                            Debugger.Break();
#endif
                            return new ErrorToken
                            {
                                Message = $"{(IsChannel ? "Channel" : "Index")} Out Of Range Internal Error: Function {funcmat}, {TokenName} '{ValueToken}' has the start " +
                                $"value of ^{r.Start.Value}, which is out of range (Specified Range: ^{maxRange} - {maxRange - 1})\n" +
                                $"Although the error is valid, this message is not intented to be displayed by the developer.\n{MathParser.ErrorReport}"
                            };
                        }
                        Range = a..Range.End;
                    }
                    return null;
                }
                else
                {
                    return new ErrorToken
                    {
                        Message = $"Type Error: Function {funcmat}, {TokenName} is neither [Number (>= 0, <= x)] nor [Range (<= x)]"
                    };
                }
            }
            var m = Mat.Mat;
            if (GetRange(mat, xIn, nameof(xIn), m.Cols, IsChannel: false, out var xRange) is ErrorToken et1) return et1;
            if (GetRange(mat, yIn, nameof(yIn), m.Rows, IsChannel: false, out var yRange) is ErrorToken et2) return et2;
            if (GetRange(mat, channelIn, nameof(channelIn), m.Channels(), IsChannel: false, out var chanRange) is ErrorToken et3) return et3;
            var tracker = new ResourcesTracker();
            var channels = m.Split();
            channels.Track(tracker);
            channels = channels[chanRange];
            
            for (int i = 0; i < channels.Length; i++)
                channels[i] = channels[i][yRange, xRange].Track(tracker);
            var outmat = new Mat();
            Cv2.Merge(channels, outmat);
            return outmat.GenerateMatToken();
        }
    }
}

static partial class Extension
{
    public static IMatValueToken GenerateMatToken(this Mat m)
    {
        if (m.IsCompatableImage()) return new ImageMatToken { Mat = m };
        if (m.IsCompatableNumberMatrix()) return new MatrixMatToken { Mat = m };
        return new MatToken { Mat = m };
    }
}