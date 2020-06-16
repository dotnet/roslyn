// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

public delegate byte ModDele(sbyte p1, ref string p2);

public enum ModEnum : ulong
{
    None = 0, Red = 255, White = 255255, Blue=1
}

public interface ModIBase
{
    object ReadOnlyProp { get; }
    object M(ref object p1, out object p2, params object[] ary);
}

namespace NS.Module
{
    public interface ModIDerive : ModIBase
    {
        object this[object p] { get; set; }
    }

    public struct ModStruct
    {
        public ModStruct(ModClass p = default(ModClass))
            : this()
        {
            SField = p;
        }
        private ModClass SField;
        internal ushort SProp { get; set; }
        public ulong SM() { return 0; }
    }

    public class ModClass
    {
        private short prop = 0;
        public short CProp { get { return prop; } set { prop = value; } }
        public ModIDerive CM(int p1 = 0, uint p2 = 1, long p3 = 2) { return null; }
        public string this[string s] { get { return s; } }
    }
}

public static class StaticModClass
{
}
