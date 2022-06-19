﻿using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoToys;

static class OpenCvExtension
{
    const ColormapTypes ColorMap = ColormapTypes.Jet;
    public static double Min(this Mat m)
    {
        m.MinMaxLoc(out double MinVal, out double _);
        return MinVal;
    }
    public static double Max(this Mat m)
    {
        m.MinMaxLoc(out double _, out double MaxVal);
        return MaxVal;
    }
    public static Mat SubtractTZero(this Mat m, double d) => (m - d).ToMat().Threshold(0, -1, ThresholdTypes.Tozero);
    public static Mat Normal1(this Mat m)
    {
        using var t = new ResourcesTracker();
        return t.T(m.AsDoubles()) / m.Max();
    }
    public static Mat Normal100(this Mat m) => m.AsDoubles() * (100 / m.Max());
    public static Mat NormalBytes(this Mat m) => (m.AsDoubles() * (255 / m.Max())).ToMat().AsBytes();
    public static Mat AsType(this Mat m, MatType t)
    {
        var n = m.Clone();
        n.ConvertTo(n, t);
        return n;
    }
    public static Mat AsBytes(this Mat m) => m.AsType(MatType.CV_8UC(m.Channels()));
    public static Mat AsDoubles(this Mat m) => m.AsType(MatType.CV_64FC(m.Channels()));
    public static Mat Heatmap(this Mat m, ColormapTypes ColorMap = ColorMap)
    {
        var n = m.Clone();
        Cv2.ApplyColorMap(n, n, ColorMap);
        return n;
    }
    public static Mat AutoCanny(this Mat image, double sigma = 0.33)
    {
        // compute the median of the single channel pixel intensities
        double v = image.Mean().ToDouble();
        // apply automatic Canny edge detection using the computed median
        int lower = (int)(Math.Max(0, (1.0 - sigma) * v));

        int upper = (int)(Math.Min(255, (1.0 + sigma) * v));

        var edged = image.Canny(lower, upper);
        // return the edged image
        return edged;
    }

    public static Mat StdFilter(this Mat m, Size KernalSize)
    {
        // Reference https://stackoverflow.com/a/11459915
        using var t = new ResourcesTracker();
        return t.T(m.VarianceFilter(KernalSize)).Sqrt();
    }
        

    public static Mat VarianceFilter(this Mat m, Size KernalSize)
    {
        using var t = new ResourcesTracker();
        // Reference https://stackoverflow.com/a/11459915
        m = t.T(m.Clone());
        m.ConvertTo(m, MatType.CV_64F);
        return t.T(t.T(t.T(m.Pow(2)).Blur(KernalSize)) - t.T(t.T(m.Blur(KernalSize)).Pow(2))).Abs();
    }
    public static Mat SepiaFilter(this Mat bgr)
    {
        using var t = new ResourcesTracker();
        // Reference: https://gist.github.com/FilipeChagasDev/bb63f46278ecb4ffe5429a84926ff812
        var gray = bgr.CvtColor(ColorConversionCodes.BGR2GRAY);
        bgr = t.T(bgr.AsDoubles());
        var normalizedGray = gray.Normal1();
        var output = new Mat();
        Cv2.Merge(new Mat[]
        {
            t.T(153 * normalizedGray),
            t.T(204 * normalizedGray),
            t.T(255 * normalizedGray)
        }, output);
        return output;
    }
    public static bool IsCompatableImage(this Mat mat)
        => mat.Type().IsInteger && mat.Type().Value % 8 == 0 && (mat.Channels() == 1 || mat.Channels() == 3 || mat.Channels() == 4);
    public static bool IsCompatableNumberMatrix(this Mat mat)
        => !mat.Type().IsInteger;
    public static Mat ToBGRA(this Mat mat)
    {
        using var t = new ResourcesTracker();
        var MatType = mat.Type();
        Debug.Assert(MatType.IsInteger && MatType.Value % 8 == 0);
        switch (MatType.Channels)
        {
            case 4:
                // BGRA Already
                return mat.Clone();
            case 1:
                // Grayscale
                mat = t.T(mat.CvtColor(ColorConversionCodes.GRAY2BGR));
                goto case 3;
            case 3:
                // BGR
                return mat.CvtColor(ColorConversionCodes.BGR2BGRA);
            default:
                Debugger.Break();
                throw new ArgumentException("Weird kind of Mat", nameof(mat));
        }
    }
    public static Mat ToGray(this Mat mat)
        => mat.ToGray(out var _);
    public static Mat ToGray(this Mat mat, out Mat? alpha)
    {
        using var t = new ResourcesTracker();
        var MatType = mat.Type();
        Debug.Assert(MatType.IsInteger && MatType.Value % 8 == 0);
        switch (MatType.Channels)
        {
            case 4:
                // BGRA
                alpha = mat.ExtractChannel(3);
                return mat.CvtColor(ColorConversionCodes.BGRA2GRAY);
            case 1:
                // Grayscale
                alpha = null;
                return mat.Clone();
            case 3:
                // BGR
                alpha = null;
                return mat.CvtColor(ColorConversionCodes.BGR2GRAY);
            default:
                Debugger.Break();
                throw new ArgumentException("Weird kind of Mat", nameof(mat));
        }
    }
    public static Mat ToBGR(this Mat mat, out Mat? alpha)
    {
        using var t = new ResourcesTracker();
        var MatType = mat.Type();
        Debug.Assert(MatType.IsInteger && MatType.Value % 8 == 0);
        switch (MatType.Channels)
        {
            case 4:
                // BGRA
                alpha = mat.ExtractChannel(3);
                return mat.Clone();
            case 1:
                // Grayscale
                alpha = null;
                mat = mat.CvtColor(ColorConversionCodes.GRAY2BGR);
                goto case 3;
            case 3:
                // BGR
                alpha = null;
                return mat.Clone();
            default:
                Debugger.Break();
                throw new ArgumentException("Weird kind of Mat", nameof(mat));
        }
    }
    public static Mat ToBGRA(this Mat mat, Mat? alpha)
    {
        if (alpha == null) return mat.ToBGRA();
        using var t = new ResourcesTracker();
        var MatType = mat.Type();
        Debug.Assert(MatType.IsInteger && MatType.Value % 8 == 0);
        switch (MatType.Channels)
        {
            case 4:
                // BGRA Already
                return mat.Clone();
            case 1:
                // Grayscale
                mat = t.T(mat.CvtColor(ColorConversionCodes.GRAY2BGR));
                goto case 3;
            case 3:
                // BGR
                return mat.InsertAlpha(alpha);
            default:
                Debugger.Break();
                throw new ArgumentException("Weird kind of Mat", nameof(mat));
        }
    }
    public static Mat InsertAlpha(this Mat bgr, Mat a)
    {
        using var t = new ResourcesTracker();
        var output = new Mat();
        Cv2.Merge(
            new Mat[]
            {
                t.T(bgr.ExtractChannel(0)),
                t.T(bgr.ExtractChannel(1)),
                t.T(bgr.ExtractChannel(2)),
                t.T(a)
            },
            output
        );
        return output;
    }
}
