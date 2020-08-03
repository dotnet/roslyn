// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Test.Extensions;
using Roslyn.Test.Utilities;
using Xunit;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    /// <summary>
    /// These tests step through source text character-by-character,
    /// checking the results of LookupSymbols at each position.
    /// </summary>
    public class LookupPositionTests : CompilingTestBase
    {
        private const char KeyPositionMarker = '`';

        [Fact]
        public void PositionalRecord1()
        {
            var text = @"
record C(int x, int y)
`{
`}";
            var expectedNames = MakeExpectedSymbols(
                Add( // Global
                    "System",
                    "Microsoft",
                    "C"),
                Add( // Members
                    "System.Boolean C.Equals(C? other)",
                    "System.Boolean C.Equals(System.Object? obj)",
                    "System.Boolean System.Object.Equals(System.Object obj)",
                    "System.Boolean System.Object.Equals(System.Object objA, System.Object objB)",
                    "System.Boolean System.Object.ReferenceEquals(System.Object objA, System.Object objB)",
                    "System.Int32 C.GetHashCode()",
                    "System.Int32 C.x { get; init; }",
                    "System.Int32 C.y { get; init; }",
                    "System.Int32 System.Object.GetHashCode()",
                    "System.Object System.Object.MemberwiseClone()",
                    "System.String System.Object.ToString()",
                    "System.Type C.EqualityContract { get; }",
                    "System.Type System.Object.GetType()",
                    "void C.Deconstruct(out System.Int32 x, out System.Int32 y)",
                    "void System.Object.Finalize()"),
                s_pop
            );

            TestLookupNames(text, expectedNames);
        }

        [Fact]
        public void PositionalRecord2()
        {
            var text = @"
`record C`<T`>(int x, T t = default(T));";
            var expectedNames = MakeExpectedSymbols(
                Add( // Global
                    "System",
                    "Microsoft",
                    "C<T>"),
                Add( // C Type parameters
                    "T"),
                Add( // Members
                    "System.Int32 C<T>.x { get; init; }",
                    "T C<T>.t { get; init; }",
                    "System.Boolean C<T>.Equals(C<T>? other)",
                    "System.Boolean C<T>.Equals(System.Object? obj)",
                    "System.Boolean System.Object.Equals(System.Object obj)",
                    "System.Boolean System.Object.Equals(System.Object objA, System.Object objB)",
                    "System.Boolean System.Object.ReferenceEquals(System.Object objA, System.Object objB)",
                    "System.Int32 C<T>.GetHashCode()",
                    "System.Int32 System.Object.GetHashCode()",
                    "System.Object System.Object.MemberwiseClone()",
                    "void C<T>.Deconstruct(out System.Int32 x, out T t)",
                    "void System.Object.Finalize()",
                    "System.String System.Object.ToString()",
                    "System.Type C<T>.EqualityContract { get; }",
                    "System.Type System.Object.GetType()"),
                s_pop
                );

            TestLookupNames(text, expectedNames);
        }

        [Fact]
        public void NominalRecord()
        {
            var text = @"
`record C`<T`>
`{
    int x { get; }
    T t { get; }
`}";
            var members = new[] {
                "System.Int32 C<T>.x { get; }",
                "T C<T>.t { get; }",
                "System.Boolean C<T>.Equals(C<T>? other)",
                "System.Boolean C<T>.Equals(System.Object? obj)",
                "System.Boolean System.Object.Equals(System.Object obj)",
                "System.Boolean System.Object.Equals(System.Object objA, System.Object objB)",
                "System.Boolean System.Object.ReferenceEquals(System.Object objA, System.Object objB)",
                "System.Int32 C<T>.GetHashCode()",
                "System.Int32 System.Object.GetHashCode()",
                "System.Object System.Object.MemberwiseClone()",
                "void System.Object.Finalize()",
                "System.String System.Object.ToString()",
                    "System.Type C<T>.EqualityContract { get; }",
                "System.Type System.Object.GetType()",
            };
            var expectedNames = MakeExpectedSymbols(
                Add( // Global
                    "System",
                    "Microsoft",
                    "C<T>"),
                Add( // C decl
                    "T"),
                Add(members), // members are visible in type parameter list
                s_pop,
                Add(members), // body
                Combine(s_pop, s_pop) // remove members and type parameters
                );

            TestLookupNames(text, expectedNames);
        }

        [Fact]
        public void ExpressionBodiedProp()
        {
            var text = @"
class C
`{
    int P => 10;
    void M() { }
`}";
            var expectedNames = MakeExpectedSymbols(
                Add( //Global
                    "System",
                    "Microsoft",
                    "C"),
                Add( //C
                    "System.Int32 C.P { get; }",
                    "void C.M()",
                    "System.Boolean System.Object.Equals(System.Object obj)",
                    "System.Boolean System.Object.Equals(System.Object objA, System.Object objB)",
                    "System.Boolean System.Object.ReferenceEquals(System.Object objA, System.Object objB)",
                    "System.Int32 System.Object.GetHashCode()",
                    "System.Object System.Object.MemberwiseClone()",
                    "void System.Object.Finalize()",
                    "System.String System.Object.ToString()",
                    "System.Type System.Object.GetType()"),
                s_pop);

            TestLookupNames(text, expectedNames);
        }

        [Fact]
        public void TestNonGenericTypes()
        {
            var text = @"
class C
`{
    int x;
    int P { get; set; }
    void M() { }

    struct S
    `{
        int y;
        int Q { set `{ `} }
        void M() { }

        interface I
        `{
            void M();
        `}
    `}
`}
";

            var expectedNames = MakeExpectedSymbols(
                Add( //Global
                    "System",
                    "Microsoft",
                    "C"),
                Add( //C
                    "C.S",
                    "System.Int32 C.x",
                    "System.Int32 C.P { get; set; }",
                    "void C.M()",
                    "System.Boolean System.Object.Equals(System.Object obj)",
                    "System.Boolean System.Object.Equals(System.Object objA, System.Object objB)",
                    "System.Boolean System.Object.ReferenceEquals(System.Object objA, System.Object objB)",
                    "System.Int32 System.Object.GetHashCode()",
                    "System.Object System.Object.MemberwiseClone()",
                    "void System.Object.Finalize()",
                    "System.String System.Object.ToString()",
                    "System.Type System.Object.GetType()"),
                Combine( //C.S
                    Remove(
                        "void C.M()"),
                    Add(
                        "C.S.I",
                        "System.Int32 C.S.y",
                        "System.Int32 C.S.Q { set; }",
                        "void C.S.M()",
                        "System.Boolean System.ValueType.Equals(System.Object obj)",
                        "System.Int32 System.ValueType.GetHashCode()",
                        "System.String System.ValueType.ToString()")),
                Add("System.Int32 value"), //C.S.set
                s_pop, //C.S.set
                Combine( //C.S.I
                    Remove(
                        "void C.S.M()",
                        "System.Boolean System.ValueType.Equals(System.Object obj)",
                        "System.Int32 System.ValueType.GetHashCode()",
                        "System.String System.ValueType.ToString()"),
                    Add(
                        "void C.S.I.M()")),
                Combine(s_pop, s_pop), //C.S.I
                Combine(s_pop, s_pop), //C.S
                s_pop //C
            );

            TestLookupNames(text, expectedNames);
        }

        [Fact]
        public void TestGenericTypes()
        {
            var text = @"
`class C`<T, Z`>
`{
    int x;
    int P { get; set; }
    void M() { }

    `struct S`<U, Z`>
    `{
        int y;
        int Q { set `{ `} }
        void M() { }

        `interface I`<V, Z`>
        `{
            void M();
        `}
    `}
`}
";

            string[] class_C_members = new string[] {
                "C<T, Z>.S<U, Z>",
                "System.Int32 C<T, Z>.x",
                "System.Int32 C<T, Z>.P { get; set; }",
                "void C<T, Z>.M()",
                "System.Boolean System.Object.Equals(System.Object obj)",
                "System.Boolean System.Object.Equals(System.Object objA, System.Object objB)",
                "System.Boolean System.Object.ReferenceEquals(System.Object objA, System.Object objB)",
                "System.Int32 System.Object.GetHashCode()",
                "System.Object System.Object.MemberwiseClone()",
                "void System.Object.Finalize()",
                "System.String System.Object.ToString()",
                "System.Type System.Object.GetType()"
            };

            string[] struct_S_members = new string[] {
                "C<T, Z>.S<U, Z>.I<V, Z>",
                "System.Int32 C<T, Z>.S<U, Z>.y",
                "System.Int32 C<T, Z>.S<U, Z>.Q { set; }",
                "void C<T, Z>.S<U, Z>.M()",
                "System.Boolean System.ValueType.Equals(System.Object obj)",
                "System.Int32 System.ValueType.GetHashCode()",
                "System.String System.ValueType.ToString()"
            };

            string interface_I_member = "void C<T, Z>.S<U, Z>.I<V, Z>.M()";

            var expectedNames = MakeExpectedSymbols(
                Add( //Global
                    "System",
                    "Microsoft",
                    "C<T, Z>"),
                Add( //C decl
                    "T",
                    "Z"),
                Add(class_C_members), //"<T, Z>" : C members are in scope in Type parameter list 
                s_pop, //C members are not in scope in Base declaration list
                Add(class_C_members), //C<T> body
                Add("U"), //C.S decl
                Combine( //"<U, Z>" : C.S members are in scope in Type parameter list 
                    Remove("void C<T, Z>.M()"),
                    Add(struct_S_members)),
                Combine(s_pop, s_pop), //C.S members are not in scope in Base declaration list
                Combine( //C.S body
                    Remove("void C<T, Z>.M()"),
                    Add(struct_S_members)),
                Add("System.Int32 value"), //C.S.set
                s_pop, //C.S.set
                Add("V"), //C.S.I decl 
                Combine( //"<V, Z>" : C.S.I members are in scope in Type parameter list 
                    Remove(
                        "void C<T, Z>.S<U, Z>.M()",
                        "System.Boolean System.ValueType.Equals(System.Object obj)",
                        "System.Int32 System.ValueType.GetHashCode()",
                        "System.String System.ValueType.ToString()"),
                    Add(interface_I_member)),
                Combine(s_pop, s_pop), //C.S.I members are not in scope in Base declaration list
                Combine( //C.S.I body
                    Remove(
                        "void C<T, Z>.S<U, Z>.M()",
                        "System.Boolean System.ValueType.Equals(System.Object obj)",
                        "System.Int32 System.ValueType.GetHashCode()",
                        "System.String System.ValueType.ToString()"),
                    Add(interface_I_member)),
                Combine(s_pop, s_pop, s_pop), //C.S.I decl and body
                Combine(s_pop, s_pop, s_pop), //C.S body and decl
                Combine(s_pop, s_pop) //C body and decl
            );

            TestLookupNames(text, expectedNames);
        }

        [Fact]
        public void TestGenericMethods()
        {
            var text = @"
class C
`{
    `void `M`<T>(T t) `{ `}
    `void `N`<T>() { `}
    void O(int t) `{ `}
`}
";

            var expectedNames = MakeExpectedSymbols(
                Add( //Global
                    "System",
                    "Microsoft",
                    "C"),
                Add( //C
                    "void C.M<T>(T t)",
                    "void C.N<T>()",
                    "void C.O(System.Int32 t)",
                    "System.Boolean System.Object.Equals(System.Object obj)",
                    "System.Boolean System.Object.Equals(System.Object objA, System.Object objB)",
                    "System.Boolean System.Object.ReferenceEquals(System.Object objA, System.Object objB)",
                    "System.Int32 System.Object.GetHashCode()",
                    "System.Object System.Object.MemberwiseClone()",
                    "void System.Object.Finalize()",
                    "System.String System.Object.ToString()",
                    "System.Type System.Object.GetType()"),
                Add("T"), s_pop, //C.M return type
                Add("T"), //C.M after name
                Add("T t"), //C.M body
                Combine(s_pop, s_pop), //C.M
                Add("T"), s_pop, //C.N return type
                Add("T"), //C.N after name
                s_pop, //C.N
                Add("System.Int32 t"), //C.O
                s_pop, //C.O
                s_pop //C
            );

            TestLookupNames(text, expectedNames);
        }

        private static readonly string[] s_commonDelegateTypeMembers = new string[] {
                "System.Boolean System.Delegate.Equals(System.Object obj)",
                "System.Boolean System.MulticastDelegate.Equals(System.Object obj)",
                "System.Delegate System.Delegate.Combine(params System.Delegate[] delegates)",
                "System.Delegate System.Delegate.Combine(System.Delegate a, System.Delegate b)",
                "System.Delegate System.Delegate.CombineImpl(System.Delegate d)",
                "System.Delegate System.Delegate.CreateDelegate(System.Type type, System.Object firstArgument, System.Reflection.MethodInfo method)",
                "System.Delegate System.Delegate.CreateDelegate(System.Type type, System.Object firstArgument, System.Reflection.MethodInfo method, System.Boolean throwOnBindFailure)",
                "System.Delegate System.Delegate.CreateDelegate(System.Type type, System.Object target, System.String method)",
                "System.Delegate System.Delegate.CreateDelegate(System.Type type, System.Object target, System.String method, System.Boolean ignoreCase)",
                "System.Delegate System.Delegate.CreateDelegate(System.Type type, System.Object target, System.String method, System.Boolean ignoreCase, System.Boolean throwOnBindFailure)",
                "System.Delegate System.Delegate.CreateDelegate(System.Type type, System.Reflection.MethodInfo method)",
                "System.Delegate System.Delegate.CreateDelegate(System.Type type, System.Reflection.MethodInfo method, System.Boolean throwOnBindFailure)",
                "System.Delegate System.Delegate.CreateDelegate(System.Type type, System.Type target, System.String method)",
                "System.Delegate System.Delegate.CreateDelegate(System.Type type, System.Type target, System.String method, System.Boolean ignoreCase)",
                "System.Delegate System.Delegate.CreateDelegate(System.Type type, System.Type target, System.String method, System.Boolean ignoreCase, System.Boolean throwOnBindFailure)",
                "System.Delegate System.Delegate.Remove(System.Delegate source, System.Delegate value)",
                "System.Delegate System.Delegate.RemoveAll(System.Delegate source, System.Delegate value)",
                "System.Delegate System.Delegate.RemoveImpl(System.Delegate d)",
                "System.Delegate System.MulticastDelegate.CombineImpl(System.Delegate follow)",
                "System.Delegate System.MulticastDelegate.RemoveImpl(System.Delegate value)",
                "System.Delegate[] System.Delegate.GetInvocationList()",
                "System.Delegate[] System.MulticastDelegate.GetInvocationList()",
                "System.Int32 System.Delegate.GetHashCode()",
                "System.Int32 System.MulticastDelegate.GetHashCode()",
                "System.Object System.Delegate.Clone()",
                "System.Object System.Delegate.DynamicInvoke(params System.Object[] args)",
                "System.Object System.Delegate.DynamicInvokeImpl(System.Object[] args)",
                "System.Object System.Delegate.Target { get; }",
                "System.Reflection.MethodInfo System.Delegate.GetMethodImpl()",
                "System.Reflection.MethodInfo System.Delegate.Method { get; }",
                "System.Reflection.MethodInfo System.MulticastDelegate.GetMethodImpl()",
                "void System.Delegate.GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)",
                "void System.MulticastDelegate.GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)",
            };

        private static readonly string[] s_commonEnumTypeMembers = new string[] {
                "System.Array System.Enum.GetValues(System.Type enumType)",
                "System.Boolean System.Enum.Equals(System.Object obj)",
                "System.Boolean System.Enum.HasFlag(System.Enum flag)",
                "System.Boolean System.Enum.IsDefined(System.Type enumType, System.Object value)",
                "System.Boolean System.Enum.TryParse<TEnum>(System.String value, out TEnum result)",
                "System.Boolean System.Enum.TryParse<TEnum>(System.String value, System.Boolean ignoreCase, out TEnum result)",
                "System.Boolean System.ValueType.Equals(System.Object obj)",
                "System.Int32 System.Enum.CompareTo(System.Object target)",
                "System.Int32 System.Enum.GetHashCode()",
                "System.Int32 System.ValueType.GetHashCode()",
                "System.Object System.Enum.Parse(System.Type enumType, System.String value)",
                "System.Object System.Enum.Parse(System.Type enumType, System.String value, System.Boolean ignoreCase)",
                "System.Object System.Enum.ToObject(System.Type enumType, System.Byte value)",
                "System.Object System.Enum.ToObject(System.Type enumType, System.Int16 value)",
                "System.Object System.Enum.ToObject(System.Type enumType, System.Int32 value)",
                "System.Object System.Enum.ToObject(System.Type enumType, System.Int64 value)",
                "System.Object System.Enum.ToObject(System.Type enumType, System.Object value)",
                "System.Object System.Enum.ToObject(System.Type enumType, System.SByte value)",
                "System.Object System.Enum.ToObject(System.Type enumType, System.UInt16 value)",
                "System.Object System.Enum.ToObject(System.Type enumType, System.UInt32 value)",
                "System.Object System.Enum.ToObject(System.Type enumType, System.UInt64 value)",
                "System.String System.Enum.Format(System.Type enumType, System.Object value, System.String format)",
                "System.String System.Enum.GetName(System.Type enumType, System.Object value)",
                "System.String System.Enum.ToString()",
                "System.String System.Enum.ToString(System.IFormatProvider provider)",
                "System.String System.Enum.ToString(System.String format)",
                "System.String System.Enum.ToString(System.String format, System.IFormatProvider provider)",
                "System.String System.ValueType.ToString()",
                "System.String[] System.Enum.GetNames(System.Type enumType)",
                "System.Type System.Enum.GetUnderlyingType(System.Type enumType)",
                "System.TypeCode System.Enum.GetTypeCode()",
            };

        [Fact, WorkItem(545556, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545556")]
        public void TestAssortedMembers()
        {
            var text = @"
namespace NS
`{
    public interface I `{ `}
`}

public abstract `class C`<T`> : NS.I
`{
    `delegate void D1()`;
    `delegate void D2(int t)`;
    `delegate void D3<U>()`;
    `delegate void D4<V>(V t)`;

    enum E
    `{
        A,
    `}

    private C() : base() { }
    protected C(T t) `: this() { `}
    public C(int t) `{ `}

    internal T P { get; set; }
    protected internal int Q
    { 
        get { return 1; }
        set `{ `}
    }

    public int this[int z]
    { 
        get `{ return 1; `}
        set `{ `}
    }

    private const int c = 1;
    private readonly int f;

    static C()
    {
    }

    `public abstract void `M`<W>(W w)`;
`}
";
            string[] class_C_members = new string[]{
                "C<T>.D1",
                "C<T>.D2",
                "C<T>.D3<U>",
                "C<T>.D4<V>",
                "C<T>.E",
                "System.Int32 C<T>.c",
                "System.Int32 C<T>.f",
                "System.Int32 C<T>.Q { get; set; }",
                "T C<T>.P { get; set; }",
                "void C<T>.M<W>(W w)",
                "System.Boolean System.Object.Equals(System.Object obj)",
                "System.Boolean System.Object.Equals(System.Object objA, System.Object objB)",
                "System.Boolean System.Object.ReferenceEquals(System.Object objA, System.Object objB)",
                "System.Int32 System.Object.GetHashCode()",
                "System.Object System.Object.MemberwiseClone()",
                "void System.Object.Finalize()",
                "System.String System.Object.ToString()",
                "System.Type System.Object.GetType()"};

            string[] delegate_d1_members = new string[]{
                "System.IAsyncResult C<T>.D1.BeginInvoke(System.AsyncCallback callback, System.Object @object)",
                "void C<T>.D1.EndInvoke(System.IAsyncResult result)",
                "void C<T>.D1.Invoke()"
            }.Concat(s_commonDelegateTypeMembers).ToArray();

            string[] delegate_d2_members = new string[]{
                "System.IAsyncResult C<T>.D2.BeginInvoke(System.Int32 t, System.AsyncCallback callback, System.Object @object)",
                "void C<T>.D2.EndInvoke(System.IAsyncResult result)",
                "void C<T>.D2.Invoke(System.Int32 t)"
            }.Concat(s_commonDelegateTypeMembers).ToArray();

            string[] delegate_d3_members = new string[]{
                "System.IAsyncResult C<T>.D3<U>.BeginInvoke(System.AsyncCallback callback, System.Object @object)",
                "void C<T>.D3<U>.EndInvoke(System.IAsyncResult result)",
                "void C<T>.D3<U>.Invoke()"
            }.Concat(s_commonDelegateTypeMembers).ToArray();

            string[] delegate_d4_members = new string[]{
                "System.IAsyncResult C<T>.D4<V>.BeginInvoke(V t, System.AsyncCallback callback, System.Object @object)",
                "void C<T>.D4<V>.EndInvoke(System.IAsyncResult result)",
                "void C<T>.D4<V>.Invoke(V t)"
            }.Concat(s_commonDelegateTypeMembers).ToArray();

            string[] enum_e_members = new string[]{
                "C<T>.E.A"
            }.Concat(s_commonEnumTypeMembers).ToArray();

            var expectedNames = MakeExpectedSymbols(
                Add( //Global
                    "NS",
                    "C<T>",
                    "System",
                    "Microsoft"),
                Add("NS.I"), //NS
                Add(
                    "System.Boolean System.Object.Equals(System.Object obj)",
                    "System.Boolean System.Object.Equals(System.Object objA, System.Object objB)",
                    "System.Boolean System.Object.ReferenceEquals(System.Object objA, System.Object objB)",
                    "System.Int32 System.Object.GetHashCode()",
                    "System.String System.Object.ToString()",
                    "System.Type System.Object.GetType()"),
                s_pop, //NS.I
                s_pop, //NS
                Add("T"), //C<T> decl
                Add(class_C_members), //"<T>" : C<T> members are in scope in Type parameter list 
                s_pop, // ": NS.I" : C<T> members are not in scope in Base declaration list
                Add(class_C_members), //C<T> body
                Add(delegate_d1_members), //C<T>.D1
                s_pop, //C<T>.D1
                Add(delegate_d2_members), //C<T>.D2
                s_pop, //C<T>.D2
                Combine( //C<T>.D3<U>
                    Add("U"), //C<T>.D3<U>
                    Add(delegate_d3_members)), //C<T>.D3<U> members are in scope in delegate declaration
                Combine(s_pop, s_pop), //C<T>.D3<U>
                Combine( //C<T>.D4<V>
                    Add("V"), //C<T>.D4<V>
                    Add(delegate_d4_members)), //C<T>.D4<V> members are in scope in delegate declaration
                Combine(s_pop, s_pop), //C<T>.D4<V>
                Add(enum_e_members), //C<T>.E
                s_pop, //C<T>.E
                Add("T t"), s_pop, //C<T>..ctor(T)
                Add("System.Int32 t"), s_pop, //C<T>..ctor(int)
                Add("System.Int32 value"), s_pop, //C<T>.Q.set
                Add("System.Int32 z"), s_pop, //C<T>.this[int].get
                Add("System.Int32 z", "System.Int32 value"), s_pop, //C<T>.this[int].set
                Add("W"), s_pop, //C<T>.M<W> return type
                Add("W"), s_pop, //C<T>.M<W> after name
                Combine(s_pop, s_pop) //C<T>
            );

            TestLookupNames(text, expectedNames);
        }

        [Fact]
        public void TestSafeControlFlow() //i.e. not unsafe
        {
            var text = @"
class C
`{
    void M()
    `{
        object a = null;
        
        `{
            int b;
        `}

        while (true)
        `{
            int c;
        `}

        do
        `{
            int d;
        `} while (true);

        for (`int e = 0; e < 10; e++)
        `{
            int f;
        `}

        for (; a == null; a = a.ToString())
        `{
            int f;
        `}

        foreach (int g in new int[1])
        `{
            int h;
        `}

        `using (System.IDisposable i = null)
        `{
            int j;
        `}

        checked
        `{
            int k;
        `}

        unchecked
        `{
            int l;
        `}

        lock (a)
        `{
            int m;
        `}

        if (true)
        `{
            int n;
        `}

        if (true)
        `{
            int o;
        `}
        else
        `{
            int p;
        `}

        if (true)
        `{
            int q;
        `}
        else if (true)
        `{
            int r;
        `}

        switch (a.GetHashCode())
        `{
            case 1:
                int s;
                break;
            case 2:
                int t;
                break;
            default:
                int u;
                break;
        `}

        try
        `{
            int v;
        `}
        catch (System.Exception)
        `{
            int w;
        `}

        try
        `{
            int x;
        `}
        catch (System.Exception y)
        `{
            int z;
        `}

        try
        `{
            int aa;
        `}
        catch (System.InvalidCastException bb)
        `{
            int cc;
        `}
        catch (System.InvalidOperationException dd)
        `{
            int ee;
        `}
        catch (System.Exception)
        `{
            int ff;
        `}

        try
        `{
            int gg;
        `}
        finally
        `{
            int hh;
        `}

        try
        `{
            int ii;
        `}
        catch (System.InvalidCastException jj)
        `{
            int kk;
        `}
        finally
        `{
            int ll;
        `}
    `}
`}
";

            var expectedNames = MakeExpectedSymbols(
                Add( //Global
                    "C",
                    "System",
                    "Microsoft"),
                Add( //C
                    "void C.M()",
                    "System.Boolean System.Object.Equals(System.Object obj)",
                    "System.Boolean System.Object.Equals(System.Object objA, System.Object objB)",
                    "System.Boolean System.Object.ReferenceEquals(System.Object objA, System.Object objB)",
                    "System.Int32 System.Object.GetHashCode()",
                    "System.Object System.Object.MemberwiseClone()",
                    "void System.Object.Finalize()",
                    "System.String System.Object.ToString()",
                    "System.Type System.Object.GetType()"),
                Add("System.Object a"), //C.M
                Add("System.Int32 b"), s_pop, //block
                Add("System.Int32 c"), s_pop, //while
                Add("System.Int32 d"), s_pop, //do-while
                Add("System.Int32 e"), //for decl
                Add("System.Int32 f"), //for body
                Combine(s_pop, s_pop), //for decl & body
                Add("System.Int32 f"), s_pop, //second for body
                Add("System.Int32 g", "System.Int32 h"), //foreach
                s_pop, //foreach
                Add("System.IDisposable i"), //using decl
                Add("System.Int32 j"), //using body
                Combine(s_pop, s_pop), //using decl & body
                Add("System.Int32 k"), s_pop, //checked
                Add("System.Int32 l"), s_pop, //unchecked
                Add("System.Int32 m"), s_pop, //lock
                Add("System.Int32 n"), s_pop, //if
                Add("System.Int32 o"), s_pop, //if-else if part
                Add("System.Int32 p"), s_pop, //if-else else part
                Add("System.Int32 q"), s_pop, //if-elseif if part
                Add("System.Int32 r"), s_pop, //if-elseif elseif part
                Add("System.Int32 s", "System.Int32 t", "System.Int32 u"), s_pop, //switch
                Add("System.Int32 v"), s_pop, //try1 try part
                Add("System.Int32 w"), //try1 catch
                s_pop, //try1 catch
                Add("System.Int32 x"), s_pop, //try2 try part
                Add("System.Exception y", "System.Int32 z"), //try2 catch
                s_pop, //try2 catch
                Add("System.Int32 aa"), s_pop, //try3 try part
                Add("System.InvalidCastException bb", "System.Int32 cc"), //try3 first catch
                s_pop, //try3 first catch
                Add("System.InvalidOperationException dd", "System.Int32 ee"), //try3 second catch
                s_pop, //try3 second catch
                Add("System.Int32 ff"), //try3 third catch
                s_pop, //try3 third catch
                Add("System.Int32 gg"), s_pop, //try4 try part
                Add("System.Int32 hh"), s_pop, //try4 finally part
                Add("System.Int32 ii"), s_pop, //try5 try part
                Add("System.InvalidCastException jj", "System.Int32 kk"), //try5 catch
                s_pop, //try5 catch
                Add("System.Int32 ll"), s_pop, //try5 finally part
                s_pop, //C.M
                s_pop //C
            );

            TestLookupNames(text, expectedNames);
        }

        [Fact]
        public void TestLambdas()
        {
            var text = @"
class C
`{
    System.Func<int, int> f1 = x `=> x`;
    System.Func<int, int> f2 = x `=> `{ int y; return x; `};

    void M()
    `{
        System.Func<int, int> g1 = x `=> x`;
        System.Func<int, int> g2 = x `=> `{ int y; return x; `};
    `}
`}
";

            var expectedNames = MakeExpectedSymbols(
                Add( //Global
                    "C",
                    "System",
                    "Microsoft"),
                Add( //C
                    "System.Func<System.Int32, System.Int32> C.f1",
                    "System.Func<System.Int32, System.Int32> C.f2",
                    "void C.M()",
                    "System.Boolean System.Object.Equals(System.Object obj)",
                    "System.Boolean System.Object.Equals(System.Object objA, System.Object objB)",
                    "System.Boolean System.Object.ReferenceEquals(System.Object objA, System.Object objB)",
                    "System.Int32 System.Object.GetHashCode()",
                    "System.Object System.Object.MemberwiseClone()",
                    "void System.Object.Finalize()",
                    "System.String System.Object.ToString()",
                    "System.Type System.Object.GetType()"),
                Add("System.Int32 x"), s_pop, //f1
                Add("System.Int32 x"), //f2 lambda parameters
                Add("System.Int32 y"), //f2 lambda body
                Combine(s_pop, s_pop), //f2 lambda parameters and body
                Add("System.Func<System.Int32, System.Int32> g1", "System.Func<System.Int32, System.Int32> g2"), //C.M
                Add("System.Int32 x"), s_pop, //g1
                Add("System.Int32 x"), //g2 lambda parameters
                Add("System.Int32 y"), //g2 lambda body
                Combine(s_pop, s_pop), //g2 lambda parameters and body
                s_pop, //C.M
                s_pop //C
            );

            TestLookupNames(text, expectedNames);
        }

        [Fact]
        public void TestNestedLambdas()
        {
            var text = @"
class C
`{
    System.Func<int, System.Func<int, int>> f1 = x `=> y `=> x + y`;
    System.Func<int, System.Func<int, int>> f2 = x `=> `{ int y; `{int z; return a `=> x`; `} `};

    void M()
    `{
        System.Func<int, System.Func<int, int>> g1 = x `=> y `=> x + y`;
        System.Func<int, System.Func<int, int>> g2 = x `=> `{ int y; `{int z; return a `=> x`; `} `};
    `}
`}
";

            var expectedNames = MakeExpectedSymbols(
                Add( //Global
                    "C",
                    "System",
                    "Microsoft"),
                Add( //C
                    "System.Func<System.Int32, System.Func<System.Int32, System.Int32>> C.f1",
                    "System.Func<System.Int32, System.Func<System.Int32, System.Int32>> C.f2",
                    "void C.M()",
                    "System.Boolean System.Object.Equals(System.Object obj)",
                    "System.Boolean System.Object.Equals(System.Object objA, System.Object objB)",
                    "System.Boolean System.Object.ReferenceEquals(System.Object objA, System.Object objB)",
                    "System.Int32 System.Object.GetHashCode()",
                    "System.Object System.Object.MemberwiseClone()",
                    "void System.Object.Finalize()",
                    "System.String System.Object.ToString()",
                    "System.Type System.Object.GetType()"),
                Add("System.Int32 x"), //f1 outer
                Add("System.Int32 y"), //f1 inner
                Combine(s_pop, s_pop), //f1
                Add("System.Int32 x"), //f2 outer
                Add("System.Int32 y"), //f2 outer block 1
                Add("System.Int32 z"), //f2 outer block 2
                Add("System.Int32 a"), s_pop, //f2 inner
                s_pop, //f2 outer block 2
                Combine(s_pop, s_pop), //f2 outer block 1 and outer
                Add( //C.M
                    "System.Func<System.Int32, System.Func<System.Int32, System.Int32>> g1",
                    "System.Func<System.Int32, System.Func<System.Int32, System.Int32>> g2"),
                Add("System.Int32 x"), //g1 outer
                Add("System.Int32 y"), //g1 inner
                Combine(s_pop, s_pop), //g1
                Add("System.Int32 x"), //g2 outer
                Add("System.Int32 y"), //g2 outer block 1
                Add("System.Int32 z"), //g2 outer block 2
                Add("System.Int32 a"), s_pop, //g2 inner
                s_pop, //g2 outer block 2
                Combine(s_pop, s_pop), //g2 outer block 1 and outer
                s_pop, //C.M
                s_pop //C
            );

            TestLookupNames(text, expectedNames);
        }

        [WorkItem(540633, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540633")]
        [Fact]
        public void TestConstructorInitializers()
        {
            var text = @"
class C
`{
    public C(int x) `{ `}
`}

class D : C
`{
    public D(int a, int b) `: base(a) { `}
    public D(int c) `: this(c, 1) { `}
`}
";

            var expectedNames = MakeExpectedSymbols(
                Add( //Global
                    "C",
                    "D",
                    "System",
                    "Microsoft"),
                Add( //C
                    "System.Boolean System.Object.Equals(System.Object obj)",
                    "System.Boolean System.Object.Equals(System.Object objA, System.Object objB)",
                    "System.Boolean System.Object.ReferenceEquals(System.Object objA, System.Object objB)",
                    "System.Int32 System.Object.GetHashCode()",
                    "System.Object System.Object.MemberwiseClone()",
                    "void System.Object.Finalize()",
                    "System.String System.Object.ToString()",
                    "System.Type System.Object.GetType()"),
                Add("System.Int32 x"), s_pop, //C.C(int)
                s_pop, //C
                Add( //D
                    "System.Boolean System.Object.Equals(System.Object obj)",
                    "System.Boolean System.Object.Equals(System.Object objA, System.Object objB)",
                    "System.Boolean System.Object.ReferenceEquals(System.Object objA, System.Object objB)",
                    "System.Int32 System.Object.GetHashCode()",
                    "System.Object System.Object.MemberwiseClone()",
                    "void System.Object.Finalize()",
                    "System.String System.Object.ToString()",
                    "System.Type System.Object.GetType()"),
                Add("System.Int32 a", "System.Int32 b"), s_pop, //D.D(int, int)
                Add("System.Int32 c"), s_pop, //D.D(int)
                s_pop //D
            );

            TestLookupNames(text, expectedNames);
        }

        [WorkItem(540888, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540888")]
        [Fact]
        public void TestLambdaInConstructorInitializer()
        {
            var text = @"
class C
`{
    public C(System.Func<int, int> x) `{ `}
    public C() : this(a `=> a`) 
    {
        M(b `=> b`); 
    }

    private void M(System.Func<int, int> f) `{ `}
`}
";

            var expectedNames = MakeExpectedSymbols(
                Add( //Global
                    "C",
                    "System",
                    "Microsoft"),
                Add( //C
                    "void C.M(System.Func<System.Int32, System.Int32> f)",
                    "System.Boolean System.Object.Equals(System.Object obj)",
                    "System.Boolean System.Object.Equals(System.Object objA, System.Object objB)",
                    "System.Boolean System.Object.ReferenceEquals(System.Object objA, System.Object objB)",
                    "System.Int32 System.Object.GetHashCode()",
                    "System.Object System.Object.MemberwiseClone()",
                    "void System.Object.Finalize()",
                    "System.String System.Object.ToString()",
                    "System.Type System.Object.GetType()"),
                Add("System.Func<System.Int32, System.Int32> x"), s_pop, //C.C(Func)
                Add("System.Int32 a"), s_pop, //C.C() ctor initializer lambda
                Add("System.Int32 b"), s_pop, //C.C() ctor body lambda
                Add("System.Func<System.Int32, System.Int32> f"), s_pop, //C.M(Func)
                s_pop //C
            );

            TestLookupNames(text, expectedNames);
        }

        [WorkItem(540890, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540890")]
        [Fact]
        public void TestLambdaAtEof()
        {
            var text = @"
class C
`{
    private void M(System.Func<int, int> f) `{ `}
    public C()
    {
        M(b `=>
";

            var expectedNames = MakeExpectedSymbols(
                Add( //Global
                    "C",
                    "System",
                    "Microsoft"),
                Add( //C
                    "void C.M(System.Func<System.Int32, System.Int32> f)",
                    "System.Boolean System.Object.Equals(System.Object obj)",
                    "System.Boolean System.Object.Equals(System.Object objA, System.Object objB)",
                    "System.Boolean System.Object.ReferenceEquals(System.Object objA, System.Object objB)",
                    "System.Int32 System.Object.GetHashCode()",
                    "System.Object System.Object.MemberwiseClone()",
                    "void System.Object.Finalize()",
                    "System.String System.Object.ToString()",
                    "System.Type System.Object.GetType()"),
                Add("System.Func<System.Int32, System.Int32> f"), s_pop, //C.M(Func)
                Add("System.Int32 b") //C.C() ctor body lambda
            );

            TestLookupNames(text, expectedNames);
        }

        [WorkItem(540890, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540890")]
        [Fact]
        public void TestLambdaWithMissingBody()
        {
            var text = @"
class C
`{
    private void M(System.Func<int, int> f) `{ `}
    public C()
    {
        M(b `=> `);
    }
`}
";

            var expectedNames = MakeExpectedSymbols(
                Add( //Global
                    "C",
                    "System",
                    "Microsoft"),
                Add( //C
                    "void C.M(System.Func<System.Int32, System.Int32> f)",
                    "System.Boolean System.Object.Equals(System.Object obj)",
                    "System.Boolean System.Object.Equals(System.Object objA, System.Object objB)",
                    "System.Boolean System.Object.ReferenceEquals(System.Object objA, System.Object objB)",
                    "System.Int32 System.Object.GetHashCode()",
                    "System.Object System.Object.MemberwiseClone()",
                    "void System.Object.Finalize()",
                    "System.String System.Object.ToString()",
                    "System.Type System.Object.GetType()"),
                Add("System.Func<System.Int32, System.Int32> f"), s_pop, //C.M(Func)
                Add("System.Int32 b"), s_pop, //C.C() ctor body lambda
                s_pop //C
            );

            TestLookupNames(text, expectedNames);
        }

        [WorkItem(540890, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540890")]
        [Fact]
        public void TestIncompleteConstructorParameters1()
        {
            var text = @"
class C
`{
    public C(int x
";

            var expectedNames = MakeExpectedSymbols(
                Add( //Global
                    "C",
                    "System",
                    "Microsoft"),
                Add( //C
                    "System.Boolean System.Object.Equals(System.Object obj)",
                    "System.Boolean System.Object.Equals(System.Object objA, System.Object objB)",
                    "System.Boolean System.Object.ReferenceEquals(System.Object objA, System.Object objB)",
                    "System.Int32 System.Object.GetHashCode()",
                    "System.Object System.Object.MemberwiseClone()",
                    "void System.Object.Finalize()",
                    "System.String System.Object.ToString()",
                    "System.Type System.Object.GetType()")
            );

            TestLookupNames(text, expectedNames);
        }

        [WorkItem(540890, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540890")]
        [Fact]
        public void TestIncompleteConstructorParameters2()
        {
            var text = @"
class C
`{
    public C(int x)
";

            var expectedNames = MakeExpectedSymbols(
                Add( //Global
                    "C",
                    "System",
                    "Microsoft"),
                Add( //C
                    "System.Boolean System.Object.Equals(System.Object obj)",
                    "System.Boolean System.Object.Equals(System.Object objA, System.Object objB)",
                    "System.Boolean System.Object.ReferenceEquals(System.Object objA, System.Object objB)",
                    "System.Int32 System.Object.GetHashCode()",
                    "System.Object System.Object.MemberwiseClone()",
                    "void System.Object.Finalize()",
                    "System.String System.Object.ToString()",
                    "System.Type System.Object.GetType()")
            //NB: can't see x because we're in the parameter list until we see another token
            );

            TestLookupNames(text, expectedNames);
        }

        [WorkItem(540890, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540890")]
        [Fact]
        public void TestIncompleteConstructorParameters3()
        {
            var text = @"
class C
`{
    public C(int x) `:
";

            var expectedNames = MakeExpectedSymbols(
                Add( //Global
                    "C",
                    "System",
                    "Microsoft"),
                Add( //C
                    "System.Boolean System.Object.Equals(System.Object obj)",
                    "System.Boolean System.Object.Equals(System.Object objA, System.Object objB)",
                    "System.Boolean System.Object.ReferenceEquals(System.Object objA, System.Object objB)",
                    "System.Int32 System.Object.GetHashCode()",
                    "System.Object System.Object.MemberwiseClone()",
                    "void System.Object.Finalize()",
                    "System.String System.Object.ToString()",
                    "System.Type System.Object.GetType()"),
                Add("System.Int32 x") // C.C(int)
            );

            TestLookupNames(text, expectedNames);
        }

        [WorkItem(540890, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540890")]
        [Fact]
        public void TestIncompleteConstructorParameters4()
        {
            var text = @"
class C
`{
    public C(int x) `{
";

            var expectedNames = MakeExpectedSymbols(
                Add( //Global
                    "C",
                    "System",
                    "Microsoft"),
                Add( //C
                    "System.Boolean System.Object.Equals(System.Object obj)",
                    "System.Boolean System.Object.Equals(System.Object objA, System.Object objB)",
                    "System.Boolean System.Object.ReferenceEquals(System.Object objA, System.Object objB)",
                    "System.Int32 System.Object.GetHashCode()",
                    "System.Object System.Object.MemberwiseClone()",
                    "void System.Object.Finalize()",
                    "System.String System.Object.ToString()",
                    "System.Type System.Object.GetType()"),
                Add("System.Int32 x") // C.C(int)
            );

            TestLookupNames(text, expectedNames);
        }

        [WorkItem(542360, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542360")]
        [Fact]
        public void TestMethodParameterAndTypeParameterScope()
        {
            var text = @"
class C
`{
    [System.ObsoleteAttribute`]
    void `M`<T>(int x) `{ `}
`}
";

            var expectedNames = MakeExpectedSymbols(
                Add( //Global
                    "C",
                    "System",
                    "Microsoft"),
                Add( //C
                    "void C.M<T>(System.Int32 x)",
                    "System.Boolean System.Object.Equals(System.Object obj)",
                    "System.Boolean System.Object.Equals(System.Object objA, System.Object objB)",
                    "System.Boolean System.Object.ReferenceEquals(System.Object objA, System.Object objB)",
                    "System.Int32 System.Object.GetHashCode()",
                    "System.Object System.Object.MemberwiseClone()",
                    "void System.Object.Finalize()",
                    "System.String System.Object.ToString()",
                    "System.Type System.Object.GetType()"),
                Add("T"), s_pop, //C.C(int) return type
                Add("T"), //C.C(int) between name and body
                Add("System.Int32 x"), //C.C(int) body
                Combine(s_pop, s_pop), //C.C(int)
                s_pop //C
            );

            TestLookupNames(text, expectedNames);
        }

        [Fact]
        [WorkItem(16801, "https://github.com/dotnet/roslyn/issues/16801")]
        public void TestLocalFunctionParameterAndTypeParameterScope()
        {
            var text = @"
class C
`{
    void Test()
    `{
        `void `M`<T>(int x) `{ `}
    `}
`}
";

            var expectedNames = MakeExpectedSymbols(
                Add( //Global
                    "C",
                    "System",
                    "Microsoft"),
                Add( //C
                    "void C.Test()",
                    "System.Boolean System.Object.Equals(System.Object obj)",
                    "System.Boolean System.Object.Equals(System.Object objA, System.Object objB)",
                    "System.Boolean System.Object.ReferenceEquals(System.Object objA, System.Object objB)",
                    "System.Int32 System.Object.GetHashCode()",
                    "System.Object System.Object.MemberwiseClone()",
                    "void System.Object.Finalize()",
                    "System.String System.Object.ToString()",
                    "System.Type System.Object.GetType()"),
                Add("void M<T>(System.Int32 x)"), // Test body
                Add("T"), //M<T>(int) return type
                s_pop, //M<T>(int) name
                Add("T"), //M<T>(int) between name and body
                Add("System.Int32 x"), //M<T>(int) body
                Combine(s_pop, s_pop), //M<T>(int) after body
                s_pop, // Test body
                s_pop //C
            );

            TestLookupNames(text, expectedNames);
        }

        [WorkItem(529406, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529406")]
        [Fact]
        public void TestLeftToRightDeclarators()
        {
            var text = @"
unsafe class C
`{
    int[] a = new int[2];

    void M()
    `{
        `fixed (int* q = &a[*p], p = new int[2])
        {

        `}

        `using (System.IDisposable d1 = d2, d2 = null)
        {

        `}

        for (`int i = j, j = 0; i < j; i++)
        {

        `}

        object o1 = o2, o2 = null;
    `}
`}
";

            var expectedNames = MakeExpectedSymbols(
                Add( //Global
                    "System",
                    "Microsoft",
                    "C"),
                Add( //C
                    "System.Int32[] C.a",
                    "void C.M()",
                    "System.Boolean System.Object.Equals(System.Object obj)",
                    "System.Boolean System.Object.Equals(System.Object objA, System.Object objB)",
                    "System.Boolean System.Object.ReferenceEquals(System.Object objA, System.Object objB)",
                    "System.Int32 System.Object.GetHashCode()",
                    "System.Object System.Object.MemberwiseClone()",
                    "void System.Object.Finalize()",
                    "System.String System.Object.ToString()",
                    "System.Type System.Object.GetType()"),
                Add( //C.M
                    "System.Object o1",
                    "System.Object o2"),
                Add("System.Int32* p", "System.Int32* q"), s_pop, //fixed stmt
                Add("System.IDisposable d1", "System.IDisposable d2"), s_pop, //using stmt
                Add("System.Int32 i", "System.Int32 j"), s_pop, //for loop
                s_pop, //C.M
                s_pop //C
            );

            TestLookupNames(text, expectedNames);
        }

        [WorkItem(782871, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/782871")]
        [Fact]
        public void NestedForEachLoops()
        {
            var text = @"
class C
`{
    static void M(string[] args)
    `{
        foreach (var arg in args)
        `{
            foreach (var ch in ar) // Note: not done typing 'arg'
            `{
            `}
        `}
    `}
`}
";

            var expectedNames = MakeExpectedSymbols(
                Add( //Global
                    "System",
                    "Microsoft",
                    "C"),
                Add( //C
                    "void C.M(System.String[] args)",
                    "System.Boolean System.Object.Equals(System.Object obj)",
                    "System.Boolean System.Object.Equals(System.Object objA, System.Object objB)",
                    "System.Boolean System.Object.ReferenceEquals(System.Object objA, System.Object objB)",
                    "System.Int32 System.Object.GetHashCode()",
                    "System.Object System.Object.MemberwiseClone()",
                    "void System.Object.Finalize()",
                    "System.String System.Object.ToString()",
                    "System.Type System.Object.GetType()"),
                Add("System.String[] args"), //C.Main
                Add("System.String arg"), //outer foreach
                Add("var ch"), s_pop, //inner foreach // NOTE: inference failed because the expression didn't bind.
                s_pop, //outer foreach
                s_pop, //C.Main
                s_pop //C
            );

            TestLookupNames(text, expectedNames);
        }

        [WorkItem(782871, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/782871")]
        [Fact]
        public void NestedForEachLoops_Embedded()
        {
            var text = @"
class C
`{
    static void M(string[] args)
    `{
        foreach (var arg in args)
            `foreach (var ch in arg) `;
    `}
`}
";

            var expectedNames = MakeExpectedSymbols(
                Add( //Global
                    "System",
                    "Microsoft",
                    "C"),
                Add( //C
                    "void C.M(System.String[] args)",
                    "System.Boolean System.Object.Equals(System.Object obj)",
                    "System.Boolean System.Object.Equals(System.Object objA, System.Object objB)",
                    "System.Boolean System.Object.ReferenceEquals(System.Object objA, System.Object objB)",
                    "System.Int32 System.Object.GetHashCode()",
                    "System.Object System.Object.MemberwiseClone()",
                    "void System.Object.Finalize()",
                    "System.String System.Object.ToString()",
                    "System.Type System.Object.GetType()"),
                Add("System.String[] args"), //C.Main
                Add("System.String arg"), //outer foreach
                s_pop, //outer foreach
                s_pop, //C.Main
                s_pop //C
            );

            TestLookupNames(text, expectedNames);
        }

        [WorkItem(782871, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/782871")]
        [Fact]
        public void NestedForLoops_Embedded()
        {
            var text = @"
class C
`{
    static void M(string[] args)
    `{
        for (`int i = 0; i < 10; i++)
            for (`int j = 0; j < 10; j++) `;
    `}
`}
";

            var expectedNames = MakeExpectedSymbols(
                Add( //Global
                    "System",
                    "Microsoft",
                    "C"),
                Add( //C
                    "void C.M(System.String[] args)",
                    "System.Boolean System.Object.Equals(System.Object obj)",
                    "System.Boolean System.Object.Equals(System.Object objA, System.Object objB)",
                    "System.Boolean System.Object.ReferenceEquals(System.Object objA, System.Object objB)",
                    "System.Int32 System.Object.GetHashCode()",
                    "System.Object System.Object.MemberwiseClone()",
                    "void System.Object.Finalize()",
                    "System.String System.Object.ToString()",
                    "System.Type System.Object.GetType()"),
                Add("System.String[] args"), //C.Main
                Add("System.Int32 i"), //outer for
                Add("System.Int32 j"), //inner for
                Combine(s_pop, s_pop), //outer for, inner for
                s_pop, //C.Main
                s_pop //C
            );

            TestLookupNames(text, expectedNames);
        }

        [WorkItem(782871, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/782871")]
        [Fact]
        public void NestedFixedStatements_Embedded()
        {
            var text = @"
unsafe class C
`{
    static void M(string[] args)
    `{
        `fixed (char* p = ""hello"")
            `fixed (char* q = ""goodbye"") `;
    `}
`}
";

            var expectedNames = MakeExpectedSymbols(
                Add( //Global
                    "System",
                    "Microsoft",
                    "C"),
                Add( //C
                    "void C.M(System.String[] args)",
                    "System.Boolean System.Object.Equals(System.Object obj)",
                    "System.Boolean System.Object.Equals(System.Object objA, System.Object objB)",
                    "System.Boolean System.Object.ReferenceEquals(System.Object objA, System.Object objB)",
                    "System.Int32 System.Object.GetHashCode()",
                    "System.Object System.Object.MemberwiseClone()",
                    "void System.Object.Finalize()",
                    "System.String System.Object.ToString()",
                    "System.Type System.Object.GetType()"),
                Add("System.String[] args"), //C.Main
                Add("System.Char* p"), //outer fixed
                Add("System.Char* q"), //inner fixed
                Combine(s_pop, s_pop), //outer fixed, inner fixed
                s_pop, //C.Main
                s_pop //C
            );

            TestLookupNames(text, expectedNames);
        }

        [WorkItem(782871, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/782871")]
        [Fact]
        public void NestedUsingStatements_Embedded()
        {
            var text = @"
class C
`{
    static void M(string[] args)
    `{
        `using (System.IDisposable d1 = null)
            `using (System.IDisposable d2 = null) `;
    `}
`}
";

            var expectedNames = MakeExpectedSymbols(
                Add( //Global
                    "System",
                    "Microsoft",
                    "C"),
                Add( //C
                    "void C.M(System.String[] args)",
                    "System.Boolean System.Object.Equals(System.Object obj)",
                    "System.Boolean System.Object.Equals(System.Object objA, System.Object objB)",
                    "System.Boolean System.Object.ReferenceEquals(System.Object objA, System.Object objB)",
                    "System.Int32 System.Object.GetHashCode()",
                    "System.Object System.Object.MemberwiseClone()",
                    "void System.Object.Finalize()",
                    "System.String System.Object.ToString()",
                    "System.Type System.Object.GetType()"),
                Add("System.String[] args"), //C.Main
                Add("System.IDisposable d1"), //outer using
                Add("System.IDisposable d2"), //inner using
                Combine(s_pop, s_pop), //outer using, inner using
                s_pop, //C.Main
                s_pop //C
            );

            TestLookupNames(text, expectedNames);
        }

        [Fact]
        public void GotoLabelWithUsings()
        {
            var source = @"
using System.Linq;

class Program
{
    static void Main(string[] args)
    {
        goto label1;
        if (args.Count > 3)
        {
label1:
            var x = 23;
        }
    }
}
";

            var compilation = CreateCompilation(source);

            var tree = compilation.SyntaxTrees.Single();
            var model = (Microsoft.CodeAnalysis.SemanticModel)(compilation.GetSemanticModel(tree));
            var symbols = model.LookupLabels(source.ToString().IndexOf("label1;", StringComparison.Ordinal));
            Assert.True(symbols.IsEmpty);
        }

        [Fact]
        public void GotoLabelShouldNotHaveColon()
        {
            var source = @"
class Program
{
    static void M(object o)
    {
        switch (o)
        {
            case int i:
                goto HERE;
            default:
@default:
label1:
                break;
        }
    }
}
";

            var compilation = CreateCompilation(source);
            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);
            var symbols = model.LookupLabels(source.ToString().IndexOf("HERE", StringComparison.Ordinal));
            AssertEx.SetEqual(new[] { "default", "case int i:", "label1" }, symbols.Select(s => s.ToTestDisplayString()));
        }

        [WorkItem(586815, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/586815")]
        [WorkItem(598371, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/598371")]
        [Fact]
        public void Cref()
        {
            var text = @"
`class Base`<T`>
`{
    private int Private;
`}

/// <see cref=`'explicit operator `int`(`Derived`)`'/>
class Derived : Base<int>
`{
    public static explicit operator int(Derived d) 
    `{ 
        return 0; 
    `}
`}
";

            var baseMembers = new[]
            {
                "System.Int32 Base<T>.Private",
                "System.Boolean System.Object.Equals(System.Object obj)",
                "System.Boolean System.Object.Equals(System.Object objA, System.Object objB)",
                "System.Boolean System.Object.ReferenceEquals(System.Object objA, System.Object objB)",
                "System.Int32 System.Object.GetHashCode()",
                "System.Object System.Object.MemberwiseClone()",
                "void System.Object.Finalize()",
                "System.String System.Object.ToString()",
                "System.Type System.Object.GetType()",
            };

            var derivedMembers = new[]
            {
                "System.Boolean System.Object.Equals(System.Object obj)",
                "System.Boolean System.Object.Equals(System.Object objA, System.Object objB)",
                "System.Boolean System.Object.ReferenceEquals(System.Object objA, System.Object objB)",
                "System.Int32 System.Object.GetHashCode()",
                "System.Object System.Object.MemberwiseClone()",
                "void System.Object.Finalize()",
                "System.String System.Object.ToString()",
                "System.Type System.Object.GetType()",
            };

            var derivedInheritedMembers = new[]
            {
                "System.Boolean System.Object.Equals(System.Object obj)",
                "System.Boolean System.Object.Equals(System.Object objA, System.Object objB)",
                "System.Boolean System.Object.ReferenceEquals(System.Object objA, System.Object objB)",
                "System.Int32 System.Object.GetHashCode()",
                "System.Object System.Object.MemberwiseClone()",
                "void System.Object.Finalize()",
                "System.String System.Object.ToString()",
                "System.Type System.Object.GetType()",
                "System.Int32 Base<System.Int32>.Private",
            };

            var inaccessibleGlobalMembers = new[]
            {
                "AssemblyRef",
                "FXAssembly",
                "SRETW",
                "ThisAssembly"
            };

            var expectedNames = MakeExpectedSymbols(
                Add( //Global
                    "System",
                    "Microsoft",
                    "Base<T>",
                    "Derived"),
                Add("T"), //Base decl
                Add(baseMembers), //"<T>" : Base members are in scope in type parameter list 
                s_pop, //Base members are not in scope in Base declaration list
                Add(baseMembers), //Base<T> body
                Combine(s_pop, s_pop), //Base<T> body
                Add(inaccessibleGlobalMembers), // Start of cref
                Add(derivedInheritedMembers), s_pop, // cref return type
                Add(derivedInheritedMembers), s_pop, // cref parameter type
                s_pop, // end cref
                Add(derivedMembers), //Derived body
                Add("Derived d"), s_pop, //Derived.op_Explicit body
                s_pop //Derived body
            );

            TestLookupNames(text, expectedNames);
        }

        [Fact]
        public void RecordBaseArguments_01()
        {
            var text = @"
record C(int X) : Base`(X`)
`{
`}
";
            var expectedNames = MakeExpectedSymbols(
                Add( // Global
                    "System",
                    "Microsoft",
                    "C"),
                Add( // Members + parameters
                    "System.Boolean C.Equals(Base? other)",
                    "System.Boolean C.Equals(C? other)",
                    "System.Boolean C.Equals(System.Object? obj)",
                    "System.Boolean System.Object.Equals(System.Object obj)",
                    "System.Boolean System.Object.Equals(System.Object objA, System.Object objB)",
                    "System.Boolean System.Object.ReferenceEquals(System.Object objA, System.Object objB)",
                    "System.Int32 C.GetHashCode()",
                    "System.Int32 System.Object.GetHashCode()",
                    "System.Int32 X",
                    "System.Object System.Object.MemberwiseClone()",
                    "System.String System.Object.ToString()",
                    "System.Type C.EqualityContract { get; }",
                    "System.Type System.Object.GetType()",
                    "void C.Deconstruct(out System.Int32 X)",
                    "void System.Object.Finalize()"),
                s_pop,
                Add( // Members
                    "System.Boolean C.Equals(Base? other)",
                    "System.Boolean C.Equals(C? other)",
                    "System.Boolean C.Equals(System.Object? obj)",
                    "System.Boolean System.Object.Equals(System.Object obj)",
                    "System.Boolean System.Object.Equals(System.Object objA, System.Object objB)",
                    "System.Boolean System.Object.ReferenceEquals(System.Object objA, System.Object objB)",
                    "System.Int32 C.GetHashCode()",
                    "System.Int32 C.X { get; init; }",
                    "System.Int32 System.Object.GetHashCode()",
                    "System.Object System.Object.MemberwiseClone()",
                    "System.String System.Object.ToString()",
                    "System.Type C.EqualityContract { get; }",
                    "System.Type System.Object.GetType()",
                    "void C.Deconstruct(out System.Int32 X)",
                    "void System.Object.Finalize()"),
                s_pop
            );

            TestLookupNames(text, expectedNames);
        }

        [Fact]
        public void RecordBaseArguments_02()
        {
            var text = @"
record C : Base(X)
`{
`}
";
            var expectedNames = MakeExpectedSymbols(
                Add( // Global
                    "System",
                    "Microsoft",
                    "C"),
                Add( // Members
                    "System.Boolean C.Equals(Base? other)",
                    "System.Boolean C.Equals(C? other)",
                    "System.Boolean C.Equals(System.Object? obj)",
                    "System.Boolean System.Object.Equals(System.Object obj)",
                    "System.Boolean System.Object.Equals(System.Object objA, System.Object objB)",
                    "System.Boolean System.Object.ReferenceEquals(System.Object objA, System.Object objB)",
                    "System.Int32 C.GetHashCode()",
                    "System.Int32 System.Object.GetHashCode()",
                    "System.Object System.Object.MemberwiseClone()",
                    "System.String System.Object.ToString()",
                    "System.Type C.EqualityContract { get; }",
                    "System.Type System.Object.GetType()",
                    "void System.Object.Finalize()"),
                s_pop
            );

            TestLookupNames(text, expectedNames);
        }

        [Fact]
        public void RecordBaseArguments_03()
        {
            var text = @"
partial record C(int X)
`{
`}

partial record C : Base(X, Y)
`{
`}
";
            var expectedNames = MakeExpectedSymbols(
                Add( // Global
                    "System",
                    "Microsoft",
                    "C"),
                Add( // Members
                    "System.Boolean C.Equals(Base? other)",
                    "System.Boolean C.Equals(C? other)",
                    "System.Boolean C.Equals(System.Object? obj)",
                    "System.Boolean System.Object.Equals(System.Object obj)",
                    "System.Boolean System.Object.Equals(System.Object objA, System.Object objB)",
                    "System.Boolean System.Object.ReferenceEquals(System.Object objA, System.Object objB)",
                    "System.Int32 C.GetHashCode()",
                    "System.Int32 C.X { get; init; }",
                    "System.Int32 System.Object.GetHashCode()",
                    "System.Object System.Object.MemberwiseClone()",
                    "System.String System.Object.ToString()",
                    "System.Type C.EqualityContract { get; }",
                    "System.Type System.Object.GetType()",
                    "void C.Deconstruct(out System.Int32 X)",
                    "void System.Object.Finalize()"),
                s_pop,
                Add( // Members
                    "System.Boolean C.Equals(Base? other)",
                    "System.Boolean C.Equals(C? other)",
                    "System.Boolean C.Equals(System.Object? obj)",
                    "System.Boolean System.Object.Equals(System.Object obj)",
                    "System.Boolean System.Object.Equals(System.Object objA, System.Object objB)",
                    "System.Boolean System.Object.ReferenceEquals(System.Object objA, System.Object objB)",
                    "System.Int32 C.GetHashCode()",
                    "System.Int32 C.X { get; init; }",
                    "System.Int32 System.Object.GetHashCode()",
                    "System.Object System.Object.MemberwiseClone()",
                    "System.String System.Object.ToString()",
                    "System.Type C.EqualityContract { get; }",
                    "System.Type System.Object.GetType()",
                    "void C.Deconstruct(out System.Int32 X)",
                    "void System.Object.Finalize()"),
                s_pop

            );

            TestLookupNames(text, expectedNames);
        }

        [Fact]
        public void RecordBaseArguments_04()
        {
            var text = @"
partial record C(int X) : Base`(X`)
`{
`}

partial record C : Base(X)
`{
`}
";
            var expectedNames = MakeExpectedSymbols(
                Add( // Global
                    "System",
                    "Microsoft",
                    "C"),
                Add( // Members + parameters
                    "System.Boolean C.Equals(Base? other)",
                    "System.Boolean C.Equals(C? other)",
                    "System.Boolean C.Equals(System.Object? obj)",
                    "System.Boolean System.Object.Equals(System.Object obj)",
                    "System.Boolean System.Object.Equals(System.Object objA, System.Object objB)",
                    "System.Boolean System.Object.ReferenceEquals(System.Object objA, System.Object objB)",
                    "System.Int32 C.GetHashCode()",
                    "System.Int32 System.Object.GetHashCode()",
                    "System.Int32 X",
                    "System.Object System.Object.MemberwiseClone()",
                    "System.String System.Object.ToString()",
                    "System.Type C.EqualityContract { get; }",
                    "System.Type System.Object.GetType()",
                    "void C.Deconstruct(out System.Int32 X)",
                    "void System.Object.Finalize()"),
                s_pop,
                Add( // Members
                    "System.Boolean C.Equals(Base? other)",
                    "System.Boolean C.Equals(C? other)",
                    "System.Boolean C.Equals(System.Object? obj)",
                    "System.Boolean System.Object.Equals(System.Object obj)",
                    "System.Boolean System.Object.Equals(System.Object objA, System.Object objB)",
                    "System.Boolean System.Object.ReferenceEquals(System.Object objA, System.Object objB)",
                    "System.Int32 C.GetHashCode()",
                    "System.Int32 C.X { get; init; }",
                    "System.Int32 System.Object.GetHashCode()",
                    "System.Object System.Object.MemberwiseClone()",
                    "System.String System.Object.ToString()",
                    "System.Type C.EqualityContract { get; }",
                    "System.Type System.Object.GetType()",
                    "void C.Deconstruct(out System.Int32 X)",
                    "void System.Object.Finalize()"),
                s_pop,
                Add( // Members
                    "System.Boolean C.Equals(Base? other)",
                    "System.Boolean C.Equals(C? other)",
                    "System.Boolean C.Equals(System.Object? obj)",
                    "System.Boolean System.Object.Equals(System.Object obj)",
                    "System.Boolean System.Object.Equals(System.Object objA, System.Object objB)",
                    "System.Boolean System.Object.ReferenceEquals(System.Object objA, System.Object objB)",
                    "System.Int32 C.GetHashCode()",
                    "System.Int32 C.X { get; init; }",
                    "System.Int32 System.Object.GetHashCode()",
                    "System.Object System.Object.MemberwiseClone()",
                    "System.String System.Object.ToString()",
                    "System.Type C.EqualityContract { get; }",
                    "System.Type System.Object.GetType()",
                    "void C.Deconstruct(out System.Int32 X)",
                    "void System.Object.Finalize()"),
                s_pop
            );

            TestLookupNames(text, expectedNames);
        }

        [Fact]
        public void RecordBaseArguments_05()
        {
            var text = @"
partial record C : Base(X)
`{
`}

partial record C(int X) : Base`(X`)
`{
`}
";
            var expectedNames = MakeExpectedSymbols(
                Add( // Global
                    "System",
                    "Microsoft",
                    "C"),
                Add( // Members
                    "System.Boolean C.Equals(Base? other)",
                    "System.Boolean C.Equals(C? other)",
                    "System.Boolean C.Equals(System.Object? obj)",
                    "System.Boolean System.Object.Equals(System.Object obj)",
                    "System.Boolean System.Object.Equals(System.Object objA, System.Object objB)",
                    "System.Boolean System.Object.ReferenceEquals(System.Object objA, System.Object objB)",
                    "System.Int32 C.GetHashCode()",
                    "System.Int32 C.X { get; init; }",
                    "System.Int32 System.Object.GetHashCode()",
                    "System.Object System.Object.MemberwiseClone()",
                    "System.String System.Object.ToString()",
                    "System.Type C.EqualityContract { get; }",
                    "System.Type System.Object.GetType()",
                    "void C.Deconstruct(out System.Int32 X)",
                    "void System.Object.Finalize()"),
                s_pop,
                Add( // Members + parameters
                    "System.Boolean C.Equals(Base? other)",
                    "System.Boolean C.Equals(C? other)",
                    "System.Boolean C.Equals(System.Object? obj)",
                    "System.Boolean System.Object.Equals(System.Object obj)",
                    "System.Boolean System.Object.Equals(System.Object objA, System.Object objB)",
                    "System.Boolean System.Object.ReferenceEquals(System.Object objA, System.Object objB)",
                    "System.Int32 C.GetHashCode()",
                    "System.Int32 System.Object.GetHashCode()",
                    "System.Int32 X",
                    "System.Object System.Object.MemberwiseClone()",
                    "System.String System.Object.ToString()",
                    "System.Type C.EqualityContract { get; }",
                    "System.Type System.Object.GetType()",
                    "void C.Deconstruct(out System.Int32 X)",
                    "void System.Object.Finalize()"),
                s_pop,
                Add( // Members
                    "System.Boolean C.Equals(Base? other)",
                    "System.Boolean C.Equals(C? other)",
                    "System.Boolean C.Equals(System.Object? obj)",
                    "System.Boolean System.Object.Equals(System.Object obj)",
                    "System.Boolean System.Object.Equals(System.Object objA, System.Object objB)",
                    "System.Boolean System.Object.ReferenceEquals(System.Object objA, System.Object objB)",
                    "System.Int32 C.GetHashCode()",
                    "System.Int32 C.X { get; init; }",
                    "System.Int32 System.Object.GetHashCode()",
                    "System.Object System.Object.MemberwiseClone()",
                    "System.String System.Object.ToString()",
                    "System.Type C.EqualityContract { get; }",
                    "System.Type System.Object.GetType()",
                    "void C.Deconstruct(out System.Int32 X)",
                    "void System.Object.Finalize()"),
                s_pop
            );

            TestLookupNames(text, expectedNames);
        }

        [Fact]
        public void RecordBaseArguments_06()
        {
            var text = @"
class C : Base(X)
`{
`}
";
            var expectedNames = MakeExpectedSymbols(
                Add( // Global
                    "System",
                    "Microsoft",
                    "C"),
                Add( // Members
                    "System.Boolean System.Object.Equals(System.Object obj)",
                    "System.Boolean System.Object.Equals(System.Object objA, System.Object objB)",
                    "System.Boolean System.Object.ReferenceEquals(System.Object objA, System.Object objB)",
                    "System.Int32 System.Object.GetHashCode()",
                    "System.Object System.Object.MemberwiseClone()",
                    "System.String System.Object.ToString()",
                    "System.Type System.Object.GetType()",
                    "void System.Object.Finalize()"),
                s_pop
            );

            TestLookupNames(text, expectedNames);
        }

        [Fact]
        public void RecordBaseArguments_07()
        {
            var text = @"
struct C : Base(X)
`{
`}
";
            var expectedNames = MakeExpectedSymbols(
                Add( // Global
                    "System",
                    "Microsoft",
                    "C"),
                Add( // Members
                    "System.Boolean System.Object.Equals(System.Object obj)",
                    "System.Boolean System.Object.Equals(System.Object objA, System.Object objB)",
                    "System.Boolean System.Object.ReferenceEquals(System.Object objA, System.Object objB)",
                    "System.Boolean System.ValueType.Equals(System.Object obj)",
                    "System.Int32 System.Object.GetHashCode()",
                    "System.Int32 System.ValueType.GetHashCode()",
                    "System.Object System.Object.MemberwiseClone()",
                    "System.String System.Object.ToString()",
                    "System.String System.ValueType.ToString()",
                    "System.Type System.Object.GetType()",
                    "void System.Object.Finalize()"),
                s_pop
            );

            TestLookupNames(text, expectedNames);
        }

        [Fact]
        public void RecordBaseArguments_08()
        {
            var text = @"
interface C : Base(X)
`{
`}
";
            var expectedNames = MakeExpectedSymbols(
                Add( // Global
                    "System",
                    "Microsoft",
                    "C"),
                Add( // Members
                    "System.Boolean System.Object.Equals(System.Object obj)",
                    "System.Boolean System.Object.Equals(System.Object objA, System.Object objB)",
                    "System.Boolean System.Object.ReferenceEquals(System.Object objA, System.Object objB)",
                    "System.Int32 System.Object.GetHashCode()",
                    "System.String System.Object.ToString()",
                    "System.Type System.Object.GetType()"),
                s_pop
            );

            TestLookupNames(text, expectedNames);
        }

        [Fact]
        public void RecordInitializers_01()
        {
            var text = @"
record C(int X)
`{
    int Z `= X + 1`;
`}
";
            var expectedNames = MakeExpectedSymbols(
                Add( // Global
                    "System",
                    "Microsoft",
                    "C"),
                Add( // Members
                    "System.Boolean C.Equals(C? other)",
                    "System.Boolean C.Equals(System.Object? obj)",
                    "System.Boolean System.Object.Equals(System.Object obj)",
                    "System.Boolean System.Object.Equals(System.Object objA, System.Object objB)",
                    "System.Boolean System.Object.ReferenceEquals(System.Object objA, System.Object objB)",
                    "System.Int32 C.GetHashCode()",
                    "System.Int32 C.X { get; init; }",
                    "System.Int32 C.Z",
                    "System.Int32 System.Object.GetHashCode()",
                    "System.Object System.Object.MemberwiseClone()",
                    "System.String System.Object.ToString()",
                    "System.Type C.EqualityContract { get; }",
                    "System.Type System.Object.GetType()",
                    "void C.Deconstruct(out System.Int32 X)",
                    "void System.Object.Finalize()"),
                Combine(
                    Remove("System.Int32 C.X { get; init; }"),
                    Add("System.Int32 X")
                    ),
                Combine(s_pop, s_pop),
                s_pop
            );

            TestLookupNames(text, expectedNames);
        }

        [Fact]
        public void RecordInitializers_02()
        {
            var text = @"
record C(int X)
`{
    static int Z = X + 1;
`}
";
            var expectedNames = MakeExpectedSymbols(
                Add( // Global
                    "System",
                    "Microsoft",
                    "C"),
                Add( // Members
                    "System.Boolean C.Equals(C? other)",
                    "System.Boolean C.Equals(System.Object? obj)",
                    "System.Boolean System.Object.Equals(System.Object obj)",
                    "System.Boolean System.Object.Equals(System.Object objA, System.Object objB)",
                    "System.Boolean System.Object.ReferenceEquals(System.Object objA, System.Object objB)",
                    "System.Int32 C.GetHashCode()",
                    "System.Int32 C.X { get; init; }",
                    "System.Int32 C.Z",
                    "System.Int32 System.Object.GetHashCode()",
                    "System.Object System.Object.MemberwiseClone()",
                    "System.String System.Object.ToString()",
                    "System.Type C.EqualityContract { get; }",
                    "System.Type System.Object.GetType()",
                    "void C.Deconstruct(out System.Int32 X)",
                    "void System.Object.Finalize()"),
                s_pop
            );

            TestLookupNames(text, expectedNames);
        }

        [Fact]
        public void RecordInitializers_03()
        {
            var text = @"
record C(int X)
`{
    const int Z = X + 1;
`}
";
            var expectedNames = MakeExpectedSymbols(
                Add( // Global
                    "System",
                    "Microsoft",
                    "C"),
                Add( // Members
                    "System.Boolean C.Equals(C? other)",
                    "System.Boolean C.Equals(System.Object? obj)",
                    "System.Boolean System.Object.Equals(System.Object obj)",
                    "System.Boolean System.Object.Equals(System.Object objA, System.Object objB)",
                    "System.Boolean System.Object.ReferenceEquals(System.Object objA, System.Object objB)",
                    "System.Int32 C.GetHashCode()",
                    "System.Int32 C.X { get; init; }",
                    "System.Int32 C.Z",
                    "System.Int32 System.Object.GetHashCode()",
                    "System.Object System.Object.MemberwiseClone()",
                    "System.String System.Object.ToString()",
                    "System.Type C.EqualityContract { get; }",
                    "System.Type System.Object.GetType()",
                    "void C.Deconstruct(out System.Int32 X)",
                    "void System.Object.Finalize()"),
                s_pop
            );

            TestLookupNames(text, expectedNames);
        }

        /// <summary>
        /// Given a program, calls LookupNames at each character position and verifies the results.
        /// 
        /// The input program is broken into regions using backticks, which will be removed before
        /// compilation.  The first region runs from the beginning of the string (inclusive) to the
        /// first backtick (exclusive).  The second region runs from the first backtick (exclusive)
        /// to the second backtick (exclusive).  The last region runs from the last backtick
        /// (exclusive) to one character past the end of the string (i.e. EOF) (inclusive).
        /// 
        /// For each region of the program, a list of expected names must be provided.  This method
        /// will assert if any region contains different names than expected.
        /// </summary>
        private static void TestLookupNames(string text, string[][] expectedNames)
        {
            int[] keyPositions;
            var model = GetModelAndKeyPositions(text, out keyPositions);

            // There should be one more list of expectedNames than there are backticks.
            // Number of key positions = number of backticks + 2 (start and end)
            int actualNumExpectedNames = expectedNames.Length;
            int expectedNumExpectedNames = keyPositions.Length - 2 + 1;
            Assert.True(actualNumExpectedNames == expectedNumExpectedNames, string.Format("Expected {0} sets of expected names, but found {1}", expectedNumExpectedNames, actualNumExpectedNames));

            for (int key = 0; key < keyPositions.Length - 1; key++)
            {
                int currPos = keyPositions[key];
                int nextPos = keyPositions[key + 1];

                for (int pos = currPos; pos < nextPos; pos++)
                {
                    CheckSymbols(model, key, pos, expectedNames[key]);
                }
            }
        }

        /// <summary>
        /// Strip the backticks out of "markedText" and record their positions.
        /// Return a SemanticModel for the compiled text.
        /// </summary>
        private static SemanticModel GetModelAndKeyPositions(string markedText, out int[] keyPositions)
        {
            ArrayBuilder<int> keyPositionBuilder = ArrayBuilder<int>.GetInstance();
            StringBuilder textBuilder = new StringBuilder();

            int position = 0;
            keyPositionBuilder.Add(position); //automatically add start-of-file
            foreach (var ch in markedText)
            {
                if (ch == KeyPositionMarker)
                {
                    Assert.False(keyPositionBuilder.Contains(position), "Duplicate position " + position);
                    keyPositionBuilder.Add(position);
                }
                else
                {
                    textBuilder.Append(ch);
                    position++;
                }
            }
            Assert.False(keyPositionBuilder.Contains(position), "Duplicate position " + position);
            keyPositionBuilder.Add(position); //automatically add end-of-file

            keyPositions = keyPositionBuilder.ToArrayAndFree();
            var text = textBuilder.ToString();

            var parseOptions = TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose);
            var compilation = CreateCompilationWithMscorlib40(text, parseOptions: parseOptions);
            var tree = compilation.SyntaxTrees[0];
            return compilation.GetSemanticModel(tree);
        }

        /// <summary>
        /// Assert that the result of LookupNames(position) matches the list of expected names.
        /// </summary>
        private static void CheckSymbols(SemanticModel model, int keyPositionNum, int position, IEnumerable<string> expectedSymbols)
        {
            var actualSymbols = model.LookupSymbols(position).Select(SymbolExtensions.ToTestDisplayString).ToArray();
            Array.Sort(actualSymbols);

            SyntaxToken token = model.SyntaxTree.GetCompilationUnitRoot().FindToken(position, findInsideTrivia: true);
            AssertEx.Equal(expectedSymbols, actualSymbols,
                message: string.Format("Lookup({0}) - '{1}' in '{2}' after {3}th '{4}' - \"-->\" found but not expected, \"++>\" expected but not found",
                         position, token.ToString(), token.Parent.ToString(), keyPositionNum, KeyPositionMarker));
        }

        private static string[][] MakeExpectedSymbols(params Action<Stack<string[]>>[] deltas)
        {
            int numRegions = deltas.Length;
            string[][] expectedNames = new string[numRegions][];
            Stack<string[]> stack = new Stack<string[]>();
            stack.Push(new string[0]);
            for (int i = 0; i < numRegions; i++)
            {
                deltas[i](stack);
                expectedNames[i] = stack.Peek();
            }
            return expectedNames;
        }

        /// <summary>
        /// NB first func is applied first, not last.
        /// </summary>
        private static Action<Stack<string[]>> Combine(params Action<Stack<string[]>>[] deltas)
        {
            return deltas.Aggregate((f, g) => stack =>
            {
                f(stack);
                g(stack);
            });
        }

        private static Action<Stack<string[]>> Add(params string[] added)
        {
            return stack =>
            {
                string[] prev = stack.Peek();
                string[] curr = prev.Concat(added).ToArray();
                Array.Sort(curr);
                stack.Push(curr);
            };
        }

        private static Action<Stack<string[]>> Remove(params string[] removed)
        {
            return stack =>
            {
                string[] prev = stack.Peek();
                string[] curr = prev.Where(x => !removed.Contains(x)).ToArray();
                Array.Sort(curr);
                stack.Push(curr);
            };
        }

        private static readonly Action<Stack<string[]>> s_pop = stack => stack.Pop();
    }
}
