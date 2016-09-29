// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Emit;
using Roslyn.Test.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    // Some utility functions for compiling and checking errors.
    public abstract class CompilingTestBase : CSharpTestBase
    {
        private const string DefaultTypeName = "C";
        private const string DefaultMethodName = "M";

        internal static BoundBlock ParseAndBindMethodBody(string program, string typeName = DefaultTypeName, string methodName = DefaultMethodName)
        {
            var compilation = CreateCompilationWithMscorlib(program);
            var method = (MethodSymbol)compilation.GlobalNamespace.GetTypeMembers(typeName).Single().GetMembers(methodName).Single();

            // Provide an Emit.Module so that the lowering passes will be run
            var module = new PEAssemblyBuilder(
                (SourceAssemblySymbol)compilation.Assembly,
                emitOptions: EmitOptions.Default,
                outputKind: OutputKind.ConsoleApplication,
                serializationProperties: GetDefaultModulePropertiesForSerialization(),
                manifestResources: Enumerable.Empty<ResourceDescription>());

            TypeCompilationState compilationState = new TypeCompilationState(method.ContainingType, compilation, module);

            var diagnostics = DiagnosticBag.GetInstance();
            var block = MethodCompiler.BindMethodBody(method, compilationState, diagnostics);
            diagnostics.Free();
            return block;
        }

        public static string DumpDiagnostic(Diagnostic diagnostic)
        {
            return string.Format("'{0}' {1}",
                diagnostic.Location.SourceTree.GetText().ToString(diagnostic.Location.SourceSpan),
                DiagnosticFormatter.Instance.Format(diagnostic.WithLocation(Location.None), EnsureEnglishUICulture.PreferredOrNull));
        }

        [Obsolete("Use VerifyDiagnostics", true)]
        public static void TestDiagnostics(IEnumerable<Diagnostic> diagnostics, params string[] diagStrings)
        {
            AssertEx.SetEqual(diagStrings, diagnostics.Select(DumpDiagnostic));
        }

        // Do a full compilation and check all the errors.
        [Obsolete("Use VerifyDiagnostics", true)]
        public void TestAllErrors(string code, params string[] errors)
        {
            var compilation = CreateCompilationWithMscorlib(code);
            var diagnostics = compilation.GetDiagnostics();
            AssertEx.SetEqual(errors, diagnostics.Select(DumpDiagnostic));
        }

        // Tests just the errors found while binding method M in class C.
        [Obsolete("Use VerifyDiagnostics", true)]
        public void TestErrors(string code, params string[] errors)
        {
            var compilation = CreateCompilationWithMscorlib(code);
            var method = (SourceMethodSymbol)compilation.GlobalNamespace.GetTypeMembers("C").Single().GetMembers("M").Single();
            var factory = compilation.GetBinderFactory(method.SyntaxTree);
            var bodyBlock = (BlockSyntax)method.BodySyntax;
            var parameterBinderContext = factory.GetBinder(bodyBlock);
            var binder = new ExecutableCodeBinder(bodyBlock.Parent, method, parameterBinderContext);
            var diagnostics = new DiagnosticBag();
            var block = binder.BindEmbeddedBlock(bodyBlock, diagnostics);
            AssertEx.SetEqual(errors, diagnostics.AsEnumerable().Select(DumpDiagnostic));
        }

        [Obsolete("Use VerifyDiagnostics", true)]
        public void TestWarnings(string code, params string[] expectedWarnings)
        {
            var compilation = CreateCompilationWithMscorlib(code);
            var method = (SourceMethodSymbol)compilation.GlobalNamespace.GetTypeMembers("C").Single().GetMembers("M").Single();
            var factory = compilation.GetBinderFactory(method.SyntaxTree);
            var bodyBlock = (BlockSyntax)method.BodySyntax;
            var parameterBinderContext = factory.GetBinder(bodyBlock);
            var binder = new ExecutableCodeBinder(bodyBlock.Parent, method, parameterBinderContext);
            var block = binder.BindEmbeddedBlock(bodyBlock, new DiagnosticBag());
            var actualWarnings = new DiagnosticBag();
            DiagnosticsPass.IssueDiagnostics(compilation, block, actualWarnings, method);
            AssertEx.SetEqual(expectedWarnings, actualWarnings.AsEnumerable().Select(DumpDiagnostic));
        }

        public const string LINQ =
        #region the string LINQ defines a complete LINQ API called List1<T> (for instance method) and List2<T> (for extension methods)
 @"using System;
using System.Text;

public delegate R Func1<in T1, out R>(T1 arg1);
public delegate R Func1<in T1, in T2, out R>(T1 arg1, T2 arg2);

public class List1<T>
{
    internal T[] data;
    internal int length;

    public List1(params T[] args)
    {
        this.data = (T[])args.Clone();
        this.length = data.Length;
    }

    public List1()
    {
        this.data = new T[0];
        this.length = 0;
    }

    public int Length { get { return length; } }

    //public T this[int index] { get { return this.data[index]; } }
    public T Get(int index) { return this.data[index]; }

    public virtual void Add(T t)
    {
        if (data.Length == length) Array.Resize(ref data, data.Length * 2 + 1);
        data[length++] = t;
    }

    public override string ToString()
    {
        StringBuilder builder = new StringBuilder();
        builder.Append('[');
        for (int i = 0; i < Length; i++)
        {
            if (i != 0) builder.Append(',').Append(' ');
            builder.Append(data[i]);
        }
        builder.Append(']');
        return builder.ToString();
    }

    public List1<E> Cast<E>()
    {
        E[] data = new E[Length];
        for (int i = 0; i < Length; i++)
            data[i] = (E)(object)this.data[i];
        return new List1<E>(data);
    }

    public List1<T> Where(Func1<T, bool> predicate)
    {
        List1<T> result = new List1<T>();
        for (int i = 0; i < Length; i++)
        {
            T datum = this.data[i];
            if (predicate(datum)) result.Add(datum);
        }
        return result;
    }

    public List1<U> Select<U>(Func1<T, U> selector)
    {
        int length = this.Length;
        U[] data = new U[length];
        for (int i = 0; i < length; i++) data[i] = selector(this.data[i]);
        return new List1<U>(data);
    }

    public List1<V> SelectMany<U, V>(Func1<T, List1<U>> selector, Func1<T, U, V> resultSelector)
    {
        List1<V> result = new List1<V>();
        int length = this.Length;
        for (int i = 0; i < length; i++)
        {
            T t = this.data[i];
            List1<U> selected = selector(t);
            int ulength = selected.Length;
            for (int j = 0; j < ulength; j++)
            {
                U u = selected.data[j];
                V v = resultSelector(t, u);
                result.Add(v);
            }
        }

        return result;
    }

    public List1<V> Join<U, K, V>(List1<U> inner, Func1<T, K> outerKeyselector,
        Func1<U, K> innerKeyselector, Func1<T, U, V> resultSelector)
    {
        List1<Joined<K, T, U>> joined = new List1<Joined<K, T, U>>();
        for (int i = 0; i < Length; i++)
        {
            T t = this.Get(i);
            K k = outerKeyselector(t);
            Joined<K, T, U> row = null;
            for (int j = 0; j < joined.Length; j++)
            {
                if (joined.Get(j).k.Equals(k))
                {
                    row = joined.Get(j);
                    break;
                }
            }
            if (row == null) joined.Add(row = new Joined<K, T, U>(k));
            row.t.Add(t);
        }
        for (int i = 0; i < inner.Length; i++)
        {
            U u = inner.Get(i);
            K k = innerKeyselector(u);
            Joined<K, T, U> row = null;
            for (int j = 0; j < joined.Length; j++)
            {
                if (joined.Get(j).k.Equals(k))
                {
                    row = joined.Get(j);
                    break;
                }
            }
            if (row == null) joined.Add(row = new Joined<K, T, U>(k));
            row.u.Add(u);
        }
        List1<V> result = new List1<V>();
        for (int i = 0; i < joined.Length; i++)
        {
            Joined<K, T, U> row = joined.Get(i);
            for (int j = 0; j < row.t.Length; j++)
            {
                T t = row.t.Get(j);
                for (int k = 0; k < row.u.Length; k++)
                {
                    U u = row.u.Get(k);
                    V v = resultSelector(t, u);
                    result.Add(v);
                }
            }
        }
        return result;
    }

    class Joined<K, T2, U>
    {
        public Joined(K k)
        {
            this.k = k;
            this.t = new List1<T2>();
            this.u = new List1<U>();
        }
        public readonly K k;
        public readonly List1<T2> t;
        public readonly List1<U> u;
    }

    public List1<V> GroupJoin<U, K, V>(List1<U> inner, Func1<T, K> outerKeyselector,
        Func1<U, K> innerKeyselector, Func1<T, List1<U>, V> resultSelector)
    {
        List1<Joined<K, T, U>> joined = new List1<Joined<K, T, U>>();
        for (int i = 0; i < Length; i++)
        {
            T t = this.Get(i);
            K k = outerKeyselector(t);
            Joined<K, T, U> row = null;
            for (int j = 0; j < joined.Length; j++)
            {
                if (joined.Get(j).k.Equals(k))
                {
                    row = joined.Get(j);
                    break;
                }
            }
            if (row == null) joined.Add(row = new Joined<K, T, U>(k));
            row.t.Add(t);
        }
        for (int i = 0; i < inner.Length; i++)
        {
            U u = inner.Get(i);
            K k = innerKeyselector(u);
            Joined<K, T, U> row = null;
            for (int j = 0; j < joined.Length; j++)
            {
                if (joined.Get(j).k.Equals(k))
                {
                    row = joined.Get(j);
                    break;
                }
            }
            if (row == null) joined.Add(row = new Joined<K, T, U>(k));
            row.u.Add(u);
        }
        List1<V> result = new List1<V>();
        for (int i = 0; i < joined.Length; i++)
        {
            Joined<K, T, U> row = joined.Get(i);
            for (int j = 0; j < row.t.Length; j++)
            {
                T t = row.t.Get(j);
                V v = resultSelector(t, row.u);
                result.Add(v);
            }
        }
        return result;
    }

    public OrderedList1<T> OrderBy<K>(Func1<T, K> Keyselector)
    {
        OrderedList1<T> result = new OrderedList1<T>(this);
        result.ThenBy(Keyselector);
        return result;
    }

    public OrderedList1<T> OrderByDescending<K>(Func1<T, K> Keyselector)
    {
        OrderedList1<T> result = new OrderedList1<T>(this);
        result.ThenByDescending(Keyselector);
        return result;
    }

    public List1<Group1<K, T>> GroupBy<K>(Func1<T, K> Keyselector)
    {
        List1<Group1<K, T>> result = new List1<Group1<K, T>>();
        for (int i = 0; i < Length; i++)
        {
            T t = this.Get(i);
            K k = Keyselector(t);
            Group1<K, T> Group1 = null;
            for (int j = 0; j < result.Length; j++)
            {
                if (result.Get(j).Key.Equals(k))
                {
                    Group1 = result.Get(j);
                    break;
                }
            }
            if (Group1 == null)
            {
                result.Add(Group1 = new Group1<K, T>(k));
            }
            Group1.Add(t);
        }
        return result;
    }

    public List1<Group1<K, E>> GroupBy<K, E>(Func1<T, K> Keyselector,
        Func1<T, E> elementSelector)
    {
        List1<Group1<K, E>> result = new List1<Group1<K, E>>();
        for (int i = 0; i < Length; i++)
        {
            T t = this.Get(i);
            K k = Keyselector(t);
            Group1<K, E> Group1 = null;
            for (int j = 0; j < result.Length; j++)
            {
                if (result.Get(j).Key.Equals(k))
                {
                    Group1 = result.Get(j);
                    break;
                }
            }
            if (Group1 == null)
            {
                result.Add(Group1 = new Group1<K, E>(k));
            }
            Group1.Add(elementSelector(t));
        }
        return result;
    }
}

public class OrderedList1<T> : List1<T>
{
    private List1<Keys1> Keys1;

    public override void Add(T t)
    {
        throw new NotSupportedException();
    }

    internal OrderedList1(List1<T> list)
    {
        Keys1 = new List1<Keys1>();
        for (int i = 0; i < list.Length; i++)
        {
            base.Add(list.Get(i));
            Keys1.Add(new Keys1());
        }
    }

    public OrderedList1<T> ThenBy<K>(Func1<T, K> Keyselector)
    {
        for (int i = 0; i < Length; i++)
        {
            object o = Keyselector(this.Get(i)); // work around bug 8405
            Keys1.Get(i).Add((IComparable)o);
        }
        Sort();
        return this;
    }

    class ReverseOrder : IComparable
    {
        IComparable c;
        public ReverseOrder(IComparable c)
        {
            this.c = c;
        }
        public int CompareTo(object o)
        {
            ReverseOrder other = (ReverseOrder)o;
            return other.c.CompareTo(this.c);
        }
        public override string ToString()
        {
            return String.Empty + '-' + c;
        }
    }

    public OrderedList1<T> ThenByDescending<K>(Func1<T, K> Keyselector)
    {
        for (int i = 0; i < Length; i++)
        {
            object o = Keyselector(this.Get(i)); // work around bug 8405
            Keys1.Get(i).Add(new ReverseOrder((IComparable)o));
        }
        Sort();
        return this;
    }

    void Sort()
    {
        Array.Sort(this.Keys1.data, this.data, 0, Length);
    }
}

class Keys1 : List1<IComparable>, IComparable
{
    public int CompareTo(object o)
    {
        Keys1 other = (Keys1)o;
        for (int i = 0; i < Length; i++)
        {
            int c = this.Get(i).CompareTo(other.Get(i));
            if (c != 0) return c;
        }
        return 0;
    }
}

public class Group1<K, T> : List1<T>
{
    public Group1(K k, params T[] data)
        : base(data)
    {
        this.Key = k;
    }

    public K Key { get; private set; }

    public override string ToString()
    {
        return Key + String.Empty + ':' + base.ToString();
    }
}

//public delegate R Func2<in T1, out R>(T1 arg1);
//public delegate R Func2<in T1, in T2, out R>(T1 arg1, T2 arg2);
//
//public class List2<T>
//{
//    internal T[] data;
//    internal int length;
//
//    public List2(params T[] args)
//    {
//        this.data = (T[])args.Clone();
//        this.length = data.Length;
//    }
//
//    public List2()
//    {
//        this.data = new T[0];
//        this.length = 0;
//    }
//
//    public int Length { get { return length; } }
//
//    //public T this[int index] { get { return this.data[index]; } }
//    public T Get(int index) { return this.data[index]; }
//
//    public virtual void Add(T t)
//    {
//        if (data.Length == length) Array.Resize(ref data, data.Length * 2 + 1);
//        data[length++] = t;
//    }
//
//    public override string ToString()
//    {
//        StringBuilder builder = new StringBuilder();
//        builder.Append('[');
//        for (int i = 0; i < Length; i++)
//        {
//            if (i != 0) builder.Append(',').Append(' ');
//            builder.Append(data[i]);
//        }
//        builder.Append(']');
//        return builder.ToString();
//    }
//
//}
//
//public class OrderedList2<T> : List2<T>
//{
//    internal List2<Keys2> Keys2;
//
//    public override void Add(T t)
//    {
//        throw new NotSupportedException();
//    }
//
//    internal OrderedList2(List2<T> list)
//    {
//        Keys2 = new List2<Keys2>();
//        for (int i = 0; i < list.Length; i++)
//        {
//            base.Add(list.Get(i));
//            Keys2.Add(new Keys2());
//        }
//    }
//
//    internal void Sort()
//    {
//        Array.Sort(this.Keys2.data, this.data, 0, Length);
//    }
//}
//
//class Keys2 : List2<IComparable>, IComparable
//{
//    public int CompareTo(object o)
//    {
//        Keys2 other = (Keys2)o;
//        for (int i = 0; i < Length; i++)
//        {
//            int c = this.Get(i).CompareTo(other.Get(i));
//            if (c != 0) return c;
//        }
//        return 0;
//    }
//}
//
//public class Group2<K, T> : List2<T>
//{
//    public Group2(K k, params T[] data)
//        : base(data)
//    {
//        this.Key = k;
//    }
//
//    public K Key { get; private set; }
//
//    public override string ToString()
//    {
//        return Key + String.Empty + ':' + base.ToString();
//    }
//}
//
//public static class Extensions2
//{
//
//    public static List2<E> Cast<T, E>(this List2<T> _this)
//    {
//        E[] data = new E[_this.Length];
//        for (int i = 0; i < _this.Length; i++)
//            data[i] = (E)(object)_this.data[i];
//        return new List2<E>(data);
//    }
//
//    public static List2<T> Where<T>(this List2<T> _this, Func2<T, bool> predicate)
//    {
//        List2<T> result = new List2<T>();
//        for (int i = 0; i < _this.Length; i++)
//        {
//            T datum = _this.data[i];
//            if (predicate(datum)) result.Add(datum);
//        }
//        return result;
//    }
//
//    public static List2<U> Select<T,U>(this List2<T> _this, Func2<T, U> selector)
//    {
//        int length = _this.Length;
//        U[] data = new U[length];
//        for (int i = 0; i < length; i++) data[i] = selector(_this.data[i]);
//        return new List2<U>(data);
//    }
//
//    public static List2<V> SelectMany<T, U, V>(this List2<T> _this, Func2<T, List2<U>> selector, Func2<T, U, V> resultSelector)
//    {
//        List2<V> result = new List2<V>();
//        int length = _this.Length;
//        for (int i = 0; i < length; i++)
//        {
//            T t = _this.data[i];
//            List2<U> selected = selector(t);
//            int ulength = selected.Length;
//            for (int j = 0; j < ulength; j++)
//            {
//                U u = selected.data[j];
//                V v = resultSelector(t, u);
//                result.Add(v);
//            }
//        }
//
//        return result;
//    }
//
//    public static List2<V> Join<T, U, K, V>(this List2<T> _this, List2<U> inner, Func2<T, K> outerKeyselector,
//        Func2<U, K> innerKeyselector, Func2<T, U, V> resultSelector)
//    {
//        List2<Joined<K, T, U>> joined = new List2<Joined<K, T, U>>();
//        for (int i = 0; i < _this.Length; i++)
//        {
//            T t = _this.Get(i);
//            K k = outerKeyselector(t);
//            Joined<K, T, U> row = null;
//            for (int j = 0; j < joined.Length; j++)
//            {
//                if (joined.Get(j).k.Equals(k))
//                {
//                    row = joined.Get(j);
//                    break;
//                }
//            }
//            if (row == null) joined.Add(row = new Joined<K, T, U>(k));
//            row.t.Add(t);
//        }
//        for (int i = 0; i < inner.Length; i++)
//        {
//            U u = inner.Get(i);
//            K k = innerKeyselector(u);
//            Joined<K, T, U> row = null;
//            for (int j = 0; j < joined.Length; j++)
//            {
//                if (joined.Get(j).k.Equals(k))
//                {
//                    row = joined.Get(j);
//                    break;
//                }
//            }
//            if (row == null) joined.Add(row = new Joined<K, T, U>(k));
//            row.u.Add(u);
//        }
//        List2<V> result = new List2<V>();
//        for (int i = 0; i < joined.Length; i++)
//        {
//            Joined<K, T, U> row = joined.Get(i);
//            for (int j = 0; j < row.t.Length; j++)
//            {
//                T t = row.t.Get(j);
//                for (int k = 0; k < row.u.Length; k++)
//                {
//                    U u = row.u.Get(k);
//                    V v = resultSelector(t, u);
//                    result.Add(v);
//                }
//            }
//        }
//        return result;
//    }
//
//    class Joined<K, T2, U>
//    {
//        public Joined(K k)
//        {
//            this.k = k;
//            this.t = new List2<T2>();
//            this.u = new List2<U>();
//        }
//        public readonly K k;
//        public readonly List2<T2> t;
//        public readonly List2<U> u;
//    }
//
//    public static List2<V> GroupJoin<T, U, K, V>(this List2<T> _this, List2<U> inner, Func2<T, K> outerKeyselector,
//        Func2<U, K> innerKeyselector, Func2<T, List2<U>, V> resultSelector)
//    {
//        List2<Joined<K, T, U>> joined = new List2<Joined<K, T, U>>();
//        for (int i = 0; i < _this.Length; i++)
//        {
//            T t = _this.Get(i);
//            K k = outerKeyselector(t);
//            Joined<K, T, U> row = null;
//            for (int j = 0; j < joined.Length; j++)
//            {
//                if (joined.Get(j).k.Equals(k))
//                {
//                    row = joined.Get(j);
//                    break;
//                }
//            }
//            if (row == null) joined.Add(row = new Joined<K, T, U>(k));
//            row.t.Add(t);
//        }
//        for (int i = 0; i < inner.Length; i++)
//        {
//            U u = inner.Get(i);
//            K k = innerKeyselector(u);
//            Joined<K, T, U> row = null;
//            for (int j = 0; j < joined.Length; j++)
//            {
//                if (joined.Get(j).k.Equals(k))
//                {
//                    row = joined.Get(j);
//                    break;
//                }
//            }
//            if (row == null) joined.Add(row = new Joined<K, T, U>(k));
//            row.u.Add(u);
//        }
//        List2<V> result = new List2<V>();
//        for (int i = 0; i < joined.Length; i++)
//        {
//            Joined<K, T, U> row = joined.Get(i);
//            for (int j = 0; j < row.t.Length; j++)
//            {
//                T t = row.t.Get(j);
//                V v = resultSelector(t, row.u);
//                result.Add(v);
//            }
//        }
//        return result;
//    }
//
//    public static OrderedList2<T> OrderBy<T, K>(this List2<T> _this, Func2<T, K> Keyselector)
//    {
//        OrderedList2<T> result = new OrderedList2<T>(_this);
//        result.ThenBy(Keyselector);
//        return result;
//    }
//
//    public static OrderedList2<T> OrderByDescending<T, K>(this List2<T> _this, Func2<T, K> Keyselector)
//    {
//        OrderedList2<T> result = new OrderedList2<T>(_this);
//        result.ThenByDescending(Keyselector);
//        return result;
//    }
//
//    public static List2<Group2<K, T>> GroupBy<T, K>(this List2<T> _this, Func2<T, K> Keyselector)
//    {
//        List2<Group2<K, T>> result = new List2<Group2<K, T>>();
//        for (int i = 0; i < _this.Length; i++)
//        {
//            T t = _this.Get(i);
//            K k = Keyselector(t);
//            Group2<K, T> Group2 = null;
//            for (int j = 0; j < result.Length; j++)
//            {
//                if (result.Get(j).Key.Equals(k))
//                {
//                    Group2 = result.Get(j);
//                    break;
//                }
//            }
//            if (Group2 == null)
//            {
//                result.Add(Group2 = new Group2<K, T>(k));
//            }
//            Group2.Add(t);
//        }
//        return result;
//    }
//
//    public static List2<Group2<K, E>> GroupBy<T, K, E>(this List2<T> _this, Func2<T, K> Keyselector,
//        Func2<T, E> elementSelector)
//    {
//        List2<Group2<K, E>> result = new List2<Group2<K, E>>();
//        for (int i = 0; i < _this.Length; i++)
//        {
//            T t = _this.Get(i);
//            K k = Keyselector(t);
//            Group2<K, E> Group2 = null;
//            for (int j = 0; j < result.Length; j++)
//            {
//                if (result.Get(j).Key.Equals(k))
//                {
//                    Group2 = result.Get(j);
//                    break;
//                }
//            }
//            if (Group2 == null)
//            {
//                result.Add(Group2 = new Group2<K, E>(k));
//            }
//            Group2.Add(elementSelector(t));
//        }
//        return result;
//    }
//
//    public static OrderedList2<T> ThenBy<T, K>(this OrderedList2<T> _this, Func2<T, K> Keyselector)
//    {
//        for (int i = 0; i < _this.Length; i++)
//        {
//            object o = Keyselector(_this.Get(i)); // work around bug 8405
//            _this.Keys2.Get(i).Add((IComparable)o);
//        }
//        _this.Sort();
//        return _this;
//    }
//
//    class ReverseOrder : IComparable
//    {
//        IComparable c;
//        public ReverseOrder(IComparable c)
//        {
//            this.c = c;
//        }
//        public int CompareTo(object o)
//        {
//            ReverseOrder other = (ReverseOrder)o;
//            return other.c.CompareTo(this.c);
//        }
//        public override string ToString()
//        {
//            return String.Empty + '-' + c;
//        }
//    }
//
//    public static OrderedList2<T> ThenByDescending<T, K>(this OrderedList2<T> _this, Func2<T, K> Keyselector)
//    {
//        for (int i = 0; i < _this.Length; i++)
//        {
//            object o = Keyselector(_this.Get(i)); // work around bug 8405
//            _this.Keys2.Get(i).Add(new ReverseOrder((IComparable)o));
//        }
//        _this.Sort();
//        return _this;
//    }
//
//}
"
        #endregion the string LINQ
;
    }
}
