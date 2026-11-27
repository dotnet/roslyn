// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Xunit;

public static class TheoryDataExtensions
{
    public static void Add<T1, T2>(this TheoryData<T1, T2> data, (T1, T2) values)
    {
        data.Add(values.Item1, values.Item2);
    }

    public static void Add<T1, T2, T3>(this TheoryData<T1, T2, T3> data, (T1, T2, T3) values)
    {
        data.Add(values.Item1, values.Item2, values.Item3);
    }

    public static void Add<T1, T2, T3, T4>(this TheoryData<T1, T2, T3, T4> data, (T1, T2, T3, T4) values)
    {
        data.Add(values.Item1, values.Item2, values.Item3, values.Item4);
    }

    public static void Add<T1, T2, T3, T4, T5>(this TheoryData<T1, T2, T3, T4, T5> data, (T1, T2, T3, T4, T5) values)
    {
        data.Add(values.Item1, values.Item2, values.Item3, values.Item4, values.Item5);
    }
}

