// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
