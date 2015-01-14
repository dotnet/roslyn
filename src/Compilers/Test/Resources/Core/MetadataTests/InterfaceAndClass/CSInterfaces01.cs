// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Metadata
{
    public enum EFoo { Zero, One, Two, Three }
    public delegate void DFoo<T>(T p1, T p2);

    public interface ICSProp
    {
        EFoo ReadOnlyProp { get; }
        EFoo WriteOnlyProp { set; }
        EFoo ReadWriteProp { get; set; }
    }

    public interface ICSGen<T, V>
    {
        void M01(T p1, T p2);
        void M01(T p1, params T[] ary);
        void M01(params T[] ary);
        void M01(T p1, ref T p2, out DFoo<T> p3);

        string M01(V p1, V p2);
        string M01(V p1, object p2);
        string M01(V p1, params object[] p2);
    }
}
