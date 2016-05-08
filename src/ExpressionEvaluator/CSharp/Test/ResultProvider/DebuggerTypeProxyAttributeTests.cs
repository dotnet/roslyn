// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.VisualStudio.Debugger.Evaluation;
using Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation;
using System;
using System.Linq;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator.UnitTests
{
    public class DebuggerTypeProxyAttributeTests : CSharpResultProviderTestBase
    {
        [Fact]
        public void Proxy()
        {
            var source =
@"using System.Diagnostics;
[DebuggerTypeProxy(typeof(P))]
class C
{
    internal C(int f)
    {
        this.F = f;
    }
    internal int F;
}
class P
{
    private readonly C c;
    public P(C c)
    {
        this.c = c;
        this.F1 = c.F;
        this.F2 = c.F + 1;
        this.F3 = c.F + 2;
        this.F4 = c.F + 3;
        this.F5 = c.F + 4;
    }
    public int F1;
    internal int F2;
    protected int F3;
    protected internal int F4;
    private int F5;
    public object P1 { get { return this.F1; } }
    internal object P2 { get { return this.F2; } }
    protected object P3 { get { return this.F3; } }
    protected internal object P4 { get { return this.F4; } }
    private object P5 { get { return this.F5; } }
    public object P6 { set { } }
    internal object P7 { private get; set; }
}
";
            var assembly = GetAssembly(source);
            var type = assembly.GetType("C");
            var rootExpr = "new C(2)";
            var value = CreateDkmClrValue(
                value: type.Instantiate(2),
                type: type,
                evalFlags: DkmEvaluationResultFlags.None);
            var evalResult = FormatResult(rootExpr, value);
            Verify(evalResult,
                EvalResult(rootExpr, "{C}", "C", "new C(2)", DkmEvaluationResultFlags.Expandable));

            var children = GetChildren(evalResult);
            Verify(children,
                EvalResult("F1", "2", "int", "new P(new C(2)).F1"),
                EvalResult("F3", "4", "int", "new P(new C(2)).F3"),
                EvalResult("F4", "5", "int", "new P(new C(2)).F4"),
                EvalResult("P1", "2", "object {int}", "new P(new C(2)).P1", DkmEvaluationResultFlags.ReadOnly),
                EvalResult("P3", "4", "object {int}", "new P(new C(2)).P3", DkmEvaluationResultFlags.ReadOnly),
                EvalResult("P4", "5", "object {int}", "new P(new C(2)).P4", DkmEvaluationResultFlags.ReadOnly),
                EvalResult("Raw View", null, "", "new C(2), raw", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Data));

            // "Raw View".
            children = GetChildren(children[children.Length - 1]);
            Verify(children,
                EvalResult("F", "2", "int", "(new C(2)).F"));
        }

        /// <summary>
        /// Proxy is used, even if associated with base type.
        /// </summary>
        [Fact]
        public void ProxyOnBase()
        {
            var source =
@"using System.Diagnostics;
[DebuggerTypeProxy(typeof(P))]
class A
{
    internal A(object f)
    {
        this.F = f;
    }
    internal readonly object F;
}
class B : A
{
    internal B(object f, object g) : base(f)
    {
        this.G = g;
    }
    internal readonly object G;
}
class C : B
{
    internal C(int f) : base(f, f + 1)
    {
    }
}
class P
{
    private readonly A a;
    public P(A a)
    {
        this.a = a;
    }
    public object PF
    {
        get { return this.a.F; }
    }
}";
            var assembly = GetAssembly(source);
            var type = assembly.GetType("C");
            var value = CreateDkmClrValue(
                value: type.Instantiate(3),
                type: type,
                evalFlags: DkmEvaluationResultFlags.None);
            var rootExpr = "new C(3)";
            var evalResult = FormatResult(rootExpr, value);
            Verify(evalResult,
                EvalResult(rootExpr, "{C}", "C", rootExpr, DkmEvaluationResultFlags.Expandable));
            var children = GetChildren(evalResult);
            Verify(children,
                EvalResult("PF", "3", "object {int}", "new P(new C(3)).PF", DkmEvaluationResultFlags.ReadOnly),
                EvalResult("Raw View", null, "", "new C(3), raw", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Data));
            children = GetChildren(children[1]);
            Verify(children,
                EvalResult("F", "3", "object {int}", "(new C(3)).F", DkmEvaluationResultFlags.ReadOnly),
                EvalResult("G", "4", "object {int}", "(new C(3)).G", DkmEvaluationResultFlags.ReadOnly));
        }

        /// <summary>
        /// Proxy associated with base type of runtime type.
        /// </summary>
        [Fact]
        public void ProxyOnRuntimeTypeBase()
        {
            var source =
@"using System.Diagnostics;
[DebuggerTypeProxy(typeof(P))]
class A
{
    internal A(object f)
    {
        this.F = f;
    }
    internal readonly object F;
}
class B : A
{
    internal B(object f) : base(f)
    {
    }
}
class C
{
    A A = new B(4);
}
class P
{
    private readonly A a;
    public P(A a)
    {
        this.a = a;
    }
    public object G
    {
        get { return this.a.F; }
    }
}";
            var assembly = GetAssembly(source);
            var type = assembly.GetType("C");
            var value = CreateDkmClrValue(
                value: type.Instantiate(),
                type: type,
                evalFlags: DkmEvaluationResultFlags.None);
            var rootExpr = "new C()";
            var evalResult = FormatResult(rootExpr, value);
            Verify(evalResult,
                EvalResult(rootExpr, "{C}", "C", rootExpr, DkmEvaluationResultFlags.Expandable));
            var children = GetChildren(evalResult);
            Verify(children,
                EvalResult("A", "{B}", "A {B}", "(new C()).A", DkmEvaluationResultFlags.Expandable));
            children = GetChildren(children[0]);
            Verify(children,
                EvalResult("G", "4", "object {int}", "new P((new C()).A).G", DkmEvaluationResultFlags.ReadOnly),
                EvalResult("Raw View", null, "", "(new C()).A, raw", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Data));
            children = GetChildren(children[1]);
            Verify(children,
                EvalResult("F", "4", "object {int}", "(new C()).A.F", DkmEvaluationResultFlags.ReadOnly));
        }

        /// <summary>
        /// Proxy associated with runtime type.
        /// </summary>
        [Fact]
        public void ProxyOnRuntimeType()
        {
            var source =
@"using System.Diagnostics;
class A
{
    internal A(object f)
    {
        this.F = f;
    }
    internal readonly object F;
}
[DebuggerTypeProxy(typeof(P))]
class B : A
{
    internal B(object f) : base(f)
    {
    }
}
class C
{
    A A = new B(4);
}
class P
{
    private readonly B b;
    public P(B b)
    {
        this.b = b;
    }
    public object G
    {
        get { return this.b.F; }
    }
}";
            var assembly = GetAssembly(source);
            var type = assembly.GetType("C");
            var value = CreateDkmClrValue(
                value: type.Instantiate(),
                type: type,
                evalFlags: DkmEvaluationResultFlags.None);
            var rootExpr = "new C()";
            var evalResult = FormatResult(rootExpr, value);
            Verify(evalResult,
                EvalResult(rootExpr, "{C}", "C", rootExpr, DkmEvaluationResultFlags.Expandable));
            var children = GetChildren(evalResult);
            Verify(children,
                EvalResult("A", "{B}", "A {B}", "(new C()).A", DkmEvaluationResultFlags.Expandable));
            children = GetChildren(children[0]);
            Verify(children,
                EvalResult("G", "4", "object {int}", "new P((new C()).A).G", DkmEvaluationResultFlags.ReadOnly),
                EvalResult("Raw View", null, "", "(new C()).A, raw", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Data));
            children = GetChildren(children[1]);
            Verify(children,
                EvalResult("F", "4", "object {int}", "(new C()).A.F", DkmEvaluationResultFlags.ReadOnly));
        }

        [Fact]
        public void GenericTypeWithGenericTypeArgument()
        {
            var source =
@"using System.Diagnostics;
[DebuggerTypeProxy(typeof(PA<>))]
class A<T>
{
    public readonly T F;
    public A(T f)
    {
        this.F = f;
    }
}
internal class PA<T>
{
    public readonly T PF;
    public PA(A<T> a)
    {
        this.PF = a.F;
    }
}
[DebuggerTypeProxy(typeof(PB<>))]
class B<T>
{
    public readonly T G;
    public B(T g)
    {
        this.G = g;
    }
}
internal class PB<T>
{
    public readonly T PG;
    public PB(B<T> b)
    {
        this.PG = b.G;
    }
}
class C
{
    B<A<string>> b = new B<A<string>>(new A<string>(""A""));
}";
            var assembly = GetAssembly(source);
            var type = assembly.GetType("C");
            var value = CreateDkmClrValue(
                value: type.Instantiate(),
                type: type,
                evalFlags: DkmEvaluationResultFlags.None);
            var rootExpr = "new C()";
            var evalResult = FormatResult(rootExpr, value);
            Verify(evalResult,
                EvalResult(rootExpr, "{C}", "C", rootExpr, DkmEvaluationResultFlags.Expandable));
            var children = GetChildren(evalResult);
            Verify(children,
                EvalResult("b", "{B<A<string>>}", "B<A<string>>", "(new C()).b", DkmEvaluationResultFlags.Expandable));
            children = GetChildren(children[0]);
            Verify(children,
                EvalResult("PG", "{A<string>}", "A<string>", "new PB<A<string>>((new C()).b).PG", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly),
                EvalResult("Raw View", null, "", "(new C()).b, raw", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Data));
            var moreChildren = GetChildren(children[1]);
            Verify(moreChildren,
                EvalResult("G", "{A<string>}", "A<string>", "(new C()).b.G", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly));
            moreChildren = GetChildren(children[0]);
            Verify(moreChildren,
                EvalResult("PF", "\"A\"", "string", "new PA<string>(new PB<A<string>>((new C()).b).PG).PF", DkmEvaluationResultFlags.RawString | DkmEvaluationResultFlags.ReadOnly),
                EvalResult("Raw View", null, "", "new PB<A<string>>((new C()).b).PG, raw", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Data));
            moreChildren = GetChildren(moreChildren[1]);
            Verify(moreChildren,
                EvalResult("F", "\"A\"", "string", "(new PB<A<string>>((new C()).b).PG).F", DkmEvaluationResultFlags.RawString | DkmEvaluationResultFlags.ReadOnly));
        }

        [Fact]
        public void GenericTypeProxyWrongArity()
        {
            var source =
@"using System.Diagnostics;
[DebuggerTypeProxy(typeof(PA<,>))]
class A<T>
{
    public readonly T F;
    public A(T f)
    {
        this.F = f;
    }
}
internal class PA<T, U>
{
    public readonly T PF;
    public PA(A<T> a)
    {
        this.PF = a.F;
    }
}
[DebuggerTypeProxy(typeof(B<>.PB<>))]
class B<T>
{
    public readonly T F;
    public B(T f)
    {
        this.F = f;
    }
    internal class PB<U>
    {
        public readonly T PF;
        public PB(B<T> b)
        {
            this.PF = b.F;
        }
    }
}
[DebuggerTypeProxy(typeof(C<>.PC))]
class C<T>
{
    public readonly T F;
    public C(T f)
    {
        this.F = f;
    }
    internal class PC
    {
        public readonly T PF;
        public PC(C<T> c)
        {
            this.PF = c.F;
        }
    }
}
class C
{
    A<int> a = new A<int>(1);
    B<object> b = new B<object>(2);
    C<short> c = new C<short>((short)3);
}";
            var assembly = GetAssembly(source);
            var type = assembly.GetType("C");
            var value = CreateDkmClrValue(
                value: type.Instantiate(),
                type: type,
                evalFlags: DkmEvaluationResultFlags.None);
            var rootExpr = "new C()";
            var evalResult = FormatResult(rootExpr, value);
            Verify(evalResult,
                EvalResult(rootExpr, "{C}", "C", rootExpr, DkmEvaluationResultFlags.Expandable));
            var children = GetChildren(evalResult);
            Verify(children,
                EvalResult("a", "{A<int>}", "A<int>", "(new C()).a", DkmEvaluationResultFlags.Expandable),
                EvalResult("b", "{B<object>}", "B<object>", "(new C()).b", DkmEvaluationResultFlags.Expandable),
                EvalResult("c", "{C<short>}", "C<short>", "(new C()).c", DkmEvaluationResultFlags.Expandable));

            // A<int> a = new A<int>(1);
            var more = GetChildren(children[0]);
            Verify(more,
                EvalResult("F", "1", "int", "(new C()).a.F", DkmEvaluationResultFlags.ReadOnly));

            // B<object> b = new B<object>(2);
            more = GetChildren(children[1]);
            Verify(more,
                EvalResult("F", "2", "object {int}", "(new C()).b.F", DkmEvaluationResultFlags.ReadOnly));

            // C<short> c = new C<short>((short)3);
            more = GetChildren(children[2]);
            Verify(more,
                EvalResult("PF", "3", "short", "new C<short>.PC((new C()).c).PF", DkmEvaluationResultFlags.ReadOnly),
                EvalResult("Raw View", null, "", "(new C()).c, raw", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Data));
            more = GetChildren(more[1]);
            Verify(more,
                EvalResult("F", "3", "short", "(new C()).c.F", DkmEvaluationResultFlags.ReadOnly));
        }

        [Fact]
        public void ProxyOnGenericBase()
        {
            var source =
@"using System.Diagnostics;
[DebuggerTypeProxy(typeof(P<,>))]
class A<T, U>
{
    internal readonly T F;
    internal readonly U G;
    internal A(T t, U u)
    {
        this.F = t;
        this.G = u;
    }
}
internal class P<T, U>
{
    private readonly A<T, U> a;
    public P(A<T, U> a)
    {
        this.a = a;
    }
    public T PF { get { return this.a.F; } }
    public U PG { get { return this.a.G; } }
}
// Arrays.
class B1<T, U> : A<T[], U[,,]>
{
    internal B1(T[] x, U[,,] y) : base(x, y)
    {
    } 
}
// Pointers.
unsafe class B2<T> : A<A<int*[], T>, A<T, void**[]>>
{
    internal B2() : base(null, null)
    {
    }
}
// Nested classes.
class B3<T> : A<B3<T>.C1, B3<T>.C2<object>>
{
    internal B3() : base(new C1(), new C2<object>())
    {
    }
    internal class C1
    {
    }
    internal class C2<U>
    {
    }
}
class B4 : A<object, object>
{
    internal B4(object o) : base(o, o)
    {
    }
}
// Generic derived type.
class C4<T> : B4
{
    internal C4(T t) : base(t)
    {
    }
}
class C
{
    B1<int, object> _1 = new B1<int, object>(new[] { 1, 2, 3 }, new[,,] { { { (object)null } } });
    B2<object> _2 = new B2<object>();
    B3<int> _3 = new B3<int>();
    C4<string> _4 = new C4<string>(string.Empty);
}";
            var assembly = GetUnsafeAssembly(source);
            var type = assembly.GetType("C");
            var value = CreateDkmClrValue(
                value: type.Instantiate(),
                type: type,
                evalFlags: DkmEvaluationResultFlags.None);
            var rootExpr = "new C()";
            var evalResult = FormatResult(rootExpr, value);
            Verify(evalResult,
                EvalResult(rootExpr, "{C}", "C", rootExpr, DkmEvaluationResultFlags.Expandable));
            var children = GetChildren(evalResult);
            Verify(children,
                EvalResult("_1", "{B1<int, object>}", "B1<int, object>", "(new C())._1", DkmEvaluationResultFlags.Expandable),
                EvalResult("_2", "{B2<object>}", "B2<object>", "(new C())._2", DkmEvaluationResultFlags.Expandable),
                EvalResult("_3", "{B3<int>}", "B3<int>", "(new C())._3", DkmEvaluationResultFlags.Expandable),
                EvalResult("_4", "{C4<string>}", "C4<string>", "(new C())._4", DkmEvaluationResultFlags.Expandable));
            Verify(GetChildren(children[0]),
                EvalResult("PF", "{int[3]}", "int[]", "new P<int[], object[,,]>((new C())._1).PF", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly),
                EvalResult("PG", "{object[1, 1, 1]}", "object[,,]", "new P<int[], object[,,]>((new C())._1).PG", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly),
                EvalResult("Raw View", null, "", "(new C())._1, raw", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Data));
            Verify(GetChildren(children[1]),
                EvalResult("PF", "null", "A<int*[], object>", "new P<A<int*[], object>, A<object, void**[]>>((new C())._2).PF", DkmEvaluationResultFlags.ReadOnly),
                EvalResult("PG", "null", "A<object, void**[]>", "new P<A<int*[], object>, A<object, void**[]>>((new C())._2).PG", DkmEvaluationResultFlags.ReadOnly),
                EvalResult("Raw View", null, "", "(new C())._2, raw", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Data));
            Verify(GetChildren(children[2]),
                EvalResult("PF", "{B3<int>.C1}", "B3<int>.C1", "new P<B3<int>.C1, B3<int>.C2<object>>((new C())._3).PF", DkmEvaluationResultFlags.ReadOnly),
                EvalResult("PG", "{B3<int>.C2<object>}", "B3<int>.C2<object>", "new P<B3<int>.C1, B3<int>.C2<object>>((new C())._3).PG", DkmEvaluationResultFlags.ReadOnly),
                EvalResult("Raw View", null, "", "(new C())._3, raw", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Data));
            Verify(GetChildren(children[3]),
                EvalResult("PF", "\"\"", "object {string}", "new P<object, object>((new C())._4).PF", DkmEvaluationResultFlags.RawString | DkmEvaluationResultFlags.ReadOnly),
                EvalResult("PG", "\"\"", "object {string}", "new P<object, object>((new C())._4).PG", DkmEvaluationResultFlags.RawString | DkmEvaluationResultFlags.ReadOnly),
                EvalResult("Raw View", null, "", "(new C())._4, raw", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Data));
        }

        [Fact, WorkItem(1024016, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1024016")]
        public void NonGenericProxyOnGenericBase()
        {
            var source =
@"using System.Diagnostics;
class A
{
    internal object F = 1;
}
[DebuggerTypeProxy(typeof(P))]
class B<T> : A
{

}
class P
{
    public P(A a)
    {
        this.G = (int)a.F + 1;
    }
    public readonly object G;
}";
            var assembly = GetAssembly(source);
            var type = assembly.GetType("B`1").MakeGenericType(typeof(object));
            var value = CreateDkmClrValue(
                value: type.Instantiate(),
                type: type,
                evalFlags: DkmEvaluationResultFlags.None);
            var rootExpr = "o"; // var o = new B<object>();
            var evalResult = FormatResult(rootExpr, value);
            Verify(evalResult,
                EvalResult(rootExpr, "{B<object>}", "B<object>", rootExpr, DkmEvaluationResultFlags.Expandable));
            var children = GetChildren(evalResult);
            Verify(children,
                EvalResult("G", "2", "object {int}", "new P(o).G", DkmEvaluationResultFlags.ReadOnly),
                EvalResult("Raw View", null, "", "o, raw", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Data));
            children = GetChildren(children[1]);
            Verify(children,
                EvalResult("F", "1", "object {int}", "o.F"));
        }

        /// <summary>
        /// Null instance should not be expandable.
        /// </summary>
        [Fact]
        public void NullInstance()
        {
            var source =
@"using System.Diagnostics;
[DebuggerTypeProxy(typeof(P))]
class A
{
    internal object F;
}
internal class P
{
    private readonly A a;
    public P(A a)
    {
        this.a = a;
    }
    public object PF { get { return (this.a == null) ? null : this.a.F; } }
}
class B
{
    A a = null;
}";
            var assembly = GetAssembly(source);
            var type = assembly.GetType("B");
            var value = CreateDkmClrValue(
                value: type.Instantiate(),
                type: type,
                evalFlags: DkmEvaluationResultFlags.None);
            var rootExpr = "new C()";
            var evalResult = FormatResult(rootExpr, value);
            Verify(evalResult,
                EvalResult(rootExpr, "{B}", "B", rootExpr, DkmEvaluationResultFlags.Expandable));
            var children = GetChildren(evalResult);
            Verify(children,
                EvalResult("a", "null", "A", "(new C()).a"));
        }

        [Fact]
        public void EmptyProxy()
        {
            var source =
@"using System.Diagnostics;
[DebuggerTypeProxy(typeof(P))]
class C
{
    internal int F = 1;
}
class P
{
    public P(C c)
    {
    }
}";
            var assembly = GetAssembly(source);
            var type = assembly.GetType("C");
            var value = CreateDkmClrValue(
                value: type.Instantiate(),
                type: type,
                evalFlags: DkmEvaluationResultFlags.None);
            var rootExpr = "new C()";
            var evalResult = FormatResult(rootExpr, value);
            Verify(evalResult,
                EvalResult(rootExpr, "{C}", "C", rootExpr, DkmEvaluationResultFlags.Expandable));
            var children = GetChildren(evalResult);
            Verify(children,
                EvalResult("Raw View", null, "", "new C(), raw", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Data));
            children = GetChildren(children[0]);
            Verify(children,
                EvalResult("F", "1", "int", "(new C()).F"));
        }

        /// <summary>
        /// Native EE includes non-expandable "Raw View"
        /// if empty (rather than dropping "Raw View").
        /// </summary>
        [Fact]
        public void EmptyRawView()
        {
            var source =
@"using System.Diagnostics;
[DebuggerTypeProxy(typeof(P))]
class C
{
    internal int GetF()
    {
        return 3;
    }
}
class P
{
    private readonly C c;
    public P(C c)
    {
        this.c = c;
    }
    public object F
    {
        get { return this.c.GetF(); }
    }
}";
            var assembly = GetAssembly(source);
            var type = assembly.GetType("C");
            var value = CreateDkmClrValue(
                value: type.Instantiate(),
                type: type,
                evalFlags: DkmEvaluationResultFlags.None);
            var rootExpr = "new C()";
            var evalResult = FormatResult(rootExpr, value);
            Verify(evalResult,
                EvalResult(rootExpr, "{C}", "C", rootExpr, DkmEvaluationResultFlags.Expandable));
            var children = GetChildren(evalResult);
            Verify(children,
                EvalResult("F", "3", "object {int}", "new P(new C()).F", DkmEvaluationResultFlags.ReadOnly),
                EvalResult("Raw View", null, "", "new C(), raw", DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Data));
        }

        /// <summary>
        /// Struct used as DebuggerTypeProxy type.
        /// </summary>
        [Fact]
        public void ValueTypeProxy()
        {
            var source =
@"using System.Diagnostics;
[DebuggerTypeProxy(typeof(S))]
class C
{
    internal int F = 3;
}
struct S
{
    private readonly C c;
    public S(C c)
    {
        this.c = c;
    }
    public object P
    {
        get { return this.c.F + 1; }
    }
}";
            var assembly = GetAssembly(source);
            var type = assembly.GetType("C");
            var value = CreateDkmClrValue(
                value: type.Instantiate(),
                type: type,
                evalFlags: DkmEvaluationResultFlags.None);
            var rootExpr = "new C()";
            var evalResult = FormatResult(rootExpr, value);
            Verify(evalResult,
                EvalResult(rootExpr, "{C}", "C", rootExpr, DkmEvaluationResultFlags.Expandable));
            var children = GetChildren(evalResult);
            Verify(children,
                EvalResult("P", "4", "object {int}", "new S(new C()).P", DkmEvaluationResultFlags.ReadOnly),
                EvalResult("Raw View", null, "", "new C(), raw", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Data));
            children = GetChildren(children[1]);
            Verify(children,
                EvalResult("F", "3", "int", "(new C()).F"));
        }

        /// <summary>
        /// DebuggerTypeProxy type with base class.
        /// </summary>
        [Fact]
        public void ProxyWithBaseType()
        {
            var source =
@"using System.Diagnostics;
[DebuggerTypeProxy(typeof(B))]
class C
{
    internal int F = 4;
}
class A
{
    public object G;
}
class B : A
{
    public B(C c)
    {
        this.G = c.F + 1;
    }
}";
            var assembly = GetAssembly(source);
            var type = assembly.GetType("C");
            var value = CreateDkmClrValue(
                value: type.Instantiate(),
                type: type,
                evalFlags: DkmEvaluationResultFlags.None);
            var rootExpr = "new C()";
            var evalResult = FormatResult(rootExpr, value);
            Verify(evalResult,
                EvalResult(rootExpr, "{C}", "C", rootExpr, DkmEvaluationResultFlags.Expandable));
            var children = GetChildren(evalResult);
            Verify(children,
                EvalResult("G", "5", "object {int}", "new B(new C()).G"),
                EvalResult("Raw View", null, "", "new C(), raw", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Data));
            var more = GetChildren(children[1]);
            Verify(more,
                EvalResult("F", "4", "int", "(new C()).F"));
        }

        /// <summary>
        /// Properties with parameters should
        /// not be included in expansion.
        /// </summary>
        [Fact]
        public void HideIndexers()
        {
            var source =
@"using System.Diagnostics;
[DebuggerTypeProxy(typeof(P))]
class C
{
    internal object this[int index]
    {
        get { return index; }
    }
    internal object this[int x, int y]
    {
        set { }
    }
}
class P
{
    private readonly C c;
    public P(C c)
    {
        this.c = c;
    }
    public object F
    {
        get { return this.c[3]; }
    }
    public object this[int index]
    {
        get { return this.c[index]; }
    }
}";
            var assembly = GetAssembly(source);
            var type = assembly.GetType("C");
            var value = CreateDkmClrValue(
                value: type.Instantiate(),
                type: type,
                evalFlags: DkmEvaluationResultFlags.None);
            var rootExpr = "new C()";
            var evalResult = FormatResult(rootExpr, value);
            Verify(evalResult,
                EvalResult(rootExpr, "{C}", "C", rootExpr, DkmEvaluationResultFlags.Expandable));
            var children = GetChildren(evalResult);
            Verify(children,
                EvalResult("F", "3", "object {int}", "new P(new C()).F", DkmEvaluationResultFlags.ReadOnly),
                EvalResult("Raw View", null, "", "new C(), raw", DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Data));
        }

        [Fact]
        public void Pointers()
        {
            var source =
@"using System.Diagnostics;
[DebuggerTypeProxy(typeof(P))]
unsafe class C
{
    internal C(long p)
    {
        this.P = (int*)p;
    }
    internal int* P;
}
unsafe class P
{
    private readonly C c;
    public P(C c)
    {
        this.c = c;
    }
    public int* Q
    {
        get { return this.c.P; }
    }
}";
            var assembly = GetUnsafeAssembly(source);
            unsafe
            {
                int i = 4;
                long p = (long)&i;
                var type = assembly.GetType("C");
                var value = CreateDkmClrValue(
                    value: ReflectionUtilities.Instantiate(type, p),
                    type: type,
                    evalFlags: DkmEvaluationResultFlags.None);
                var rootExpr = "new C()";
                var evalResult = FormatResult(rootExpr, value);
                Verify(evalResult,
                    EvalResult(rootExpr, "{C}", "C", rootExpr, DkmEvaluationResultFlags.Expandable));
                var children = GetChildren(evalResult);
                Verify(children,
                    EvalResult("Q", PointerToString(new IntPtr(p)), "int*", "new P(new C()).Q", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly),
                    EvalResult("Raw View", null, "", "new C(), raw", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Data));
                var more = GetChildren(children[0]);
                Verify(more,
                    EvalResult("*new P(new C()).Q", "4", "int", "*new P(new C()).Q"));
                more = GetChildren(children[1]);
                Verify(more,
                    EvalResult("P", PointerToString(new IntPtr(p)), "int*", "(new C()).P", DkmEvaluationResultFlags.Expandable));
                more = GetChildren(more[0]);
                Verify(more,
                    EvalResult("*(new C()).P", "4", "int", "*(new C()).P"));
            }
        }

        [Fact]
        public void StaticMembers()
        {
            var source =
@"using System.Diagnostics;
[DebuggerTypeProxy(typeof(PA))]
class A
{
    internal object F = 1;
    internal static object P { get { return 2; } }
}
class PA
{
    private readonly A a;
    public PA(A a)
    {
        this.a = a;
    }
    public object P { get { return this.a.F; } }
    public static object Q { get { return A.P; } }
}
[DebuggerTypeProxy(typeof(PB))]
class B
{
    internal static object F = 3;
}
class PB
{
    public PB(B b)
    {
    }
    public static object G = B.F;
}";
            var assembly = GetAssembly(source);
            var typeA = assembly.GetType("A");
            var typeB = assembly.GetType("B");

            // A
            var value = CreateDkmClrValue(
                value: typeA.Instantiate(),
                type: typeA,
                evalFlags: DkmEvaluationResultFlags.None);
            var rootExpr = "new C()";
            var evalResult = FormatResult(rootExpr, value);
            Verify(evalResult,
                EvalResult(rootExpr, "{A}", "A", rootExpr, DkmEvaluationResultFlags.Expandable));
            var children = GetChildren(evalResult);
            Verify(children,
                EvalResult("P", "1", "object {int}", "new PA(new C()).P", DkmEvaluationResultFlags.ReadOnly),
                EvalResult("Static members", null, "", "PA", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Class),
                EvalResult("Raw View", null, "", "new C(), raw", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Data));
            Verify(GetChildren(children[1]),
                EvalResult("Q", "2", "object {int}", "PA.Q", DkmEvaluationResultFlags.ReadOnly));
            children = GetChildren(children[2]);
            Verify(children,
                EvalResult("F", "1", "object {int}", "(new C()).F"),
                EvalResult("Static members", null, "", "A", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Class));
            children = GetChildren(children[1]);
            Verify(children,
                EvalResult("P", "2", "object {int}", "A.P", DkmEvaluationResultFlags.ReadOnly));

            // B
            value = CreateDkmClrValue(
                value: typeB.Instantiate(),
                type: typeB,
                evalFlags: DkmEvaluationResultFlags.None);
            rootExpr = "new B()";
            evalResult = FormatResult(rootExpr, value);
            Verify(evalResult,
                EvalResult(rootExpr, "{B}", "B", rootExpr, DkmEvaluationResultFlags.Expandable));
            children = GetChildren(evalResult);
            Verify(children,
                EvalResult("Static members", null, "", "PB", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Class),
                EvalResult("Raw View", null, "", "new B(), raw", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Data));
            Verify(GetChildren(children[0]),
                EvalResult("G", "3", "object {int}", "PB.G"));
            children = GetChildren(children[1]);
            Verify(children,
                EvalResult("Static members", null, "", "B", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Class));
            children = GetChildren(children[0]);
            Verify(children,
                EvalResult("F", "3", "object {int}", "B.F"));
        }

        [Fact]
        public void NullInstanceStaticMembers()
        {
            var source =
@"using System.Diagnostics;
[DebuggerTypeProxy(typeof(P))]
class C
{
    internal static object P { get { return 2; } }
}
class P
{
    public P(C c)
    {
    }
    public static object Q { get { return C.P; } }
}";
            var assembly = GetAssembly(source);
            var type = assembly.GetType("C");
            var value = CreateDkmClrValue(
                value: null,
                type: type,
                evalFlags: DkmEvaluationResultFlags.None);
            var rootExpr = "new C()";
            var evalResult = FormatResult(rootExpr, value);
            Verify(evalResult,
                EvalResult(rootExpr, "null", "C", rootExpr, DkmEvaluationResultFlags.Expandable));
            var children = GetChildren(evalResult);
            Verify(children,
                EvalResult("Static members", null, "", "C", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Class));
            // Note: The native EE uses the proxy type, even for
            // null instances, so statics on the proxy type are
            // displayed. That case is not supported currently.
            Verify(GetChildren(children[0]),
                EvalResult("P", "2", "object {int}", "C.P", DkmEvaluationResultFlags.ReadOnly));
        }

        /// <summary>
        /// Proxy type members should be in alphabetical order.
        /// </summary>
        [Fact]
        public void OrderedMembers()
        {
            var source =
@"using System.Diagnostics;
[DebuggerTypeProxy(typeof(P))]
class C
{
}
class P
{
    public P(C c)
    {
    }
    public object D = 0;
    public object A = 1;
    public object C { get { return 2; } }
    public object B { get { return 3; } }
}";
            var assembly = GetAssembly(source);
            var type = assembly.GetType("C");
            var value = CreateDkmClrValue(
                value: type.Instantiate(),
                type: type,
                evalFlags: DkmEvaluationResultFlags.None);
            var rootExpr = "new C()";
            var evalResult = FormatResult(rootExpr, value);
            Verify(evalResult,
                EvalResult(rootExpr, "{C}", "C", rootExpr, DkmEvaluationResultFlags.Expandable));
            var children = GetChildren(evalResult);
            Verify(children,
                EvalResult("A", "1", "object {int}", "new P(new C()).A"),
                EvalResult("B", "3", "object {int}", "new P(new C()).B", DkmEvaluationResultFlags.ReadOnly),
                EvalResult("C", "2", "object {int}", "new P(new C()).C", DkmEvaluationResultFlags.ReadOnly),
                EvalResult("D", "0", "object {int}", "new P(new C()).D"),
                EvalResult("Raw View", null, "", "new C(), raw", DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Data));
        }

        [Fact]
        public void InstantiateProxyTypeException()
        {
            var source =
@"using System.Diagnostics;
[DebuggerTypeProxy(typeof(P))]
class C
{
    internal C(int f)
    {
        this.F = f;
    }
    internal int F;
}
class P
{
    public P(C c)
    {
        throw new System.ArgumentException();
    }
}";
            var assembly = GetAssembly(source);
            var type = assembly.GetType("C");
            var typeP = assembly.GetType("P");
            var value = CreateDkmClrValue(
                value: type.Instantiate(3),
                type: type,
                evalFlags: DkmEvaluationResultFlags.None);
            var rootExpr = "new C(3)";
            var actualProxyType = ((DkmClrDebuggerTypeProxyAttribute)value.Type.GetEvalAttributes().First()).ProxyType;
            Assert.Equal(((TypeImpl)actualProxyType.GetLmrType()).Type, typeP);
            var evalResult = FormatResult(rootExpr, value);
            Verify(evalResult,
                EvalResult(rootExpr, "{C}", "C", rootExpr, DkmEvaluationResultFlags.Expandable));
            // Exception from InstantiateProxyType should
            // have been caught and proxy type dropped.
            var children = GetChildren(evalResult);
            Verify(children,
                EvalResult("F", "3", "int", "(new C(3)).F"));
        }
    }
}
