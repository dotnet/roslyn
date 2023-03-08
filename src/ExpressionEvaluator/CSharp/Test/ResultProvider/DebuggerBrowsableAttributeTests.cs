// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Reflection;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.VisualStudio.Debugger.Clr;
using Microsoft.VisualStudio.Debugger.Evaluation;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator.UnitTests
{
    public class DebuggerBrowsableAttributeTests : CSharpResultProviderTestBase
    {
        [Fact]
        public void Never()
        {
            var source =
@"using System.Diagnostics;
[DebuggerTypeProxy(typeof(P))]
class C
{
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    object F = 1;
    object P { get { return 3; } }
}
class P
{
    public P(C c) { }
    public object G = 2;
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public object Q { get { return 4; } }
}";
            var assembly = GetAssembly(source);
            var type = assembly.GetType("C");
            var value = CreateDkmClrValue(
                value: type.Instantiate(),
                type: type,
                evalFlags: DkmEvaluationResultFlags.None);
            var evalResult = FormatResult("new C()", value);
            Verify(evalResult,
                EvalResult("new C()", "{C}", "C", "new C()", DkmEvaluationResultFlags.Expandable));
            var children = GetChildren(evalResult);
            Verify(children,
                EvalResult("G", "2", "object {int}", "new P(new C()).G"),
                EvalResult("Raw View", null, "", "new C(), raw", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Data));
            children = GetChildren(children[1]);
            Verify(children,
                EvalResult("P", "3", "object {int}", "(new C()).P", DkmEvaluationResultFlags.ReadOnly));
        }

        /// <summary>
        /// DebuggerBrowsableAttributes are not inherited.
        /// </summary>
        [Fact]
        public void Never_OverridesAndImplements()
        {
            var source =
@"using System.Diagnostics;
interface I
{
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    object P1 { get; }
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    object P2 { get; }
    object P3 { get; }
    object P4 { get; }
}
abstract class A
{
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    internal abstract object P5 { get; }
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    internal virtual object P6 { get { return 0; } }
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    internal abstract object P7 { get; }
    internal abstract object P8 { get; }
}
class B : A, I
{
    public object P1 { get { return 1; } }
    object I.P2 { get { return 2; } }
    object I.P3 { get { return 3; } }
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    object I.P4 { get { return 4; } }
    internal override object P5 { get { return 5; } }
    internal override object P6 { get { return 6; } }
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    internal override object P7 { get { return 7; } }
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    internal override object P8 { get { return 8; } }
}
class C
{
    I o = new B();
}";
            var assembly = GetAssembly(source);
            var type = assembly.GetType("C");
            var value = CreateDkmClrValue(
                value: type.Instantiate(),
                type: type,
                evalFlags: DkmEvaluationResultFlags.None);
            var evalResult = FormatResult("c", value);
            Verify(evalResult,
                EvalResult("c", "{C}", "C", "c", DkmEvaluationResultFlags.Expandable));
            var children = GetChildren(evalResult);
            Verify(children,
                EvalResult("o", "{B}", "I {B}", "c.o", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.CanFavorite));
            children = GetChildren(children[0]);
            Verify(children,
                EvalResult("I.P2", "2", "object {int}", "c.o.P2", DkmEvaluationResultFlags.ReadOnly),
                EvalResult("I.P3", "3", "object {int}", "c.o.P3", DkmEvaluationResultFlags.ReadOnly),
                EvalResult("P1", "1", "object {int}", "((B)c.o).P1", DkmEvaluationResultFlags.ReadOnly | DkmEvaluationResultFlags.CanFavorite),
                EvalResult("P5", "5", "object {int}", "((B)c.o).P5", DkmEvaluationResultFlags.ReadOnly | DkmEvaluationResultFlags.CanFavorite),
                EvalResult("P6", "6", "object {int}", "((B)c.o).P6", DkmEvaluationResultFlags.ReadOnly | DkmEvaluationResultFlags.CanFavorite));
        }

        [Fact]
        public void DuplicateAttributes()
        {
            var source =
@"using System.Diagnostics;
abstract class A
{
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public object P1;
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public object P2;
    internal object P3 => 0;
}
class B : A
{
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    new public object P1 => base.P1;
    new public object P2 => 1;
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    internal new object P3 => 2;
}";
            var assembly = GetAssembly(source);
            var type = assembly.GetType("B");
            var value = CreateDkmClrValue(
                value: type.Instantiate(),
                type: type,
                evalFlags: DkmEvaluationResultFlags.None,
                valueFlags: DkmClrValueFlags.Synthetic);
            var evalResult = FormatResult("this", value);
            Verify(evalResult,
                EvalResult("this", "{B}", "B", "this", DkmEvaluationResultFlags.Expandable));
            var children = GetChildren(evalResult);
            Verify(children,
                EvalResult("P3 (A)", "0", "object {int}", "((A)this).P3", DkmEvaluationResultFlags.ReadOnly));
        }

        /// <summary>
        /// DkmClrDebuggerBrowsableAttributes are obtained from the
        /// containing type and associated with the member name. For
        /// explicit interface implementations, the name will include
        /// namespace and type.
        /// </summary>
        [Fact]
        public void Never_ExplicitNamespaceGeneric()
        {
            var source =
@"using System.Diagnostics;
namespace N1
{
    namespace N2
    {
        class A<T>
        {
            internal interface I<U>
            {
                T P { get; }
                U Q { get; }
            }
        }
    }
    class B : N2.A<object>.I<int>
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        object N2.A<object>.I<int>.P { get { return 1; } }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public int Q { get { return 2; } }
    }
}";
            var assembly = GetAssembly(source);
            var type = assembly.GetType("N1.B");
            var value = CreateDkmClrValue(
                value: type.Instantiate(),
                type: type,
                evalFlags: DkmEvaluationResultFlags.None);
            var evalResult = FormatResult("o", value);
            Verify(evalResult,
                EvalResult("o", "{N1.B}", "N1.B", "o"));
        }

        [Fact]
        public void RootHidden()
        {
            var source =
@"using System.Diagnostics;
struct S
{
    internal S(int[] x, object[] y) : this()
    {
        this.F = x;
        this.Q = y;
    }
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    internal int[] F;
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    internal int P { get { return this.F.Length; } }
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    internal object[] Q { get; private set; }
}
class A
{
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    internal readonly B G = new B { H = 4 };
}
class B
{
    internal object H;
}";
            var assembly = GetAssembly(source);
            var typeS = assembly.GetType("S");
            var typeA = assembly.GetType("A");
            var value = CreateDkmClrValue(
                value: typeS.Instantiate(new int[] { 1, 2 }, new object[] { 3, typeA.Instantiate() }),
                type: new DkmClrType((TypeImpl)typeS),
                evalFlags: DkmEvaluationResultFlags.None);
            var evalResult = FormatResult("o", value);
            Verify(evalResult,
                EvalResult("o", "{S}", "S", "o", DkmEvaluationResultFlags.Expandable));
            var children = GetChildren(evalResult);
            Verify(children,
                EvalResult("[0]", "1", "int", "o.F[0]"),
                EvalResult("[1]", "2", "int", "o.F[1]"),
                EvalResult("[0]", "3", "object {int}", "o.Q[0]"),
                EvalResult("[1]", "{A}", "object {A}", "o.Q[1]", DkmEvaluationResultFlags.Expandable));
            Verify(GetChildren(children[3]),
                 EvalResult("H", "4", "object {int}", "((A)o.Q[1]).G.H"));
        }

        [Fact]
        public void RootHidden_Exception()
        {
            var source =
@"using System;
using System.Diagnostics;
class E : Exception
{
}
class F : E
{
    object G = 1;
}
class C
{
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    object P { get { throw new F(); } }
}";

            using (new EnsureEnglishUICulture())
            {
                var runtime = new DkmClrRuntimeInstance(ReflectionUtilities.GetMscorlib(GetAssembly(source)));
                using (runtime.Load())
                {
                    var type = runtime.GetType("C");
                    var value = type.Instantiate();
                    var evalResult = FormatResult("o", value);
                    Verify(evalResult,
                        EvalResult("o", "{C}", "C", "o", DkmEvaluationResultFlags.Expandable));
                    var children = GetChildren(evalResult);
                    Verify(children[1],
                        EvalResult("G", "1", "object {int}", null));
                    Verify(children[7],
                        EvalResult("Message", "\"Exception of type 'F' was thrown.\"", "string", null,
                            DkmEvaluationResultFlags.RawString | DkmEvaluationResultFlags.ReadOnly));
                }
            }
        }

        /// <summary>
        /// Instance of type where all members are marked
        /// [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)].
        /// </summary>
        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/934800")]
        public void RootHidden_Empty()
        {
            var source =
@"using System.Diagnostics;
class A
{
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    internal readonly object F1 = new C();
}
class B
{
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    internal readonly object F2 = new C();
}
class C
{
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    internal readonly object F3 = new object();
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    internal readonly object F4 = null;
}";
            var assembly = GetAssembly(source);
            var type = assembly.GetType("C");
            var value = CreateDkmClrValue(
                value: type.Instantiate(),
                type: type,
                evalFlags: DkmEvaluationResultFlags.None);
            var evalResult = FormatResult("o", value);
            Verify(evalResult,
                EvalResult("o", "{C}", "C", "o", DkmEvaluationResultFlags.Expandable)); // Ideally, not expandable.
            var children = GetChildren(evalResult);
            Verify(children); // No children.
        }

        [Fact]
        public void DebuggerBrowsable_GenericType()
        {
            var source =
@"using System.Diagnostics;
class C<T>
{
    internal C(T f, T g)
    {
        this.F = f;
        this.G = g;
    }
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    internal readonly T F;
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    internal readonly T G;
}
struct S
{
    internal S(object o)
    {
        this.O = o;
    }
    internal readonly object O;
}";
            var assembly = GetAssembly(source);
            var typeS = assembly.GetType("S");
            var typeC = assembly.GetType("C`1").MakeGenericType(typeS);
            var value = CreateDkmClrValue(
                value: typeC.Instantiate(typeS.Instantiate(1), typeS.Instantiate(2)),
                type: new DkmClrType((TypeImpl)typeC),
                evalFlags: DkmEvaluationResultFlags.None);
            var evalResult = FormatResult("o", value);
            Verify(evalResult,
                EvalResult("o", "{C<S>}", "C<S>", "o", DkmEvaluationResultFlags.Expandable));
            var children = GetChildren(evalResult);
            Verify(children,
                EvalResult("O", "2", "object {int}", "o.G.O", DkmEvaluationResultFlags.ReadOnly));
        }

        [Fact]
        public void RootHidden_ExplicitImplementation()
        {
            var source =
@"using System.Diagnostics;
interface I<T>
{
    T P { get; }
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    T Q { get; }
}
class A
{
    internal object F;
}
class B : I<A>
{
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    A I<A>.P { get { return new A() { F = 1 }; } }
    A I<A>.Q { get { return new A() { F = 2 }; } }
}";
            var assembly = GetAssembly(source);
            var type = assembly.GetType("B");
            var value = CreateDkmClrValue(
                value: type.Instantiate(),
                type: type,
                evalFlags: DkmEvaluationResultFlags.None);
            var evalResult = FormatResult("o", value);
            Verify(evalResult,
                EvalResult("o", "{B}", "B", "o", DkmEvaluationResultFlags.Expandable));
            var children = GetChildren(evalResult);
            Verify(children,
                EvalResult("F", "1", "object {int}", "((I<A>)o).P.F"),
                EvalResult("I<A>.Q", "{A}", "A", "((I<A>)o).Q", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly));
            Verify(GetChildren(children[1]),
                EvalResult("F", "2", "object {int}", "((I<A>)o).Q.F", DkmEvaluationResultFlags.CanFavorite));
        }

        [Fact]
        public void RootHidden_ProxyType()
        {
            var source =
@"using System.Diagnostics;
[DebuggerTypeProxy(typeof(P))]
class A
{
    internal A(int f)
    {
        this.F = f;
    }
    internal int F;
}
class P
{
    private readonly A a;
    public P(A a)
    {
        this.a = a;
    }
    public object Q { get { return this.a.F; } }
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public object R { get { return this.a.F + 1; } }
}
class B
{
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    internal object F = new A(1);
    internal object G = new A(3);
}";
            var assembly = GetAssembly(source);
            var type = assembly.GetType("B");
            var value = CreateDkmClrValue(
                value: type.Instantiate(),
                type: type,
                evalFlags: DkmEvaluationResultFlags.None);
            var evalResult = FormatResult("o", value);
            Verify(evalResult,
                EvalResult("o", "{B}", "B", "o", DkmEvaluationResultFlags.Expandable));
            var children = GetChildren(evalResult);
            Verify(children,
                EvalResult("Q", "1", "object {int}", "new P(o.F).Q", DkmEvaluationResultFlags.ReadOnly),
                EvalResult("Raw View", null, "", "o.F, raw", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Data),
                EvalResult("G", "{A}", "object {A}", "o.G", DkmEvaluationResultFlags.Expandable));
            Verify(GetChildren(children[1]),
                EvalResult("F", "1", "int", "((A)o.F).F"));
            Verify(GetChildren(children[2]),
                EvalResult("Q", "3", "object {int}", "new P(o.G).Q", DkmEvaluationResultFlags.ReadOnly),
                EvalResult("Raw View", null, "", "o.G, raw", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Data));
        }

        [Fact]
        public void RootHidden_Recursive()
        {
            var source =
@"using System.Diagnostics;
class A
{
    internal A(object o)
    {
        this.F = o;
    }
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    internal object F;
}
class B
{
    internal B(object o)
    {
        this.F = o;
    }
    internal object F;
}";
            var assembly = GetAssembly(source);
            var typeA = assembly.GetType("A");
            var typeB = assembly.GetType("B");
            var value = CreateDkmClrValue(
                value: typeA.Instantiate(typeA.Instantiate(typeA.Instantiate(typeB.Instantiate(4)))),
                type: new DkmClrType((TypeImpl)typeA),
                evalFlags: DkmEvaluationResultFlags.None);
            var evalResult = FormatResult("o", value);
            Verify(evalResult,
                EvalResult("o", "{A}", "A", "o", DkmEvaluationResultFlags.Expandable));
            var children = GetChildren(evalResult);
            Verify(children,
                EvalResult("F", "4", "object {int}", "((B)((A)((A)o.F).F).F).F"));
        }

        [Fact]
        public void RootHidden_OnStaticMembers()
        {
            var source =
@"using System.Diagnostics;
class A
{
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    internal static object F = new B();
}
class B
{
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    internal static object P { get { return new C(); } }
    internal static object G = 1;
}
class C
{
    internal static object Q { get { return 3; } }
    internal object H = 2;
}";
            var assembly = GetAssembly(source);
            var type = assembly.GetType("A");
            var value = CreateDkmClrValue(
                value: type.Instantiate(),
                type: type,
                evalFlags: DkmEvaluationResultFlags.None);
            var evalResult = FormatResult("o", value);
            Verify(evalResult,
                EvalResult("o", "{A}", "A", "o", DkmEvaluationResultFlags.Expandable));
            var children = GetChildren(evalResult);
            Verify(children,
                EvalResult("Static members", null, "", "A", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Class));
            children = GetChildren(children[0]);
            Verify(children,
                EvalResult("Static members", null, "", "B", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Class));
            children = GetChildren(children[0]);
            Verify(children,
                EvalResult("G", "1", "object {int}", "B.G"),
                EvalResult("H", "2", "object {int}", "((C)B.P).H"),
                EvalResult("Static members", null, "", "C", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Class));
            children = GetChildren(children[2]);
            Verify(children,
                EvalResult("Q", "3", "object {int}", "C.Q", DkmEvaluationResultFlags.ReadOnly));
        }

        // Dev12 exposes the contents of RootHidden members even
        // if the members are private (e.g.: ImmutableArray<T>).
        [Fact]
        public void RootHidden_OnNonPublicMembers()
        {
            var source =
@"using System.Diagnostics;
public class C<T>
{
    public C(params T[] items)
    {
        this.items = items;
    }
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    private T[] items;
}";
            var assembly = GetAssembly(source);
            var runtime = new DkmClrRuntimeInstance(new Assembly[0]);
            var type = assembly.GetType("C`1").MakeGenericType(typeof(int));
            var value = CreateDkmClrValue(
                value: type.Instantiate(1, 2, 3),
                type: new DkmClrType(runtime.DefaultModule, runtime.DefaultAppDomain, (TypeImpl)type),
                evalFlags: DkmEvaluationResultFlags.None);
            var evalResult = FormatResult("o", value, inspectionContext: CreateDkmInspectionContext(DkmEvaluationFlags.HideNonPublicMembers));
            Verify(evalResult,
                EvalResult("o", "{C<int>}", "C<int>", "o", DkmEvaluationResultFlags.Expandable));
            var children = GetChildren(evalResult);
            Verify(children,
                EvalResult("[0]", "1", "int", "o.items[0]"),
                EvalResult("[1]", "2", "int", "o.items[1]"),
                EvalResult("[2]", "3", "int", "o.items[2]"));
        }

        // Dev12 does not merge "Static members" (or "Non-Public
        // members") across multiple RootHidden members. In
        // other words, there may be multiple "Static members" (or
        // "Non-Public members") rows within the same container.
        [Fact]
        public void RootHidden_WithStaticAndNonPublicMembers()
        {
            var source =
@"using System.Diagnostics;
public class A
{
    public static int PA { get { return 1; } }
    internal int FA = 2;
}
public class B
{
    internal int PB { get { return 3; } }
    public static int FB = 4;
}
public class C
{
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public readonly object FA = new A();
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public object PB { get { return new B(); } }
    public static int PC { get { return 5; } }
    internal int FC = 6;
}";
            var assembly = GetAssembly(source);
            var runtime = new DkmClrRuntimeInstance(new Assembly[0]);
            var type = assembly.GetType("C");
            var value = CreateDkmClrValue(
                value: type.Instantiate(),
                type: new DkmClrType(runtime.DefaultModule, runtime.DefaultAppDomain, (TypeImpl)type),
                evalFlags: DkmEvaluationResultFlags.None);
            var inspectionContext = CreateDkmInspectionContext(DkmEvaluationFlags.HideNonPublicMembers);
            var evalResult = FormatResult("o", value, inspectionContext: inspectionContext);
            Verify(evalResult,
                EvalResult("o", "{C}", "C", "o", DkmEvaluationResultFlags.Expandable));
            var children = GetChildren(evalResult, inspectionContext: inspectionContext);
            Verify(children,
                EvalResult("Static members", null, "", "A", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Class),
                EvalResult("Non-Public members", null, "", "o.FA, hidden", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Data),
                EvalResult("Static members", null, "", "B", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Class),
                EvalResult("Non-Public members", null, "", "o.PB, hidden", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Data),
                EvalResult("Static members", null, "", "C", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Class),
                EvalResult("Non-Public members", null, "", "o, hidden", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Data));
            Verify(GetChildren(children[0]),
                EvalResult("PA", "1", "int", "A.PA", DkmEvaluationResultFlags.ReadOnly));
            Verify(GetChildren(children[1]),
                EvalResult("FA", "2", "int", "((A)o.FA).FA"));
            Verify(GetChildren(children[2]),
                EvalResult("FB", "4", "int", "B.FB"));
            Verify(GetChildren(children[3]),
                EvalResult("PB", "3", "int", "((B)o.PB).PB", DkmEvaluationResultFlags.ReadOnly));
            Verify(GetChildren(children[4]),
                EvalResult("PC", "5", "int", "C.PC", DkmEvaluationResultFlags.ReadOnly));
            Verify(GetChildren(children[5]),
                EvalResult("FC", "6", "int", "o.FC"));
        }

        [Fact]
        public void ConstructedGenericType()
        {
            var source = @"
using System.Diagnostics;

public class C<T>
{
    public T X;
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public T Y;
}
";
            var assembly = GetAssembly(source);
            var type = assembly.GetType("C`1").MakeGenericType(typeof(int));
            var value = CreateDkmClrValue(
                value: type.Instantiate(),
                type: type,
                evalFlags: DkmEvaluationResultFlags.None);
            var evalResult = FormatResult("o", value);
            Verify(evalResult,
                EvalResult("o", "{C<int>}", "C<int>", "o", DkmEvaluationResultFlags.Expandable));

            Verify(GetChildren(evalResult),
                EvalResult("X", "0", "int", "o.X", DkmEvaluationResultFlags.CanFavorite));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18581")]
        public void AccessibilityNotTrumpedByAttribute()
        {
            var source =
@"using System.Diagnostics;
class C
{
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private int[] _someArray = { 10, 20 };

    private object SomethingPrivate = 3;

    [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
    internal object InternalCollapsed { get { return 1; } }
    [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
    private object PrivateCollapsed { get { return 3; } }
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    private int[] PrivateRootHidden { get { return _someArray; } }
}";
            var assembly = GetAssembly(source);
            var type = assembly.GetType("C");
            var value = CreateDkmClrValue(
                value: type.Instantiate(),
                type: type,
                evalFlags: DkmEvaluationResultFlags.None);
            var evalResult = FormatResult("new C()", value, inspectionContext: CreateDkmInspectionContext(DkmEvaluationFlags.HideNonPublicMembers));
            Verify(evalResult,
                EvalResult("new C()", "{C}", "C", "new C()", DkmEvaluationResultFlags.Expandable));
            var children = GetChildren(evalResult);
            Verify(children,
                EvalResult("[0]", "10", "int", "(new C()).PrivateRootHidden[0]"),
                EvalResult("[1]", "20", "int", "(new C()).PrivateRootHidden[1]"),
                EvalResult("Non-Public members", null, "", "new C(), hidden", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Data));
            var nonPublicChildren = GetChildren(children[2]);
            Verify(nonPublicChildren,
                EvalResult("InternalCollapsed", "1", "object {int}", "(new C()).InternalCollapsed", DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Property, DkmEvaluationResultAccessType.Internal),
                EvalResult("PrivateCollapsed", "3", "object {int}", "(new C()).PrivateCollapsed", DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Property, DkmEvaluationResultAccessType.Private),
                EvalResult("SomethingPrivate", "3", "object {int}", "(new C()).SomethingPrivate", DkmEvaluationResultFlags.None, DkmEvaluationResultCategory.Data, DkmEvaluationResultAccessType.Private));
        }
    }
}
