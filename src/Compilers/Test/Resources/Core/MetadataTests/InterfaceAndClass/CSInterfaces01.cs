// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Metadata
{
    public enum EGoo { Zero, One, Two, Three }
    public delegate void DGoo<T>(T p1, T p2);

    public interface ICSProp
    {
        EGoo ReadOnlyProp { get; }
        EGoo WriteOnlyProp { set; }
        EGoo ReadWriteProp { get; set; }
    }

    public interface ICSGen<T, V>
    {
        void M01(T p1, T p2);
        void M01(T p1, params T[] ary);
        void M01(params T[] ary);
        void M01(T p1, ref T p2, out DGoo<T> p3);

        string M01(V p1, V p2);
        string M01(V p1, object p2);
        string M01(V p1, params object[] p2);
    }
}
