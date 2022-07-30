﻿using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace PTMS;

public static class OpenCvExtension
{
    public static T Assign<T>(this T item, out T t)
    {
        t = item;
        return item;
    }
    public static T Edit<T>(this T item, Action<T> t)
    {
        t.Invoke(item);
        return item;
    }
    const ColormapTypes ColorMap = ColormapTypes.Jet;
    public static TCvObject Track<TCvObject>(this TCvObject mat, ResourcesTracker tracker) where TCvObject : DisposableObject
        => tracker.T(mat);
    public static TCvObject[] Track<TCvObject>(this TCvObject[] mats, ResourcesTracker tracker) where TCvObject : DisposableObject
    {
        foreach (var mat in mats)
            tracker.T(mat);
        return mats;
    }
    public static void Dispose<TCvObject>(this IEnumerable<TCvObject> mats) where TCvObject : DisposableObject
    {
        foreach (var mat in mats)
            mat.Dispose();
    }
    public static Mat InplaceCvtColor(this Mat src, ColorConversionCodes code, int dstCn = 0)
    {
        Cv2.CvtColor(src, src, code, dstCn);
        return src;
    }
    public static TCvObject Track<TCvObject>(this TCvObject mat, out ResourcesTracker tracker) where TCvObject : DisposableObject
    {
        tracker = new ResourcesTracker();
        return tracker.T(mat);
    }
    public static T DisposeTracker<T>(this T item, ResourcesTracker tracker)
    {
        tracker.Dispose();
        return item;
    }
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
    public static Mat SubtractTZero(this Mat m, double d)
    {
        using var tracker = new ResourcesTracker();
        return (m - d).Track(tracker).ToMat().Track(tracker).Threshold(0, -1, ThresholdTypes.Tozero);
    }
    public static Mat Magnitude(this Mat m)
    {
        using var tracker = new ResourcesTracker();
        var channels = m.Split().Track(tracker);
        return channels.Aggregate((a, b) => a.Mul(b).Track(tracker)).Clone();
    }
    public static Mat Normal1(this Mat m)
    {
        using var tracker = new ResourcesTracker();
        return m.AsDoubles().Track(tracker) / m.Max();
    }
    public static Mat Normal100(this Mat m)
    {
        using var tracker = new ResourcesTracker();
        return m.AsDoubles().Track(tracker) * (100 / m.Max());
    }
    public static Mat NormalBytes(this Mat m)
    {
        using var tracker = new ResourcesTracker();
        return (m.AsDoubles().Track(tracker) * (255 / m.Max())).Track(tracker).ToMat().InplaceAsBytes();
    }
    public static Mat AsType(this Mat m, MatType t)
    {
        var n = m.Clone();
        n.ConvertTo(n, t);
        return n;
    }
    public static Mat InplaceAsType(this Mat m, MatType t)
    {
        m.ConvertTo(m, t);
        return m;
    }
    public static Mat AsBytes(this Mat m) => m.AsType(MatType.CV_8UC(m.Channels()));
    public static Mat InplaceAsBytes(this Mat m) => m.InplaceAsType(MatType.CV_8UC(m.Channels()));
    public static Mat InplaceAsDoubles(this Mat m) => m.InplaceAsType(MatType.CV_64FC(m.Channels()));
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
        using var tracker = new ResourcesTracker();
        return m.VarianceFilter(KernalSize).Track(tracker).Sqrt();
    }
        

    public static Mat VarianceFilter(this Mat m, Size KernalSize)
    {
        using var tracker = new ResourcesTracker();
        // Reference https://stackoverflow.com/a/11459915
        m = m.Clone().Track(tracker);
        m.ConvertTo(m, MatType.CV_64F);
        return (
                m.Pow(2).Track(tracker).Blur(KernalSize).Track(tracker) -
                m.Blur(KernalSize).Track(tracker).Pow(2).Track(tracker)
            ).Track(tracker).Abs();
    }
    public static Mat SepiaFilter(this Mat bgr)
    {
        using var tracker = new ResourcesTracker();
        // Reference: https://gist.github.com/FilipeChagasDev/bb63f46278ecb4ffe5429a84926ff812
        var gray = bgr.CvtColor(ColorConversionCodes.BGR2GRAY).Track(tracker);
        bgr = bgr.AsDoubles().Track(tracker);
        var normalizedGray = gray.Normal1().Track(tracker);
        var output = new Mat();
        Cv2.Merge(new Mat[]
        {
            (153 * normalizedGray).Track(tracker),
            (204 * normalizedGray).Track(tracker),
            (255 * normalizedGray).Track(tracker)
        }, output);
        return output;
    }
    public static bool IsCompatableImage(this Mat mat)
        => mat.Type().Assign(out var type).IsInteger &&
        type.Value % 8 == 0 &&
        (mat.Channels().Assign(out var channel) == 1 || channel == 3 || channel == 4);
    public static bool IsCompatableNumberMatrix(this Mat mat)
        => !mat.Type().IsInteger;
    public static Mat ToBGRA(this Mat mat)
    {
        using var tracker = new ResourcesTracker();
        var MatType = mat.Type();
        Debug.Assert(MatType.IsInteger && MatType.Value % 8 == 0);
        switch (MatType.Channels)
        {
            case 4:
                // BGRA Already
                return mat.Clone();
            case 1:
                // Grayscale
                mat = mat.CvtColor(ColorConversionCodes.GRAY2BGR).Track(tracker);
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
        => mat.ToGray(out var a).Edit(_ => a?.Dispose());
    public static Mat ToGray(this Mat mat, out Mat? alpha)
    {
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
    public static Mat InplaceToGray(this Mat mat)
        => mat.InplaceToGray(out var a).Edit(_ => a?.Dispose());
    public static Mat InplaceToGray(this Mat mat, out Mat? alpha)
    {
        var MatType = mat.Type();
        Debug.Assert(MatType.IsInteger && MatType.Value % 8 == 0);
        switch (MatType.Channels)
        {
            case 4:
                // BGRA
                alpha = mat.ExtractChannel(3);
                return mat.InplaceCvtColor(ColorConversionCodes.BGRA2GRAY);
            case 1:
                // Grayscale
                alpha = null;
                return mat;
            case 3:
                // BGR
                alpha = null;
                return mat.InplaceCvtColor(ColorConversionCodes.BGR2GRAY);
            default:
                Debugger.Break();
                throw new ArgumentException("Weird kind of Mat", nameof(mat));
        }
    }
    public static Mat InplaceToBGR(this Mat mat)
        => mat.InplaceToBGR(out var a).Edit(_ => a?.Dispose());
    public static Mat InplaceToBGRA(this Mat mat)
    {
        using var Tracker = new ResourcesTracker();
        var MatType = mat.Type();
        Debug.Assert(MatType.IsInteger && MatType.Value % 8 == 0);
        switch (MatType.Channels)
        {
            case 4:
                // BGRA
                return mat;
            case 1:
                // Grayscale
                return mat.InplaceCvtColor(ColorConversionCodes.GRAY2BGRA);
            case 3:
                // BGR
                return mat.InplaceCvtColor(ColorConversionCodes.BGR2BGRA);
            default:
                Debugger.Break();
                throw new ArgumentException("Weird kind of Mat", nameof(mat));
        }
    }
    public static Mat InplaceToBGR(this Mat mat, out Mat? alpha)
    {
        using var Tracker = new ResourcesTracker();
        var MatType = mat.Type();
        Debug.Assert(MatType.IsInteger && MatType.Value % 8 == 0);
        switch (MatType.Channels)
        {
            case 4:
                // BGRA
                alpha = mat.ExtractChannel(3);
                return mat.InplaceCvtColor(ColorConversionCodes.BGRA2BGR);
            case 1:
                // Grayscale
                alpha = null;
                return mat.InplaceCvtColor(ColorConversionCodes.GRAY2BGR);
            case 3:
                // BGR
                alpha = null;
                return mat;
            default:
                Debugger.Break();
                throw new ArgumentException("Weird kind of Mat", nameof(mat));
        }
    }
    public static Mat ToBGR(this Mat mat) => mat.ToBGR(out var a).Edit(_ => a?.Dispose());
    public static Mat ToBGR(this Mat mat, out Mat? alpha)
    {
        using var Tracker = new ResourcesTracker();
        var MatType = mat.Type();
        Debug.Assert(MatType.IsInteger && MatType.Value % 8 == 0);
        switch (MatType.Channels)
        {
            case 4:
                // BGRA
                alpha = mat.ExtractChannel(3);
                return mat.CvtColor(ColorConversionCodes.BGRA2BGR);
            case 1:
                // Grayscale
                alpha = null;
                return mat.CvtColor(ColorConversionCodes.GRAY2BGR);
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
        using var tracker = new ResourcesTracker();
        var MatType = mat.Type();
        Debug.Assert(MatType.IsInteger && MatType.Value % 8 == 0);
        switch (MatType.Channels)
        {
            case 4:
                // BGRA Already
                return mat.Clone();
            case 1:
                // Grayscale
                mat = mat.CvtColor(ColorConversionCodes.GRAY2BGR).Track(tracker);
                goto case 3;
            case 3:
                // BGR
                return mat.InsertAlpha(alpha);
            default:
                Debugger.Break();
                throw new ArgumentException("Weird kind of Mat", nameof(mat));
        }
    }
    public static Mat InsertAlpha(this Mat bgr, Mat? a)
    {
        if (a == null) return bgr.CvtColor(ColorConversionCodes.BGR2BGRA);
        using var tracker = new ResourcesTracker();
        var output = new Mat();
        Cv2.Merge(
            new Mat[]
            {
                bgr.ExtractChannel(0).Track(tracker),
                bgr.ExtractChannel(1).Track(tracker),
                bgr.ExtractChannel(2).Track(tracker),
                a
            },
            output
        );
        return output;
    }
    public static Mat InplaceInsertAlpha(this Mat mat, Mat? a)
    {
        if (a == null) return mat.InplaceCvtColor(ColorConversionCodes.BGR2BGRA);
        using var tracker = new ResourcesTracker();
        Cv2.Merge(
            new Mat[]
            {
                mat.ExtractChannel(0).Track(tracker),
                mat.ExtractChannel(1).Track(tracker),
                mat.ExtractChannel(2).Track(tracker),
                a
            },
            mat
        );
        return mat;
    }
    public static bool IsIdenticalInSizeAndChannel(this Mat m1, Mat m2)
        => m1.Channels() == m2.Channels() && m1.IsIdenticalInSize(m2);
    public static bool IsIdenticalInSize(this Mat m1, Mat m2)
        => m1.Rows == m2.Rows && m1.Cols == m2.Cols;
    public static Mat SubMat(this Mat m, System.Range? yRange = null, System.Range? xRange = null, System.Range? chanRange = null)
    {
        return m.SubMat(yRange: yRange ?? .., xRange: xRange ?? .., chanRange: chanRange ?? ..);
    }
    public static Mat SubMat(this Mat m, System.Range yRange, System.Range xRange, System.Range chanRange)
    {
        var tracker = new ResourcesTracker();
        var channels = m.Split();
        channels.Track(tracker);
        channels = channels[chanRange];

        for (int i = 0; i < channels.Length; i++)
            channels[i] = channels[i][yRange, xRange].Track(tracker);
        var m1 = new Mat();
        Cv2.Merge(channels, m1);
        return m1;
    }
    public static Mat Merge(this Mat[] channels)
    {
        Mat m = new();
        Cv2.Merge(channels, m);
        return m;
    }
    public static T[] InplaceSelect<T>(this T[] ts, Func<T, T> func)
    {
        for (int i = 0; i < ts.Length; i++)
            ts[i] = func.Invoke(ts[i]);
        return ts;
    }
    public static int Round(this double value) => (int)Math.Round(value);
    public static Mat RotateAndScale(this Mat m, double Angle, double Scale = 1)
    {
        var l = Angle % 90 == 0 ? Math.Max(m.Width, m.Height) : Math.Sqrt(Math.Pow(m.Width, 2) + Math.Pow(m.Height, 2));
        var hor = ((l - m.Width) / 2).Round();
        var vert = ((l - m.Height) / 2).Round();
        var scalel = (l * Scale).Round();
        if (scalel == 0)
        {
            return new Mat(new Size(1, 1), MatType.CV_8UC4, 0);
        }
        using var tracker = new ResourcesTracker();

        var m1 = new Mat(new Size(l, l), MatType.CV_8UC4);
        m1.SetTo(0);
        var mbgra = m.ToBGRA();
        m1[vert..(vert + m.Height), hor..(hor + m.Width)] = mbgra.Track(tracker);
        m1 = m1.Track(tracker).Resize(new Size(scalel, scalel));
        //var m2 = new Mat(new Size(l, l), MatType.CV_8UC4);
        //m2.SetTo(0);
        //m2[vert..(vert + m.Height), hor..(hor + m.Width)] = mbgra.EmptyClone();
        //m2 = m2.Track(tracker).Resize(new Size(scalel, scalel));

        var rot_mat = Cv2.GetRotationMatrix2D(new Point2f(scalel / 2, scalel / 2), Angle, 1).Track(tracker);
        return m1.Track(tracker).WarpAffine(rot_mat, new Size(scalel, scalel));
    }
    enum EdgeModes
    {
        AdaptiveThreshold,
        StandardDeviation
    }
    public static Mat CartoonFilter(this Mat Source, double Intensity, int NoiseReduce = 1, int EdgeBlockSize = 9, int ColorSmoothness = 9, bool EdgeOnly = false)
    {
        using var tracker = new ResourcesTracker();
        Source = Source.ToBGR(out var alpha).Track(tracker);
        alpha?.Track(tracker);
        // Reference: Modified from https://github.com/ethand91/opencv-cartoon-filter/blob/master/main.py

        var gray = Source.ToGray().Track(tracker);
        if (NoiseReduce is > -1)
        {
            Cv2.MedianBlur(gray, gray, ksize: NoiseReduce);
        }
        Mat edges = gray.AdaptiveThreshold(
            maxValue: 255,
            adaptiveMethod: AdaptiveThresholdTypes.MeanC,
            thresholdType: ThresholdTypes.Binary,
            blockSize: EdgeBlockSize,
            c: 9
        );

        Mat output = new Mat();
        if (EdgeOnly)
        {
            output = edges.ToBGR();
        }
        else
        {
            var color = Source.BilateralFilter(
                d: ColorSmoothness,
                sigmaColor: 200,
                sigmaSpace: 200
            ).Track(tracker);

            Mat m = tracker.NewMat();
            Cv2.BitwiseAnd(
                src1: color,
                src2: color,
                dst: output,
                mask: edges
            );
        }

        Cv2.AddWeighted(output, Intensity, Source, 1 - Intensity, 0, output);

        return output.InplaceInsertAlpha(alpha);
    }
    public static Scalar ToScalar(this double Number, int times)
        => times switch
        {
            1 => new Scalar(Number),
            2 => new Scalar(Number, Number),
            3 => new Scalar(Number, Number, Number),
            4 => new Scalar(Number, Number, Number, Number),
            _ => throw new ArgumentException()
        };
}
