// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;

namespace NS.Module.CS01
{
    public abstract class ModChainA
    {
        public static string StaticField = "Static String Constant";
        public abstract EventLog MA01(EventArgs arg = default(EventArgs));
        public virtual object MA02(ref NS.Module.ModStruct p1, ulong p2 = 123 + 321) { return null; }
        public virtual NS.Module.ModIDerive MA03(short p1, EventArgs p2 = null, string p3 = "Optional.String.Constant") { return null; }
    }

    public class ModChainB : ModChainA
    {
        internal readonly string ReadonlyField;
        public ModChainB()
        {
            ReadonlyField = "Readonly-String_Constant";
        }
        public NS.Module.ModStruct Prop { get; set; }
        public override EventLog MA01(EventArgs arg = null) { return null; }
        public new object MA02(ref NS.Module.ModStruct p1, ulong p2 = 0) { return null; }
    }

    public sealed class ModChainC : ModChainB
    {
        volatile int VolatileField = 123;
        // overload
        public object MA02(ref NS.Module.ModStruct p1, out ulong p2, uint p3 = 0) { p2 = 0; return null; }
        public override NS.Module.ModIDerive MA03(short p1, EventArgs p2 = default(EventArgs), string p3 = "Override Optional.String") { return null; }
    }

    public static class Extension
    {
        public static void ExtModChainA01(this ModChainA p1, string p2) { }
        public static void ExtModChainC01(this ModChainC p1, object p2) { }
    }

    public delegate void GenDele<T>(T t);
    namespace CS02
    {
        public interface ModIGen2<T, R> where T : class where R : class
        {
            R P01 { get; set; }
            R M01(ref T t);
            object this[T t] { get; }
            event Func<T, T, R> E01;
            event GenDele<T> E02;
        }

        public class ModClassImplImp<T> : ModIGen2<T, Action<T>> where T: class
        {
            public Action<T> P01 { get { return null; } set { } }
            public Action<T> M01(ref T t) { return new Action<T>(MyAction); }
            public object this[T t] { get { return null; } }
            public event Func<T, T, Action<T>> E01
            {
                add { _E01 += value; }
                remove { _E01 -= value; }
            }
            public event GenDele<T> E02
            {
                add { _E02 += value; }
                remove { _E02 -= value; }
            }
            Func<T, T, Action<T>> _E01;
            GenDele<T> _E02;
            void MyAction(T t) { }

            internal void GenM<X>(X x) { }
        }

        public struct ModStructImplExp : ModIGen2<Expression, object>
        {
            object ModIGen2<Expression, object>.P01 { get; set; }
            object ModIGen2<Expression, object>.M01(ref Expression t) { return null; }
            object ModIGen2<Expression, object>.this[Expression t] { get { return null; } }
            event Func<Expression, Expression, object> ModIGen2<Expression, object>.E01
            {
                add { _E01 += value; }
                remove { _E01 -= value; }
            }
            event GenDele<Expression> ModIGen2<Expression, object>.E02
            {
                add { _E02 += value; }
                remove { _E02 -= value; }
            }
            Func<Expression, Expression, object> _E01;
            GenDele<Expression> _E02;
        }
    }

}
