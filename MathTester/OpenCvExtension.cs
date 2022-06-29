using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoToys;

static class OpenCvExtension
{
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
}
