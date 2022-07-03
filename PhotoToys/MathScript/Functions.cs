using OpenCvSharp;
using PhotoToys;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            _ => Functions.GetValueOrDefault(Name, new ErrorToken { Message = $"The name '{Name}' is not a valid function" })
        };
    }
}

struct Abs : IFunction
{
    public IValueToken Invoke(IList<IValueToken> Parameters)
    {
        const string fn = nameof(Abs);
        const string numberOverload = $"[Number] {fn}([Number] x)";
        const string matOverload = $"[Mat] {fn}abs([Mat] x)";
        if (Parameters.FirstOrDefault(a => a is ErrorToken, null) is ErrorToken et) return et;
        if (Parameters.Count != 1) return new ErrorToken
        {
            Message = $"Parameter Error: Function {numberOverload} | {matOverload} accept 1 paramter but {Parameters.Count} was/were given"
        };
        var x = Parameters[0];
        if (x is INumberValueToken Number)
            return new NumberToken { Number = Math.Abs(Number.Number) };
        else if (x is IMatValueToken Mat)
            return new MatToken { Mat = Mat.Mat.Abs() };
        else return new ErrorToken
        {
            Message = $"Type Error: Function {numberOverload} | {matOverload}, x '{x}' should be [Number/Mat]"
        };
    }
}
struct Clamp : IFunction
{
    public IValueToken Invoke(IList<IValueToken> Parameters)
    {
        const string fn = nameof(Clamp);
        const string OverloadNumber = $"[Number] {fn}([Number] x, [Number] lowerBound, [Number] upperBound)";
        const string OverloadMatNumber = $"[Mat] {fn}([Mat] x, [Number] lowerBound, [Number] upperBound)";
        const string OverloadMatMat = $"[Mat] {fn}([Mat] x, [Mat] lowerBound, [Mat] upperBound)";
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
        const string fn = nameof(Min);
        const string OverloadNumber = $"[Number] {fn}(params [Number] Xs)";
        const string OverloadOneMat = $"[Number] {fn}([Mat] x)";
        const string OverloadMultipleMat = $"[Mat] {fn}(params [Mat] Xs)";
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
        const string fn = nameof(Max);
        const string OverloadNumber = $"[Number] {fn}(params [Number] Xs)";
        const string OverloadOneMat = $"[Number] {fn}([Mat] x)";
        const string OverloadMultipleMat = $"[Mat] {fn}(params [Mat] Xs)";
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
struct RGBReplace : IFunction
{
    public IValueToken Invoke(IList<IValueToken> Parameters)
    {
        const string func = $"[Mat] {nameof(RGBReplace)}([4-Channel Mat] rgba, [3-Channel Mat] rgb)";
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
        return new MatToken { Mat = m };
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
        var RGBA = RGBAMat.Mat.Split();

        RGBA[3].Dispose();
        RGBA[3] = AlphaMat.Mat;
        Mat m = new();
        Cv2.Merge(RGBA, m);
        RGBA.Dispose();
        return new MatToken { Mat = m };
    }
}
struct GetChannelCount : IFunction
{
    public IValueToken Invoke(IList<IValueToken> Parameters)
    {
        const string func = $"[Number] {nameof(GetChannelCount)}([Mat] mat)";
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
        const string func = $"[Number] {nameof(GetRGB)}([Mat] mat)";
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
struct GetChannel : IFunction
{
    public IValueToken Invoke(IList<IValueToken> Parameters)
    {
        const string func = $"[1-Channel Mat] {nameof(GetChannel)}([n-Channel Mat] mat, [Number (>= 0, < n)] channel)";
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
        return new MatToken { Mat = Mat.Mat.ExtractChannel(idx) };
    }
}
struct ReplaceChannel : IFunction
{
    public IValueToken Invoke(IList<IValueToken> Parameters)
    {
        const string func = $"[n-Channel Mat] {nameof(ReplaceChannel)}([n-Channel Mat] mat, [Number (>= 0, < n)] channel, [1-Channel Mat] replaceMat)";
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

        var splittedMat = Mat.Mat.Split();
        splittedMat[idx] = ReplaceMat.Mat;
        Mat m = new();
        Cv2.Merge(splittedMat, m);
        splittedMat.Dispose();
        return new MatToken { Mat = m };
    }
}
struct NormalizeTo : IFunction
{
    public IValueToken Invoke(IList<IValueToken> Parameters)
    {
        const string func = $"[Mat] {nameof(NormalizeTo)}([Mat] mat, [Number] normMax)";
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
        var m = Mat.Mat * (NormalizeMax.Number / max);
        return new MatToken { Mat = m.Track(tracker).ToMat() };
    }
}
struct ToImage : IFunction
{
    public IValueToken Invoke(IList<IValueToken> Parameters)
    {
        const string func = $"[Mat] {nameof(ToImage)}([{{1/3/4}}-Channel Mat] mat)";
        if (Parameters.FirstOrDefault(a => a is ErrorToken, null) is ErrorToken et) return et;
        if (Parameters.Count != 1) return new ErrorToken
        {
            Message = $"Parameter Error: {func} accept 1 paramter but {Parameters.Count} was/were given"
        };
        var mat = Parameters[0];
        if (mat is not IMatValueToken Mat || Mat.Mat.Channels() is not (1 or 3 or 4))
            return new ErrorToken
            {
                Message = $"Type Error: Function {func}, mat '{mat}' should be [{{1/3/4}}-Channel Mat]"
            };
        return new MatToken { Mat = Mat.Mat.AsBytes() };
    }
}