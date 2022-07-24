using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml.Media.Imaging;
using OpenCvSharp;
using Windows.Storage.Streams;

namespace PhotoToys;

static class Extension
{
    public static T Assign<T>(this T item, out T var)
    {
        var = item;
        return item;
    }
    public static T Edit<T>(this T item, Action<T>? func)
    {
        func?.Invoke(item);
        return item;
    }
    public static TOut Apply<TIn,TOut>(this TIn item, Func<TIn, TOut> func)
    {
        return func.Invoke(item);
    }
    public static void AddRange<T>(this IList<T> list, IEnumerable<T> items)
    {
        foreach (var item in items)
            list.Add(item);
    }
    public static IEnumerable<T> AsSingleEnumerable<T>(this T item)
    {
        yield return item;
    }
    public static IEnumerable<T> AsDoubleEnumerable<T>(this T item1, T item2)
    {
        yield return item1;
        yield return item2;
    }
    public static BitmapImage ToBitmapImage(this Mat m, bool DisposeMat = false)
    {

        BitmapImage bmp = m.ToBytes().AsBitmapImage();

        if (DisposeMat) m.Dispose();

        return bmp;
    }
    public static BitmapImage AsBitmapImage(this byte[] byteArray)
    {
        using var stream = new InMemoryRandomAccessStream();
        stream.WriteAsync(byteArray.AsBuffer()).GetResults();
        // I made this one synchronous on the UI thread;
        // this is not a best practice.
        var image = new BitmapImage();
        stream.Seek(0);
        image.SetSource(stream);
        return image;
    }
    public static IEnumerable<(int Index, T Value)> Enumerate<T>(this IEnumerable<T> Enumerable)
    {
        int i = 0;
        foreach (var a in Enumerable)
            yield return (i++, a);
    }
    public static string ToReadableName(this string codeName)
        => string.Join("", codeName.SelectMany(x => char.IsUpper(x) ? ' '.AsDoubleEnumerable(x) : x.AsSingleEnumerable()).Skip(1));
}
