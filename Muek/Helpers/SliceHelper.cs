using System;

namespace Muek.Helpers;

public static class SliceHelper
{
    public static float[] Slice(this float[] source, int index, int length)
    {
        var result = new float[length];
        Array.Copy(source, index, result, 0, length);
        return result;
    }
}