// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Debugger;
using Microsoft.VisualStudio.Debugger.Clr;
using Microsoft.VisualStudio.Debugger.ComponentInterfaces;
using Microsoft.VisualStudio.Debugger.Evaluation;
using Microsoft.VisualStudio.Debugger.Symbols;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class ExpansionTests : CSharpResultProviderTestBase
    {
        [Fact]
        public void Primitives()
        {
            // System.Object
            Verify(FormatResult("null", CreateDkmClrValue(null, typeof(object), evalFlags: DkmEvaluationResultFlags.None)), EvalResult("null", "null", "object", "null"));
            Verify(FormatResult("new object()", CreateDkmClrValue(new object())), EvalResult("new object()", "{object}", "object", "new object()"));
            // System.DBNull
            Verify(FormatResult("DBNull.Value", CreateDkmClrValue(DBNull.Value)), EvalResult("DBNull.Value", "{}", "System.DBNull", "DBNull.Value", DkmEvaluationResultFlags.Expandable));
            // System.Boolean
            Verify(FormatResult("new Boolean()", CreateDkmClrValue(new Boolean())), EvalResult("new Boolean()", "false", "bool", "new Boolean()", DkmEvaluationResultFlags.Boolean));
            Verify(FormatResult("false", CreateDkmClrValue(false, typeof(bool), evalFlags: DkmEvaluationResultFlags.Boolean)), EvalResult("false", "false", "bool", "false", DkmEvaluationResultFlags.Boolean));
            Verify(FormatResult("true", CreateDkmClrValue(true, typeof(bool), evalFlags: DkmEvaluationResultFlags.Boolean | DkmEvaluationResultFlags.BooleanTrue)), EvalResult("true", "true", "bool", "true", DkmEvaluationResultFlags.Boolean | DkmEvaluationResultFlags.BooleanTrue));
            // System.Char
            Verify(FormatResult("new Char()", CreateDkmClrValue(new Char())), EvalResult("new Char()", "0 '\\0'", "char", "new Char()", editableValue: "'\\0'"));
            // System.SByte
            Verify(FormatResult("new SByte()", CreateDkmClrValue(new SByte())), EvalResult("new SByte()", "0", "sbyte", "new SByte()"));
            // System.Byte
            Verify(FormatResult("new Byte()", CreateDkmClrValue(new Byte())), EvalResult("new Byte()", "0", "byte", "new Byte()"));
            // System.Int16
            Verify(FormatResult("new Int16()", CreateDkmClrValue(new Int16())), EvalResult("new Int16()", "0", "short", "new Int16()"));
            // System.UInt16
            Verify(FormatResult("new UInt16()", CreateDkmClrValue(new UInt16())), EvalResult("new UInt16()", "0", "ushort", "new UInt16()"));
            // System.Int32
            Verify(FormatResult("new Int32()", CreateDkmClrValue(new Int32())), EvalResult("new Int32()", "0", "int", "new Int32()"));
            // System.UInt32
            Verify(FormatResult("new UInt32()", CreateDkmClrValue(new UInt32())), EvalResult("new UInt32()", "0", "uint", "new UInt32()"));
            // System.Int64
            Verify(FormatResult("new Int64()", CreateDkmClrValue(new Int64())), EvalResult("new Int64()", "0", "long", "new Int64()"));
            // System.UInt64
            Verify(FormatResult("new UInt64()", CreateDkmClrValue(new UInt64())), EvalResult("new UInt64()", "0", "ulong", "new UInt64()"));
            // System.Single
            Verify(FormatResult("new Single()", CreateDkmClrValue(new Single())), EvalResult("new Single()", "0", "float", "new Single()"));
            // System.Double
            Verify(FormatResult("new Double()", CreateDkmClrValue(new Double())), EvalResult("new Double()", "0", "double", "new Double()"));
            // System.Decimal
            Verify(FormatResult("new Decimal()", CreateDkmClrValue(new Decimal())), EvalResult("new Decimal()", "0", "decimal", "new Decimal()", editableValue: "0M"));
            // System.DateTime
            // Set currentCulture to en-US for the test to pass in all locales
            using (new CultureContext("en-US"))
            {
                Verify(FormatResult("new DateTime()", CreateDkmClrValue(new DateTime())), EvalResult("new DateTime()", "{1/1/0001 12:00:00 AM}", "System.DateTime", "new DateTime()", DkmEvaluationResultFlags.Expandable));
            }

            // System.String
            Verify(FormatResult("stringNull", CreateDkmClrValue(null, typeof(string), evalFlags: DkmEvaluationResultFlags.None)), EvalResult("stringNull", "null", "string", "stringNull"));
            Verify(FormatResult("\"\"", CreateDkmClrValue("")), EvalResult("\"\"", "\"\"", "string", "\"\"", DkmEvaluationResultFlags.RawString, editableValue: "\"\""));
        }

        /// <summary>
        /// Get children in blocks.
        /// </summary>
        [Fact]
        public void GetChildrenTest()
        {
            var source =
@"class C
{
    internal object F1;
    protected object F2;
    private object F3;
    internal object P1 { get { return null; } }
    protected object P2 { get { return null; } }
    private object P3 { get { return null; } }
}";
            var assembly = GetAssembly(source);
            var type = assembly.GetType("C");
            var rootExpr = "new C()";
            var value = CreateDkmClrValue(Activator.CreateInstance(type));
            var evalResult = (DkmSuccessEvaluationResult)FormatResult(rootExpr, value);
            Verify(evalResult,
                EvalResult(rootExpr, "{C}", "C", rootExpr, DkmEvaluationResultFlags.Expandable));

            DkmEvaluationResultEnumContext enumContext;
            var children = GetChildren(evalResult, 1, null, out enumContext);
            Assert.Equal(1, children.Length);

            var resultsBuilder = ArrayBuilder<DkmEvaluationResult>.GetInstance();
            resultsBuilder.AddRange(children);

            while (resultsBuilder.Count < enumContext.Count)
            {
                var items = GetItems(enumContext, resultsBuilder.Count, 2);
                Assert.InRange(items.Length, 0, 2);
                resultsBuilder.AddRange(items);
            }

            Verify(resultsBuilder.ToArrayAndFree(),
                EvalResult("F1", "null", "object", "(new C()).F1"),
                EvalResult("F2", "null", "object", "(new C()).F2"),
                EvalResult("F3", "null", "object", "(new C()).F3"),
                EvalResult("P1", "null", "object", "(new C()).P1", DkmEvaluationResultFlags.ReadOnly),
                EvalResult("P2", "null", "object", "(new C()).P2", DkmEvaluationResultFlags.ReadOnly),
                EvalResult("P3", "null", "object", "(new C()).P3", DkmEvaluationResultFlags.ReadOnly));
        }

        /// <summary>
        /// Get children out or order.
        /// </summary>
        [Fact]
        public void GetChildrenOutOfOrder()
        {
            var source =
@"class C
{
    internal object F1;
    protected object F2;
    private object F3;
    internal object P1 { get { return null; } }
    protected object P2 { get { return null; } }
    private object P3 { get { return null; } }
}";

            var assembly = GetAssembly(source);
            var type = assembly.GetType("C");
            var rootExpr = "new C()";
            var value = CreateDkmClrValue(Activator.CreateInstance(type));
            var evalResult = FormatResult(rootExpr, value);

            var builder = ArrayBuilder<DkmEvaluationResult>.GetInstance();

            DkmEvaluationResultEnumContext enumContext;
            var children = GetChildren(evalResult, 0, null, out enumContext);
            builder.AddRange(children);

            builder.AddRange(GetItems(enumContext, 3, 2));
            builder.AddRange(GetItems(enumContext, 1, 1));
            builder.AddRange(GetItems(enumContext, 1, 1));
            builder.AddRange(GetItems(enumContext, 2, 0));

            Verify(builder.ToArrayAndFree(),
                EvalResult("P1", "null", "object", "(new C()).P1", DkmEvaluationResultFlags.ReadOnly),
                EvalResult("P2", "null", "object", "(new C()).P2", DkmEvaluationResultFlags.ReadOnly),
                EvalResult("F2", "null", "object", "(new C()).F2"),
                EvalResult("F2", "null", "object", "(new C()).F2"));
        }

        /// <summary>
        /// GetChildren should return the smaller number
        /// of items if request is outside range.
        /// </summary>
        [Fact]
        public void GetChildrenRequestOutsideRange()
        {
            var source =
@"class C
{
    internal object F1;
    internal object F2;
    internal object F3;
    internal object F4;
}";
            var assembly = GetAssembly(source);
            var type = assembly.GetType("C");
            var value = CreateDkmClrValue(Activator.CreateInstance(type));
            var evalResult = FormatResult("o", value);
            DkmEvaluationResultEnumContext enumContext;
            var children = GetChildren(evalResult, 100, null, out enumContext);
            Assert.Equal(enumContext.Count, 4);
            Verify(children,
                EvalResult("F1", "null", "object", "o.F1"),
                EvalResult("F2", "null", "object", "o.F2"),
                EvalResult("F3", "null", "object", "o.F3"),
                EvalResult("F4", "null", "object", "o.F4"));
        }

        /// <summary>
        /// GetItems should return the smaller number
        /// of items if request is outside range.
        /// </summary>
        [Fact]
        public void GetItemsRequestOutsideRange()
        {
            var source =
@"class C
{
    internal object F1;
    internal object F2;
    internal object F3;
    internal object F4;
}";
            var assembly = GetAssembly(source);
            var type = assembly.GetType("C");
            var value = CreateDkmClrValue(Activator.CreateInstance(type));
            var evalResult = FormatResult("o", value);
            DkmEvaluationResultEnumContext enumContext;
            var children = GetChildren(evalResult, 0, null, out enumContext);
            Assert.Equal(enumContext.Count, 4);
            Verify(children);
            children = GetItems(enumContext, 2, 4);
            Verify(children,
                EvalResult("F3", "null", "object", "o.F3"),
                EvalResult("F4", "null", "object", "o.F4"));
            children = GetItems(enumContext, 4, 1);
            Verify(children);
            children = GetItems(enumContext, 6, 2);
            Verify(children);
        }

        /// <summary>
        /// Null instance should not be expandable.
        /// </summary>
        [Fact]
        public void NullInstance()
        {
            var source =
@"class C
{
    object o;
    string s;
    C c;
}";
            var assembly = GetAssembly(source);
            var type = assembly.GetType("C");
            var rootExpr = "new C()";
            var value = CreateDkmClrValue(Activator.CreateInstance(type));
            var evalResult = FormatResult(rootExpr, value);
            Verify(evalResult,
                EvalResult(rootExpr, "{C}", "C", rootExpr, DkmEvaluationResultFlags.Expandable));
            var children = GetChildren(evalResult);
            Verify(children,
                EvalResult("c", "null", "C", "(new C()).c"),
                EvalResult("o", "null", "object", "(new C()).o"),
                EvalResult("s", "null", "string", "(new C()).s"));
        }

        [Fact]
        public void BaseAndDerived()
        {
            var source =
@"abstract class A
{
    internal object F;
    internal abstract object P { get; }
}
class B : A
{
    internal override object P { get { return null; } }
    internal virtual object Q { get { return null; } }
}
class C : B
{
}
class P
{
    object o = new C();
    A a = new C();
    B b = new C();
    C c = new C();
}";
            var assembly = GetAssembly(source);
            var type = assembly.GetType("P");
            var rootExpr = "new P()";
            var value = CreateDkmClrValue(Activator.CreateInstance(type));
            var evalResult = FormatResult(rootExpr, value);
            Verify(evalResult,
                EvalResult(rootExpr, "{P}", "P", rootExpr, DkmEvaluationResultFlags.Expandable));
            var children = GetChildren(evalResult);
            Verify(children,
                EvalResult("a", "{C}", "A {C}", "(new P()).a", DkmEvaluationResultFlags.Expandable),
                EvalResult("b", "{C}", "B {C}", "(new P()).b", DkmEvaluationResultFlags.Expandable),
                EvalResult("c", "{C}", "C", "(new P()).c", DkmEvaluationResultFlags.Expandable),
                EvalResult("o", "{C}", "object {C}", "(new P()).o", DkmEvaluationResultFlags.Expandable));

            // B b = new C();
            Verify(GetChildren(children[1]),
                EvalResult("F", "null", "object", "(new P()).b.F"),
                EvalResult("P", "null", "object", "(new P()).b.P", DkmEvaluationResultFlags.ReadOnly),
                EvalResult("Q", "null", "object", "(new P()).b.Q", DkmEvaluationResultFlags.ReadOnly));
        }

        [Fact]
        public void Interface()
        {
            var source =
@"interface IA
{
}
interface IB : IA
{
}
class A : IB
{
    internal object F = 4;
}
class B : A
{
}
class C
{
    IA a = new B();
}";
            var assembly = GetAssembly(source);
            var type = assembly.GetType("C");
            var rootExpr = "new C()";
            var value = CreateDkmClrValue(Activator.CreateInstance(type));
            var evalResult = FormatResult(rootExpr, value);
            Verify(evalResult,
                EvalResult(rootExpr, "{C}", "C", rootExpr, DkmEvaluationResultFlags.Expandable));
            var children = GetChildren(evalResult);
            Verify(children,
                EvalResult("a", "{B}", "IA {B}", "(new C()).a", DkmEvaluationResultFlags.Expandable));
            children = GetChildren(children[0]);
            Verify(children,
                EvalResult("F", "4", "object {int}", "((A)(new C()).a).F"));
        }

        [Fact]
        public void ExplicitInterfaceImplementation()
        {
            var source = @"
interface I<T>
{
    int P1 { get; }
    int P2 { get; }
}

class C : I<I<string>>
{
    public int P1 { get { return 1; } }
    int I<I<string>>.P2 { get { return 2; } }
}";
            var assembly = GetAssembly(source);
            var type = assembly.GetType("C");
            var rootExpr = "new C()";
            var value = CreateDkmClrValue(Activator.CreateInstance(type));
            var evalResult = (DkmSuccessEvaluationResult)FormatResult(rootExpr, value);
            Verify(evalResult,
                EvalResult(rootExpr, "{C}", "C", rootExpr, DkmEvaluationResultFlags.Expandable));

            DkmEvaluationResultEnumContext enumContext;
            var children = GetChildren(evalResult, 2, null, out enumContext);
            Verify(children,
                EvalResult("I<I<string>>.P2", "2", "int", "((I<I<string>>)(new C())).P2", DkmEvaluationResultFlags.ReadOnly),
                EvalResult("P1", "1", "int", "(new C()).P1", DkmEvaluationResultFlags.ReadOnly));
        }

        [Fact]
        public void ExplicitInterfaceImplementation2()
        {
            var source = @"
interface I<T>
{
    int P1 { get; }
    int P2 { get; }
}

class C : I<bool>, I<char>
{
    public int P1 { get { return 1; } }
    int I<bool>.P2 { get { return 2; } }
    int I<char>.P2 { get { return 3; } }
}";
            var assembly = GetAssembly(source);
            var type = assembly.GetType("C");
            var rootExpr = "new C()";
            var value = CreateDkmClrValue(Activator.CreateInstance(type));
            var evalResult = (DkmSuccessEvaluationResult)FormatResult(rootExpr, value);
            Verify(evalResult,
                EvalResult(rootExpr, "{C}", "C", rootExpr, DkmEvaluationResultFlags.Expandable));

            DkmEvaluationResultEnumContext enumContext;
            var children = GetChildren(evalResult, 3, null, out enumContext);
            Verify(children,
                EvalResult("I<bool>.P2", "2", "int", "((I<bool>)(new C())).P2", DkmEvaluationResultFlags.ReadOnly),
                EvalResult("I<char>.P2", "3", "int", "((I<char>)(new C())).P2", DkmEvaluationResultFlags.ReadOnly),
                EvalResult("P1", "1", "int", "(new C()).P1", DkmEvaluationResultFlags.ReadOnly));
        }

        [Fact]
        public void ExplicitInterfaceImplementationVb()
        {
            var source = @"
.class interface private abstract auto ansi I
{
  .method public newslot specialname abstract strict virtual 
          instance int32  get_P() cil managed
  {
  }

  .property instance int32 P()
  {
    .get instance int32 I::get_P()
  }
} // end of class I

.class private auto ansi C
       extends [mscorlib]System.Object
       implements I
{
  .method public specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }

  .method public newslot specialname strict virtual final 
          instance int32  get_Q() cil managed
  {
    .override I::get_P
    ldc.i4.1
    ret
  }

  .property instance int32 Q()
  {
    .get instance int32 C::get_Q()
  }
} // end of class C
";
            ImmutableArray<byte> assemblyBytes;
            ImmutableArray<byte> pdbBytes;
            CSharpTestBase.EmitILToArray(source, appendDefaultHeader: true, includePdb: false, assemblyBytes: out assemblyBytes, pdbBytes: out pdbBytes);
            var assembly = ReflectionUtilities.Load(assemblyBytes);
            var type = assembly.GetType("C");
            var rootExpr = "new C()";
            var value = CreateDkmClrValue(Activator.CreateInstance(type));
            var evalResult = (DkmSuccessEvaluationResult)FormatResult(rootExpr, value);
            Verify(evalResult,
                EvalResult(rootExpr, "{C}", "C", rootExpr, DkmEvaluationResultFlags.Expandable));

            DkmEvaluationResultEnumContext enumContext;
            var children = GetChildren(evalResult, 3, null, out enumContext);
            Verify(children,
                EvalResult("Q", "1", "int", "(new C()).Q", DkmEvaluationResultFlags.ReadOnly));
        }

        [Fact]
        public void EmptyBaseAndDerived()
        {
            var source =
@"class A
{
}
class B : A
{
}
class C
{
    object o = new B();
    A a = new B();
    B b = new B();
}";
            var assembly = GetAssembly(source);
            var type = assembly.GetType("C");
            var rootExpr = "new C()";
            var value = CreateDkmClrValue(Activator.CreateInstance(type));
            var evalResult = FormatResult(rootExpr, value);
            Verify(evalResult,
                EvalResult(rootExpr, "{C}", "C", rootExpr, DkmEvaluationResultFlags.Expandable));
            var children = GetChildren(evalResult);
            Verify(children,
                EvalResult("a", "{B}", "A {B}", "(new C()).a", DkmEvaluationResultFlags.None),
                EvalResult("b", "{B}", "B", "(new C()).b", DkmEvaluationResultFlags.None),
                EvalResult("o", "{B}", "object {B}", "(new C()).o", DkmEvaluationResultFlags.None));

            // A a = new B();
            var more = GetChildren(children[0]);
            Verify(more);

            // B b = new B();
            more = GetChildren(children[1]);
            Verify(more);

            // object o = new B();
            more = GetChildren(children[2]);
            Verify(more);
        }

        [Fact]
        public void ValueTypeBaseAndDerived()
        {
            var source =
@"struct S
{
    object F;
}
class C
{
    object o = new S();
    System.ValueType v = new S();
    S s = new S();
}";
            var assembly = GetAssembly(source);
            var type = assembly.GetType("C");
            var rootExpr = "new C()";
            var value = CreateDkmClrValue(Activator.CreateInstance(type));
            var evalResult = FormatResult(rootExpr, value);
            Verify(evalResult,
                EvalResult(rootExpr, "{C}", "C", rootExpr, DkmEvaluationResultFlags.Expandable));
            var children = GetChildren(evalResult);
            Verify(children,
                EvalResult("o", "{S}", "object {S}", "(new C()).o", DkmEvaluationResultFlags.Expandable),
                EvalResult("s", "{S}", "S", "(new C()).s", DkmEvaluationResultFlags.Expandable),
                EvalResult("v", "{S}", "System.ValueType {S}", "(new C()).v", DkmEvaluationResultFlags.Expandable));

            // object o = new S();
            var more = GetChildren(children[0]);
            Verify(more,
                EvalResult("F", "null", "object", "((S)(new C()).o).F"));

            // S s = new S();
            more = GetChildren(children[1]);
            Verify(more,
                EvalResult("F", "null", "object", "(new C()).s.F"));

            // System.ValueType v = new S();
            more = GetChildren(children[2]);
            Verify(more,
                EvalResult("F", "null", "object", "((S)(new C()).v).F"));
        }

        [Fact]
        public void WriteOnlyProperty()
        {
            var source =
@"class C
{
    object P { set { } }
    static object Q { set { } }
}";
            var assembly = GetAssembly(source);
            var type = assembly.GetType("C");
            var rootExpr = "new C()";
            var value = CreateDkmClrValue(Activator.CreateInstance(type));
            var evalResult = FormatResult(rootExpr, value);
            Verify(evalResult,
                EvalResult(rootExpr, "{C}", "C", rootExpr));
            var children = GetChildren(evalResult);
            Verify(children);
        }

        [Fact]
        public void Enums()
        {
            var source =
@"using System;
enum E { A, B }
enum F : byte { }
[Flags] enum @if { @else = 1, fi }
class C
{
    E e = E.B;
    F f = default(F);
    @if g = @if.@else | @if.fi;
    @if h = (@if)5;
}";
            var assembly = GetAssembly(source);
            var type = assembly.GetType("C");
            var rootExpr = "new C()";
            var value = CreateDkmClrValue(Activator.CreateInstance(type));
            var evalResult = FormatResult(rootExpr, value);
            Verify(evalResult,
                EvalResult(rootExpr, "{C}", "C", rootExpr, DkmEvaluationResultFlags.Expandable));
            var children = GetChildren(evalResult);
            Verify(children,
                EvalResult("e", "B", "E", "(new C()).e", editableValue: "E.B"),
                EvalResult("f", "0", "F", "(new C()).f", editableValue: "0"),
                EvalResult("g", "else | fi", "if", "(new C()).g", editableValue: "@if.@else | @if.fi"),
                EvalResult("h", "5", "if", "(new C()).h", editableValue: "5"));
        }

        [Fact]
        public void Nullable()
        {
            var source =
@"enum E
{
    A
}
struct S
{
    internal S(int f) { F = f; }
    object F;
}
class C
{
    E? e1 = E.A;
    E? e2 = null;
    S? s1 = new S(1);
    S? s2 = null;
    object o1 = new System.Nullable<S>(default(S));
    object o2 = new System.Nullable<S>();
}";
            var assembly = GetAssembly(source);
            var type = assembly.GetType("C");
            var value = CreateDkmClrValue(Activator.CreateInstance(type));
            var rootExpr = "new C()";
            var evalResult = FormatResult(rootExpr, value);
            Verify(evalResult,
                EvalResult(rootExpr, "{C}", "C", rootExpr, DkmEvaluationResultFlags.Expandable));
            var children = GetChildren(evalResult);
            Verify(children,
                EvalResult("e1", "A", "E?", "(new C()).e1", editableValue: "E.A"),
                EvalResult("e2", "null", "E?", "(new C()).e2"),
                EvalResult("o1", "{S}", "object {S}", "(new C()).o1", DkmEvaluationResultFlags.Expandable),
                EvalResult("o2", "null", "object", "(new C()).o2"),
                EvalResult("s1", "{S}", "S?", "(new C()).s1", DkmEvaluationResultFlags.Expandable),
                EvalResult("s2", "null", "S?", "(new C()).s2"));
            // object o1 = new System.Nullable<S>(default(S));
            Verify(GetChildren(children[2]),
                EvalResult("F", "null", "object", "((S)(new C()).o1).F"));
            // S? s1 = new S();
            Verify(GetChildren(children[4]),
                EvalResult("F", "1", "object {int}", "(new C()).s1.F"));
        }

        [Fact]
        public void Pointers()
        {
            var source =
@"unsafe class C
{
    internal C(long p)
    {
        this.p = (int*)p;
    }
    int* p;
    int* q;
}";
            var assembly = GetUnsafeAssembly(source);
            unsafe
            {
                int i = 4;
                long p = (long)&i;
                var type = assembly.GetType("C");
                var rootExpr = string.Format("new C({0})", p);
                var value = CreateDkmClrValue(type.Instantiate(p));
                var evalResult = FormatResult(rootExpr, value);
                Verify(evalResult,
                    EvalResult(rootExpr, "{C}", "C", rootExpr, DkmEvaluationResultFlags.Expandable));
                var children = GetChildren(evalResult);
                Verify(children,
                    EvalResult("p", PointerToString(new IntPtr(p)), "int*", string.Format("({0}).p", rootExpr), DkmEvaluationResultFlags.Expandable),
                    EvalResult("q", PointerToString(IntPtr.Zero), "int*", string.Format("({0}).q", rootExpr)));
                string fullName = string.Format("*({0}).p", rootExpr);
                Verify(GetChildren(children[0]),
                    EvalResult(fullName, "4", "int", fullName));
            }
        }

        /// <summary>
        /// This tests the managed address-of functionality.  When you take the address
        /// of a managed object, what you get back is an IntPtr*.  As in dev12, this
        /// exposes two pointers, the one to the IntPtr and the one to the actual data
        /// (in the IntPtr).  For example, if you have a string "str", then "&str" yields
        /// an IntPtr*.  The pointer is to the "string&" (typed as IntPtr, since Roslyn
        /// doesn't have a representation for reference types) and the IntPtr is a pointer
        /// to the actual string object on the heap.
        /// </summary>
        [WorkItem(1022632)]
        [Fact]
        public void IntPtrPointer()
        {
            var source = @"
using System;

unsafe class C
{
    internal C(long p)
    {
        this.p = (IntPtr*)p;
    }
    IntPtr* p;
    IntPtr* q;
}";
            var assembly = GetUnsafeAssembly(source);
            unsafe
            {
                // NOTE: We're depending on endian-ness to put
                // the interesting bytes first when we run this
                // test as 32-bit.
                long i = 4;
                long p = (long)&i;
                var type = assembly.GetType("C");
                var rootExpr = string.Format("new C({0})", p);
                var value = CreateDkmClrValue(type.Instantiate(p));
                var evalResult = FormatResult(rootExpr, value);
                Verify(evalResult,
                    EvalResult(rootExpr, "{C}", "C", rootExpr, DkmEvaluationResultFlags.Expandable));
                var children = GetChildren(evalResult);
                Verify(children,
                    EvalResult("p", PointerToString(new IntPtr(p)), "System.IntPtr*", string.Format("({0}).p", rootExpr), DkmEvaluationResultFlags.Expandable),
                    EvalResult("q", PointerToString(IntPtr.Zero), "System.IntPtr*", string.Format("({0}).q", rootExpr)));
                string fullName = string.Format("*({0}).p", rootExpr);
                children = GetChildren(children[0]);
                Verify(children,
                    EvalResult(fullName, "{4}", "System.IntPtr", fullName, DkmEvaluationResultFlags.Expandable));
                children = GetChildren(children[0]);
                Verify(children,
                    EvalResult("m_value", PointerToString(new IntPtr(i)), "void*", string.Format("({0}).m_value", fullName)),
                    EvalResult("Static members", null, "", "System.IntPtr", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Class));
            }
        }

        [WorkItem(1154608)]
        [Fact]
        public void VoidPointer()
        {
            var source = @"
using System;

unsafe class C
{
    internal C(long p)
    {
        this.v = (void*)p;
        this.vv = (void**)p;
    }
    void* v;
    void** vv;
}";
            var assembly = GetUnsafeAssembly(source);
            unsafe
            {
                // NOTE: We're depending on endian-ness to put
                // the interesting bytes first when we run this
                // test as 32-bit.
                long i = 4;
                long p = (long)&i;
                long pp = (long)&p;
                var type = assembly.GetType("C");
                var rootExpr = $"new C({pp})";
                var value = CreateDkmClrValue(type.Instantiate(pp));
                var evalResult = FormatResult(rootExpr, value);
                Verify(evalResult,
                    EvalResult(rootExpr, "{C}", "C", rootExpr, DkmEvaluationResultFlags.Expandable));
                var children = GetChildren(evalResult);
                Verify(children,
                    EvalResult("v", PointerToString(new IntPtr(pp)), "void*", $"({rootExpr}).v"),
                    EvalResult("vv", PointerToString(new IntPtr(pp)), "void**", $"({rootExpr}).vv", DkmEvaluationResultFlags.Expandable));
                string fullName = $"*({rootExpr}).vv";
                children = GetChildren(children[1]);
                Verify(children,
                    EvalResult(fullName, PointerToString(new IntPtr(p)), "void*", fullName));
            }
        }

        [WorkItem(1064176)]
        [Fact]
        public void NullPointer()
        {
            /*
            unsafe class C
            {
                void M()
                {
                    byte *ptr = null;
                }
            }
            */
            var rootExpr = "ptr";
            var type = typeof(byte*);
            var value = CreateDkmClrValue(0, type);
            var evalResult = FormatResult(rootExpr, value);
            Verify(evalResult,
                EvalResult(rootExpr, "0x00000000", "byte*", rootExpr)); // should not be expandable
            Assert.Empty(GetChildren(evalResult));
            value = CreateDkmClrValue(0L, type);
            evalResult = FormatResult(rootExpr, value);
            Verify(evalResult,
                EvalResult(rootExpr, "0x0000000000000000", "byte*", rootExpr)); // should not be expandable
            Assert.Empty(GetChildren(evalResult));
        }

        [Fact]
        public void InvalidPointer()
        {
            /*
            unsafe class C
            {
                void M()
                {
                    byte *ptr = <invalid address>;
                }
            }
            */
            var rootExpr = "ptr";
            var type = typeof(byte*);
            var value = CreateDkmClrValue(0x1337, type);
            var evalResult = FormatResult(rootExpr, value);
            Verify(evalResult,
                EvalResult(rootExpr, "0x00001337", "byte*", rootExpr, DkmEvaluationResultFlags.Expandable));
            Verify(GetChildren(evalResult),
                EvalResult("*ptr", "Cannot dereference '*ptr'. The pointer is not valid.", "byte", "*ptr", DkmEvaluationResultFlags.ExceptionThrown));
        }

        [Fact]
        public void StaticMembers()
        {
            var source =
@"class A
{
    const int F = 1;
    static readonly int G = 2;
}
class B : A
{
}
struct S
{
    const object F = null;
    static object P { get { return 3; } }
}
enum E
{
    A,
    B
}
class C
{
    A a = default(A);
    B b = null;
    S s = new S();
    S? sn = null;
    E e = E.B;
}";
            var assembly = GetAssembly(source);
            var type = assembly.GetType("C");
            var rootExpr = "new C()";
            var value = CreateDkmClrValue(Activator.CreateInstance(type));
            var evalResult = FormatResult(rootExpr, value);
            Verify(evalResult,
                EvalResult(rootExpr, "{C}", "C", rootExpr, DkmEvaluationResultFlags.Expandable));
            var children = GetChildren(evalResult);
            Verify(children,
                EvalResult("a", "null", "A", "(new C()).a", DkmEvaluationResultFlags.Expandable),
                EvalResult("b", "null", "B", "(new C()).b", DkmEvaluationResultFlags.Expandable),
                EvalResult("e", "B", "E", "(new C()).e", editableValue: "E.B"),
                EvalResult("s", "{S}", "S", "(new C()).s", DkmEvaluationResultFlags.Expandable),
                EvalResult("sn", "null", "S?", "(new C()).sn"));

            // A a = default(A);
            var more = GetChildren(children[0]);
            Verify(more,
                EvalResult("Static members", null, "", "A", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Class));
            more = GetChildren(more[0]);
            Verify(more,
                EvalResult("F", "1", "int", "A.F", DkmEvaluationResultFlags.ReadOnly),
                EvalResult("G", "2", "int", "A.G", DkmEvaluationResultFlags.ReadOnly));

            // S s = new S();
            more = GetChildren(children[3]);
            Verify(more,
                EvalResult("Static members", null, "", "S", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Class));
            more = GetChildren(more[0]);
            Verify(more,
                EvalResult("F", "null", "object", "S.F", DkmEvaluationResultFlags.ReadOnly),
                EvalResult("P", "3", "object {int}", "S.P", DkmEvaluationResultFlags.ReadOnly));
        }

        [Fact]
        public void StaticMembersBaseAndDerived()
        {
            var source =
@"class A
{
    static readonly int F = 1;
}
class B : A
{
}
class C : B
{
    static object P { get { return 2; } }
}
class P
{
    B b = new C();
}";
            var assembly = GetAssembly(source);
            var type = assembly.GetType("P");
            var rootExpr = "new P()";
            var value = CreateDkmClrValue(Activator.CreateInstance(type));
            var evalResult = FormatResult(rootExpr, value);
            Verify(evalResult,
                EvalResult(rootExpr, "{P}", "P", rootExpr, DkmEvaluationResultFlags.Expandable));
            var children = GetChildren(evalResult);
            Verify(children,
                EvalResult("b", "{C}", "B {C}", "(new P()).b", DkmEvaluationResultFlags.Expandable));
            children = GetChildren(children[0]);
            Verify(children,
                EvalResult("Static members", null, "", "C", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Class));
            children = GetChildren(children[0]);
            Verify(children,
                EvalResult("F", "1", "int", "A.F", DkmEvaluationResultFlags.ReadOnly),
                EvalResult("P", "2", "object {int}", "C.P", DkmEvaluationResultFlags.ReadOnly));
        }

        [Fact]
        public void NoExpansion_Members()
        {
            var source =
@"class A
{
    readonly int F = 1;
}
class B : A
{
}
class C : B
{
    static object P { get { return 2; } }
}
class D
{
    C F = new C();
}";
            var assembly = GetAssembly(source);
            var type = assembly.GetType("C");

            // Non-null value.
            var value = CreateDkmClrValue(Activator.CreateInstance(type),
                type: type);
            var evalResult = FormatResult("o", value, inspectionContext: CreateDkmInspectionContext(DkmEvaluationFlags.NoExpansion));
            Verify(evalResult,
                EvalResult("o", "{C}", "C", "o", DkmEvaluationResultFlags.None));

            // Null value.
            value = CreateDkmClrValue(null,
                type: type);
            evalResult = FormatResult("o", value, inspectionContext: CreateDkmInspectionContext(DkmEvaluationFlags.NoExpansion));
            Verify(evalResult,
                EvalResult("o", "null", "C", "o", DkmEvaluationResultFlags.None));

            // NoExpansion for children.
            value = CreateDkmClrValue(Activator.CreateInstance(assembly.GetType("D")));
            evalResult = FormatResult("o", value);
            Verify(evalResult,
                EvalResult("o", "{D}", "D", "o", DkmEvaluationResultFlags.Expandable));
            var children = GetChildren(
                evalResult,
                inspectionContext: CreateDkmInspectionContext(DkmEvaluationFlags.NoExpansion));
            Verify(children,
                EvalResult("F", "{C}", "C", "o.F", DkmEvaluationResultFlags.None));
        }

        [Fact]
        public void NoExpansion_DebuggerTypeProxy()
        {
            var source =
@"using System.Diagnostics;
[DebuggerTypeProxy(typeof(P))]
class C
{
    internal object F;
}
class D
{
    C F = new C();
}
internal class P
{
    private readonly C c;
    public P(C c)
    {
        this.c = c;
    }
    public object PF
    {
        get { return this.c.F; }
    }
}";
            var assembly = GetAssembly(source);
            var type = assembly.GetType("C");
            var value = CreateDkmClrValue(Activator.CreateInstance(type),
                type: type);
            var evalResult = FormatResult("o", value, inspectionContext: CreateDkmInspectionContext(DkmEvaluationFlags.NoExpansion));
            Verify(evalResult,
                EvalResult("o", "{C}", "C", "o", DkmEvaluationResultFlags.None));

            // NoExpansion for children.
            value = CreateDkmClrValue(Activator.CreateInstance(assembly.GetType("D")));
            evalResult = FormatResult("o", value);
            Verify(evalResult,
                EvalResult("o", "{D}", "D", "o", DkmEvaluationResultFlags.Expandable));
            var children = GetChildren(
                evalResult,
                inspectionContext: CreateDkmInspectionContext(DkmEvaluationFlags.NoExpansion));
            Verify(children,
                EvalResult("F", "{C}", "C", "o.F", DkmEvaluationResultFlags.None));
        }

        [Fact]
        public void NoExpansion_Array()
        {
            var value = CreateDkmClrValue(new[] { 1, 2, 3 });
            var evalResult = FormatResult("a", value, inspectionContext: CreateDkmInspectionContext(DkmEvaluationFlags.NoExpansion));
            Verify(evalResult,
                EvalResult("a", "{int[3]}", "int[]", "a", DkmEvaluationResultFlags.None));
        }

        [Fact]
        public void NoExpansion_Pointer()
        {
            var source =
@"unsafe class C
{
    internal C(long p)
    {
        this.P = (int*)p;
    }
    int* P;
}";
            var assembly = GetUnsafeAssembly(source);
            unsafe
            {
                int i = 4;
                long p = (long)&i;
                var type = assembly.GetType("C");
                var value = CreateDkmClrValue(type.Instantiate(p));
                var evalResult = FormatResult("o", value);
                Verify(evalResult,
                    EvalResult("o", "{C}", "C", "o", DkmEvaluationResultFlags.Expandable));
                var children = GetChildren(
                    evalResult,
                    inspectionContext: CreateDkmInspectionContext(DkmEvaluationFlags.NoExpansion));
                Verify(children,
                    EvalResult("P", PointerToString(new IntPtr(p)), "int*", "o.P", DkmEvaluationResultFlags.None));
            }
        }

        [WorkItem(933845)]
        [Fact]
        public void StaticMemberOfBaseType()
        {
            var source =
@"class A
{
    internal static object F = new B();
}
class B
{
    internal object G = 1;
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
                EvalResult("F", "{B}", "object {B}", "A.F", DkmEvaluationResultFlags.Expandable));
            children = GetChildren(children[0]);
            Verify(children,
                EvalResult("G", "1", "object {int}", "((B)A.F).G"));
        }

        [Fact]
        public void BaseTypeWithNamespace()
        {
            var source =
@"namespace N
{
    class B
    {
        int i = 0;
    }
}
class C : N.B
{
    int j = 0;
}";
            var assembly = GetAssembly(source);
            var type = assembly.GetType("C");
            var rootExpr = "new C()";
            var value = CreateDkmClrValue(Activator.CreateInstance(type));
            var evalResult = FormatResult(rootExpr, value);
            Verify(evalResult,
                EvalResult(rootExpr, "{C}", "C", rootExpr, DkmEvaluationResultFlags.Expandable));
            var children = GetChildren(evalResult);
            Verify(children,
                EvalResult("i", "0", "int", "(new C()).i"),
                EvalResult("j", "0", "int", "(new C()).j"));
        }

        /// <summary>
        /// Members should be in alphabetical order.
        /// </summary>
        [Fact]
        public void OrderedMembers()
        {
            var source =
@"interface I
{
    int M4 { get; }
}
class A : I
{
    public int M1 = 0;
    protected int m0 = 1;
    internal int m5 = 2;
    private int m4 = 3;
    int I.M4 { get { return 4; } }
    public int M7 { get { return 5; } }
    protected int m6 { get { return 6; } }
    internal int M3 { get { return 7; } }
    private int M2 { get { return 8; } }

}
    class B
    {
    public static int m2 = 0;
    protected static int m3 = 1;
    internal static int m6 = 2;
    private static int m7 = 3;
    public static int M4 { get { return 4; } }
    protected static int M5 { get { return 5; } }
    internal static int M0 { get { return 6; } }
    private static int M1 { get { return 7; } }
}
class C
{
    A a = new A();
    B b = new B();
}";
            var assembly = GetAssembly(source);
            var type = assembly.GetType("C");
            var rootExpr = "new C()";
            var value = CreateDkmClrValue(Activator.CreateInstance(type));
            var evalResult = FormatResult(rootExpr, value);
            Verify(evalResult,
                EvalResult(rootExpr, "{C}", "C", rootExpr, DkmEvaluationResultFlags.Expandable));
            var children = GetChildren(evalResult);
            Verify(children,
                EvalResult("a", "{A}", "A", "(new C()).a", DkmEvaluationResultFlags.Expandable),
                EvalResult("b", "{B}", "B", "(new C()).b", DkmEvaluationResultFlags.Expandable));

            // A a = new A();
            var more = GetChildren(children[0]);
            Verify(more,
                EvalResult("I.M4", "4", "int", "((I)(new C()).a).M4", DkmEvaluationResultFlags.ReadOnly),
                EvalResult("M1", "0", "int", "(new C()).a.M1"),
                EvalResult("M2", "8", "int", "(new C()).a.M2", DkmEvaluationResultFlags.ReadOnly),
                EvalResult("M3", "7", "int", "(new C()).a.M3", DkmEvaluationResultFlags.ReadOnly),
                EvalResult("M7", "5", "int", "(new C()).a.M7", DkmEvaluationResultFlags.ReadOnly),
                EvalResult("m0", "1", "int", "(new C()).a.m0"),
                EvalResult("m4", "3", "int", "(new C()).a.m4"),
                EvalResult("m5", "2", "int", "(new C()).a.m5"),
                EvalResult("m6", "6", "int", "(new C()).a.m6", DkmEvaluationResultFlags.ReadOnly));

            // B b = new B();
            more = GetChildren(children[1]);
            Verify(more,
                EvalResult("Static members", null, "", "B", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Class));
            more = GetChildren(more[0]);
            Verify(more,
                EvalResult("M0", "6", "int", "B.M0", DkmEvaluationResultFlags.ReadOnly),
                EvalResult("M1", "7", "int", "B.M1", DkmEvaluationResultFlags.ReadOnly),
                EvalResult("M4", "4", "int", "B.M4", DkmEvaluationResultFlags.ReadOnly),
                EvalResult("M5", "5", "int", "B.M5", DkmEvaluationResultFlags.ReadOnly),
                EvalResult("m2", "0", "int", "B.m2"),
                EvalResult("m3", "1", "int", "B.m3"),
                EvalResult("m6", "2", "int", "B.m6"),
                EvalResult("m7", "3", "int", "B.m7"));
        }

        /// <summary>
        /// Hide members that have compiler-generated names.
        /// </summary>
        /// <remarks>
        /// As in dev11, the FullName expressions don't parse.
        /// </remarks> 
        [Fact]
        public void HiddenMembers()
        {
            var source =
@".class public A
{
  .method public hidebysig specialname rtspecialname instance void .ctor() { ret }
  .field public object '@'
  .field public object '<'
  .field public static object '>'
  .field public static object '><'
  .field public object '<>'
  .field public object '1<>'
  .field public object '<2'
  .field public object '<>__'
  .field public object '<>k'
  .field public static object '<3>k'
  .field public static object '<<>>k'
  .field public static object '<>>k'
  .field public static object '<<>k'
  .field public static object '< >k'
  .field public object 'CS$'
  .field public object 'CS$<>0_'
  .field public object 'CS$<>7__8'
  .field public object 'CS$$<>7__8'
  .field public object 'CS<>7__8'
  .field public static object '$<>7__8'
  .field public static object 'CS$<M>7'
}
.class public B
{
  .method public hidebysig specialname rtspecialname instance void .ctor() { ret }
  .method public instance object '<>k__get'() { ldnull ret }
  .method public static object '<M>7__get'() { ldnull ret }
  .property instance object '@'() { .get instance object B::'<>k__get'() }
  .property instance object '<'() { .get instance object B::'<>k__get'() }
  .property object '>'() { .get object B::'<M>7__get'() }
  .property object '><'() { .get object B::'<M>7__get'() }
  .property instance object '<>'() { .get instance object B::'<>k__get'() }
  .property instance object '1<>'() { .get instance object B::'<>k__get'() }
  .property instance object '<2'() { .get instance object B::'<>k__get'() }
  .property instance object '<>__'() { .get instance object B::'<>k__get'() }
  .property instance object '<>k'() { .get instance object B::'<>k__get'() }
  .property object '<3>k'() { .get object B::'<M>7__get'() }
  .property object '<<>>k'() { .get object B::'<M>7__get'() }
  .property object '<>>k'() { .get object B::'<M>7__get'() }
  .property object '<<>k'() { .get object B::'<M>7__get'() }
  .property object '< >k'() { .get object B::'<M>7__get'() }
  .property instance object 'VB$'() { .get instance object B::'<>k__get'() }
  .property instance object 'VB$<>0_'() { .get instance object B::'<>k__get'() }
  .property instance object 'VB$Me7__8'() { .get instance object B::'<>k__get'() }
  .property instance object 'VB$$<>7__8'() { .get instance object B::'<>k__get'() }
  .property instance object 'VB<>7__8'() { .get instance object B::'<>k__get'() }
  .property object '$<>7__8'() { .get object B::'<M>7__get'() }
  .property object 'CS$<M>7'() { .get object B::'<M>7__get'() }
}";
            ImmutableArray<byte> assemblyBytes;
            ImmutableArray<byte> pdbBytes;
            CSharpTestBase.EmitILToArray(source, appendDefaultHeader: true, includePdb: false, assemblyBytes: out assemblyBytes, pdbBytes: out pdbBytes);
            var assembly = ReflectionUtilities.Load(assemblyBytes);

            var type = assembly.GetType("A");
            var rootExpr = "new A()";
            var value = CreateDkmClrValue(Activator.CreateInstance(type));
            var evalResult = FormatResult(rootExpr, value);
            Verify(evalResult,
                EvalResult(rootExpr, "{A}", "A", rootExpr, DkmEvaluationResultFlags.Expandable));
            var children = GetChildren(evalResult);
            Verify(children,
                EvalResult("1<>", "null", "object", fullName: null),
                EvalResult("@", "null", "object", fullName: null),
                EvalResult("CS<>7__8", "null", "object", fullName: null),
                EvalResult("Static members", null, "", "A", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Class));
            children = GetChildren(children[children.Length - 1]);
            Verify(children,
                EvalResult(">", "null", "object", fullName: null),
                EvalResult("><", "null", "object", fullName: null));

            type = assembly.GetType("B");
            rootExpr = "new B()";
            value = CreateDkmClrValue(Activator.CreateInstance(type));
            evalResult = FormatResult(rootExpr, value);
            Verify(evalResult,
                EvalResult(rootExpr, "{B}", "B", rootExpr, DkmEvaluationResultFlags.Expandable));
            children = GetChildren(evalResult);
            Verify(children,
                EvalResult("1<>", "null", "object", fullName: null, flags: DkmEvaluationResultFlags.ReadOnly),
                EvalResult("@", "null", "object", fullName: null, flags: DkmEvaluationResultFlags.ReadOnly),
                EvalResult("VB<>7__8", "null", "object", fullName: null, flags: DkmEvaluationResultFlags.ReadOnly),
                EvalResult("Static members", null, "", "B", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Class));
            children = GetChildren(children[children.Length - 1]);
            Verify(children,
                EvalResult(">", "null", "object", fullName: null, flags: DkmEvaluationResultFlags.ReadOnly),
                EvalResult("><", "null", "object", fullName: null, flags: DkmEvaluationResultFlags.ReadOnly));
        }

        /// <summary>
        /// ImmutableArray includes [DebuggerDisplay(...)]
        /// that returns the underlying array.
        /// </summary>
        [Fact]
        public void ImmutableArray()
        {
            var runtime = new DkmClrRuntimeInstance(ReflectionUtilities.GetMscorlib(typeof(ImmutableArray<>).Assembly));
            var rawValue = System.Collections.Immutable.ImmutableArray.Create(1, 2, 3);
            var type = runtime.GetType(typeof(ImmutableArray<>)).MakeGenericType(runtime.GetType(typeof(int)));
            var value = CreateDkmClrValue(
                value: rawValue,
                type: type,
                evalFlags: DkmEvaluationResultFlags.None);
            var evalResult = FormatResult("c", value);
            Verify(evalResult,
                EvalResult("c", "Length = 3", "System.Collections.Immutable.ImmutableArray<int>", "c", DkmEvaluationResultFlags.Expandable));
            var children = GetChildren(evalResult);
            Verify(children,
                EvalResult("[0]", "1", "int", "c.array[0]"),
                EvalResult("[1]", "2", "int", "c.array[1]"),
                EvalResult("[2]", "3", "int", "c.array[2]"),
                EvalResult("Static members", null, "", "System.Collections.Immutable.ImmutableArray<int>", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Class));
        }

        [WorkItem(933845)]
        [Fact]
        public void DeclaredTypeObject()
        {
            var source = @"
class A
{
    internal object F;
}
class B : A
{
    internal B(object f)
    {
        F = f;
    }
    internal object P { get { return this.F; } }
}
class C
{
    A a = new B(1);
    B b = new B(2);
    object o = new B(3);
}
";
            var assembly = GetAssembly(source);
            var typeC = assembly.GetType("C");
            var instanceC = typeC.Instantiate();

            var children = GetChildren(FormatResult("c", CreateDkmClrValue(instanceC)));
            Verify(children,
                EvalResult("a", "{B}", "A {B}", "c.a", DkmEvaluationResultFlags.Expandable),
                EvalResult("b", "{B}", "B", "c.b", DkmEvaluationResultFlags.Expandable),
                EvalResult("o", "{B}", "object {B}", "c.o", DkmEvaluationResultFlags.Expandable));

            Verify(GetChildren(children[0]), // as A
                EvalResult("F", "1", "object {int}", "c.a.F"),
                EvalResult("P", "1", "object {int}", "((B)c.a).P", DkmEvaluationResultFlags.ReadOnly));

            Verify(GetChildren(children[1]), // as B
                EvalResult("F", "2", "object {int}", "c.b.F"),
                EvalResult("P", "2", "object {int}", "c.b.P", DkmEvaluationResultFlags.ReadOnly));

            Verify(GetChildren(children[2]), // as object
                EvalResult("F", "3", "object {int}", "((A)c.o).F"),
                EvalResult("P", "3", "object {int}", "((B)c.o).P", DkmEvaluationResultFlags.ReadOnly));
        }

        [WorkItem(933845)]
        [Fact]
        public void DeclaredTypeObject_Array()
        {
            var source = @"
interface I
{
    int Q { get; set; }
}
class A
{
    internal object F;
}
class B : A, I
{
    internal B(object f)
    {
        F = f;
    }
    internal object P { get { return this.F; } }
    int I.Q { get; set; }
}
class C
{
    A[] a = new A[] { new B(1) };
    B[] b = new B[] { new B(2) };
    I[] i = new I[] { new B(3) };
    object[] o = new object[] { new B(4) };
}
";
            var assembly = GetAssembly(source);
            var typeC = assembly.GetType("C");
            var instanceC = typeC.Instantiate();

            var children = GetChildren(FormatResult("c", CreateDkmClrValue(instanceC)));
            Verify(children,
                EvalResult("a", "{A[1]}", "A[]", "c.a", DkmEvaluationResultFlags.Expandable),
                EvalResult("b", "{B[1]}", "B[]", "c.b", DkmEvaluationResultFlags.Expandable),
                EvalResult("i", "{I[1]}", "I[]", "c.i", DkmEvaluationResultFlags.Expandable),
                EvalResult("o", "{object[1]}", "object[]", "c.o", DkmEvaluationResultFlags.Expandable));

            Verify(GetChildren(GetChildren(children[0]).Single()), // as A[]
                EvalResult("F", "1", "object {int}", "c.a[0].F"),
                EvalResult("I.Q", "0", "int", "((I)c.a[0]).Q"),
                EvalResult("P", "1", "object {int}", "((B)c.a[0]).P", DkmEvaluationResultFlags.ReadOnly));

            Verify(GetChildren(GetChildren(children[1]).Single()), // as B[]
                EvalResult("F", "2", "object {int}", "c.b[0].F"),
                EvalResult("I.Q", "0", "int", "((I)c.b[0]).Q"),
                EvalResult("P", "2", "object {int}", "c.b[0].P", DkmEvaluationResultFlags.ReadOnly));

            Verify(GetChildren(GetChildren(children[2]).Single()), // as I[]
                EvalResult("F", "3", "object {int}", "((A)c.i[0]).F"),
                EvalResult("I.Q", "0", "int", "c.i[0].Q"),
                EvalResult("P", "3", "object {int}", "((B)c.i[0]).P", DkmEvaluationResultFlags.ReadOnly));

            Verify(GetChildren(GetChildren(children[3]).Single()), // as object[]
                EvalResult("F", "4", "object {int}", "((A)c.o[0]).F"),
                EvalResult("I.Q", "0", "int", "((I)c.o[0]).Q"),
                EvalResult("P", "4", "object {int}", "((B)c.o[0]).P", DkmEvaluationResultFlags.ReadOnly));
        }

        [WorkItem(933845)]
        [Fact]
        public void DeclaredTypeObject_Static()
        {
            var source = @"
class A
{
    internal static object F = new B();
}
class B
{
    internal object G = 1;
}
class C
{
    A a = new A();
}
";
            var assembly = GetAssembly(source);
            var typeC = assembly.GetType("C");
            var instanceC = typeC.Instantiate();

            var children = GetChildren(FormatResult("c", CreateDkmClrValue(instanceC)));
            Verify(children,
                EvalResult("a", "{A}", "A", "c.a", DkmEvaluationResultFlags.Expandable));

            children = GetChildren(children[0]);
            Verify(children,
                EvalResult("Static members", null, "", "A", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Class));

            children = GetChildren(children[0]);
            Verify(children,
                EvalResult("F", "{B}", "object {B}", "A.F", DkmEvaluationResultFlags.Expandable));

            children = GetChildren(children[0]);
            Verify(children,
                EvalResult("G", "1", "object {int}", "((B)A.F).G"));
        }

        [WorkItem(933845)]
        [Fact]
        public void ExceptionThrownFlag()
        {
            var source = @"
struct S
{
    int x;

    S This 
    { 
        get 
        { 
            throw new System.Exception(); 
        }
    }
}
";
            var assembly = GetAssembly(source);
            var type = assembly.GetType("S");
            var value = type.Instantiate();

            var children = GetChildren(FormatResult("s", CreateDkmClrValue(value)));
            Verify(children,
                EvalResult("This", "'s.This' threw an exception of type 'System.Exception'", "S {System.Exception}", "s.This", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly | DkmEvaluationResultFlags.ExceptionThrown),
                EvalResult("x", "0", "int", "s.x"));
        }

        [WorkItem(933845)]
        [Fact]
        public void ExceptionThrownFlag_ProxyType()
        {
            var source = @"
struct S
{
    int x;

    S This
    {
        get
        {
            throw new E();
        }
    }
}

[System.Diagnostics.DebuggerTypeProxy(typeof(EProxy))]
class E : System.Exception
{
    public int y = 1;
}

class EProxy
{
    public int z;

    public EProxy(E e)
    {
        this.z = e.y;
    }
}
";
            var assembly = GetAssembly(source);
            var type = assembly.GetType("S");
            var value = type.Instantiate();

            var children = GetChildren(FormatResult("s", CreateDkmClrValue(value)));
            Verify(children,
                EvalResult("This", "'s.This' threw an exception of type 'E'", "S {E}", "s.This", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly | DkmEvaluationResultFlags.ExceptionThrown),
                EvalResult("x", "0", "int", "s.x"));

            children = GetChildren(children[0]);
            Verify(children,
                EvalResult("z", "1", "int", null),
                EvalResult("Raw View", null, "", "s.This, raw", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly | DkmEvaluationResultFlags.ExceptionThrown, DkmEvaluationResultCategory.Data));
        }

        [WorkItem(933845)]
        [WorkItem(967366)]
        [Fact]
        public void ExceptionThrownFlag_DerivedExceptionType()
        {
            var source = @"
struct S
{
    int x;

    S This
    {
        get
        {
            throw new System.NullReferenceException();
        }
    }
}
";
            using (new EnsureEnglishUICulture())
            {
                var runtime = new DkmClrRuntimeInstance(ReflectionUtilities.GetMscorlibAndSystemCore(GetAssembly(source)));
                using (runtime.Load())
                {
                    var type = runtime.GetType("S");
                    var value = CreateDkmClrValue(type.Instantiate(), type: type);
                    var children = GetChildren(FormatResult("s", value));
                    Verify(children,
                        EvalResult(
                            "This",
                            "'s.This' threw an exception of type 'System.NullReferenceException'",
                            "S {System.NullReferenceException}",
                            "s.This",
                            DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly | DkmEvaluationResultFlags.ExceptionThrown),
                        EvalResult("x", "0", "int", "s.x"));

                    // NOTE: The real EE will show only the exception message, but our mock does not support autoexp.
                    children = GetChildren(children[0]);
                    Verify(children[6],
                        EvalResult("Message", "\"Object reference not set to an instance of an object.\"", "string", null, DkmEvaluationResultFlags.RawString | DkmEvaluationResultFlags.ReadOnly));
                }
            }
        }

        [WorkItem(933845)]
        [Fact]
        public void ExceptionThrownFlag_DebuggerDisplay()
        {
            var source = @"
struct S
{
    int x;

    S This
    {
        get
        {
            throw new E();
        }
    }
}

[System.Diagnostics.DebuggerDisplay(""DisplayValue"")]
class E : System.Exception
{
}
";
            using (new EnsureEnglishUICulture())
            {
                var runtime = new DkmClrRuntimeInstance(ReflectionUtilities.GetMscorlib(GetAssembly(source)));
                using (runtime.Load())
                {
                    var type = runtime.GetType("S");
                    var value = CreateDkmClrValue(type.Instantiate(), type: type);
                    var children = GetChildren(FormatResult("s", value));
                    Verify(children,
                        EvalResult("This", "'s.This' threw an exception of type 'E'", "S {E}", "s.This", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly | DkmEvaluationResultFlags.ExceptionThrown),
                        EvalResult("x", "0", "int", "s.x"));
                    children = GetChildren(children[0]);
                    Verify(children[6],
                        EvalResult("Message", "\"Exception of type 'E' was thrown.\"", "string", null, DkmEvaluationResultFlags.RawString | DkmEvaluationResultFlags.ReadOnly));
                }
            }
        }

        [Fact]
        public void ExceptionThrownFlag_AtRoot()
        {
            var value = CreateDkmClrValue(
                new NullReferenceException(),
                evalFlags: DkmEvaluationResultFlags.ExceptionThrown);
            var evalResult = FormatResult("c.P", value);
            Verify(evalResult,
                EvalResult(
                    "c.P",
                    "'c.P' threw an exception of type 'System.NullReferenceException'",
                    "System.NullReferenceException", "c.P",
                    DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ExceptionThrown));
        }

        [WorkItem(1043730)]
        [Fact]
        public void ExceptionThrownFlag_Nullable()
        {
            /*
            var source =
@"class C
{
    string str;
}";
            */
            using (new EnsureEnglishUICulture())
            {
                var runtime = new DkmClrRuntimeInstance(ReflectionUtilities.GetMscorlib());
                var type = runtime.GetType(typeof(NullReferenceException));
                var value = CreateDkmClrValue(
                    type.Instantiate(),
                    type: type,
                    evalFlags: DkmEvaluationResultFlags.ExceptionThrown);
                var evalResult = FormatResult("c?.str.Length", value, runtime.GetType(typeof(int?)));
                Verify(evalResult,
                    EvalResult(
                        "c?.str.Length",
                        "'c?.str.Length' threw an exception of type 'System.NullReferenceException'",
                        "int? {System.NullReferenceException}",
                        "c?.str.Length",
                        DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ExceptionThrown));
                var children = GetChildren(evalResult);
                Verify(children[6],
                    EvalResult(
                        "Message",
                        "\"Object reference not set to an instance of an object.\"",
                        "string",
                        null,
                        DkmEvaluationResultFlags.RawString | DkmEvaluationResultFlags.ReadOnly));
            }
        }

        [WorkItem(1043730)]
        [Fact]
        public void ExceptionThrownFlag_NullableMember()
        {
            var source =
@"class C
{
    int? P
    {
        get { throw new System.NullReferenceException(); }
    }
}";
            var assembly = GetAssembly(source);
            var type = assembly.GetType("C");
            var value = CreateDkmClrValue(type.Instantiate());
            var evalResult = FormatResult("c", value);
            var children = GetChildren(evalResult);
            Verify(children,
                EvalResult(
                    "P",
                    "'c.P' threw an exception of type 'System.NullReferenceException'",
                    "int? {System.NullReferenceException}",
                    "c.P",
                    DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly | DkmEvaluationResultFlags.ExceptionThrown));
        }

        [Fact]
        public void MultilineString()
        {
            var str = "\r\nline1\r\nline2";
            var quotedStr = "\"\\r\\nline1\\r\\nline2\"";
            var value = CreateDkmClrValue(str, evalFlags: DkmEvaluationResultFlags.RawString);
            var evalResult = FormatResult("str", value);
            Verify(evalResult,
                EvalResult("str", quotedStr, "string", "str", DkmEvaluationResultFlags.RawString, editableValue: quotedStr));
        }

        [Fact]
        public void UnicodeChar()
        {
            // This char is printable, so we expect the EditableValue to just be the char.
            var value = CreateDkmClrValue('\u1234');
            var evalResult = FormatResult("c", value);
            Verify(evalResult,
                EvalResult("c", "4660 '\u1234'", "char", "c", editableValue: "'\u1234'"));

            // This char is not printable, so we expect the EditableValue to be the unicode escape representation.
            value = CreateDkmClrValue('\u001f');
            evalResult = FormatResult("c", value, inspectionContext: CreateDkmInspectionContext(radix: 16));
            Verify(evalResult,
                EvalResult("c", "0x001f '\\u001f'", "char", "c", editableValue: "'\\u001f'"));

            // This char is not printable, but there is a specific escape character.
            value = CreateDkmClrValue('\u0007');
            evalResult = FormatResult("c", value, inspectionContext: CreateDkmInspectionContext(radix: 16));
            Verify(evalResult,
                EvalResult("c", "0x0007 '\\a'", "char", "c", editableValue: "'\\a'"));
        }

        [WorkItem(1138095)]
        [Fact]
        public void UnicodeString()
        {
            var value = CreateDkmClrValue("\u1234\u001f\u0007");
            var evalResult = FormatResult("s", value);
            Verify(evalResult,
                EvalResult("s", $"\"{'\u1234'}\\u001f\\a\"", "string", "s", editableValue: $"\"{'\u1234'}\\u001f\\a\"", flags: DkmEvaluationResultFlags.RawString));
        }

        [WorkItem(1002381)]
        [Fact]
        public void BaseTypeEditableValue()
        {
            var source =
@"using System;
using System.Collections.Generic;
[Flags] enum E { A = 1, B = 2 }
class C
{
    IEnumerable<char> s = string.Empty;
    object d = 1M;
    ValueType e = E.A | E.B;
}";
            var assembly = GetAssembly(source);
            var type = assembly.GetType("C");
            var value = CreateDkmClrValue(Activator.CreateInstance(type));
            var evalResult = FormatResult("o", value);
            Verify(evalResult,
                EvalResult("o", "{C}", "C", "o", DkmEvaluationResultFlags.Expandable));
            var children = GetChildren(evalResult);
            Verify(children,
                EvalResult("d", "1", "object {decimal}", "o.d", editableValue: "1M"),
                EvalResult("e", "A | B", "System.ValueType {E}", "o.e", DkmEvaluationResultFlags.None, editableValue: "E.A | E.B"),
                EvalResult("s", "\"\"", "System.Collections.Generic.IEnumerable<char> {string}", "o.s", DkmEvaluationResultFlags.RawString, editableValue: "\"\""));
        }

        [WorkItem(965892)]
        [Fact]
        public void DeclaredTypeAndRuntimeTypeDifferent()
        {
            var source =
@"class A { }
class B : A { }
";
            var assembly = GetAssembly(source);
            var type = assembly.GetType("B");
            var declaredType = assembly.GetType("A");
            var value = CreateDkmClrValue(Activator.CreateInstance(type), type);
            var evalResult = FormatResult("a", value, new DkmClrType((TypeImpl)declaredType));
            Verify(evalResult,
                EvalResult("a", "{B}", "A {B}", "a", DkmEvaluationResultFlags.None));
            var children = GetChildren(evalResult);
            Verify(children);
        }

        /// <summary>
        /// Full name should be null for members of thrown
        /// exception since there's no valid expression.
        /// </summary>
        [WorkItem(1003260)]
        [Fact]
        public void ExceptionThrown_Member()
        {
            var source =
@"class E : System.Exception
{
    internal object F;
}
class C
{
    internal object P { get { throw new E() { F = 1 }; } }
    internal object Q { get { return new E() { F = 2 }; } }
}";
            using (new EnsureEnglishUICulture())
            {
                var runtime = new DkmClrRuntimeInstance(ReflectionUtilities.GetMscorlib(GetAssembly(source)));
                using (runtime.Load())
                {
                    var type = runtime.GetType("C");
                    var value = CreateDkmClrValue(type.Instantiate(), type: type);
                    var evalResult = FormatResult("o", value);
                    Verify(evalResult,
                        EvalResult("o", "{C}", "C", "o", DkmEvaluationResultFlags.Expandable));
                    var children = GetChildren(evalResult);
                    Verify(children,
                        EvalResult("P", "'o.P' threw an exception of type 'E'", "object {E}", "o.P", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly | DkmEvaluationResultFlags.ExceptionThrown),
                        EvalResult("Q", "{E: Exception of type 'E' was thrown.}", "object {E}", "o.Q", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly));
                    var moreChildren = GetChildren(children[0]);
                    Verify(moreChildren[1],
                        EvalResult("F", "1", "object {int}", null));
                    Verify(moreChildren[7],
                        EvalResult("Message", "\"Exception of type 'E' was thrown.\"", "string", null, DkmEvaluationResultFlags.RawString | DkmEvaluationResultFlags.ReadOnly));
                    moreChildren = GetChildren(children[1]);
                    Verify(moreChildren[1],
                        EvalResult("F", "2", "object {int}", "((E)o.Q).F"));
                    Verify(moreChildren[7],
                        EvalResult("Message", "\"Exception of type 'E' was thrown.\"", "string", "((System.Exception)o.Q).Message", DkmEvaluationResultFlags.RawString | DkmEvaluationResultFlags.ReadOnly));
                }
            }
        }

        [WorkItem(1026721)]
        [Fact]
        public void ExceptionThrown_ReadOnly()
        {
            var source =
@"class RO : System.Exception
{
}
class RW : System.Exception
{
    internal object F;
}
class C
{
    internal object RO1 { get { throw new RO(); } }
    internal object RO2 { get { throw new RW(); } }
    internal object RW1 { get { throw new RO(); } set { } }
    internal object RW2 { get { throw new RW(); } set { } }
}";
            var assembly = GetAssembly(source);
            var type = assembly.GetType("C");
            var value = CreateDkmClrValue(Activator.CreateInstance(type));
            var evalResult = FormatResult("o", value);
            Verify(evalResult,
                EvalResult("o", "{C}", "C", "o", DkmEvaluationResultFlags.Expandable));
            var children = GetChildren(evalResult);
            Verify(children,
                EvalResult("RO1", "'o.RO1' threw an exception of type 'RO'", "object {RO}", "o.RO1", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly | DkmEvaluationResultFlags.ExceptionThrown),
                EvalResult("RO2", "'o.RO2' threw an exception of type 'RW'", "object {RW}", "o.RO2", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly | DkmEvaluationResultFlags.ExceptionThrown),
                EvalResult("RW1", "'o.RW1' threw an exception of type 'RO'", "object {RO}", "o.RW1", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ExceptionThrown),
                EvalResult("RW2", "'o.RW2' threw an exception of type 'RW'", "object {RW}", "o.RW2", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ExceptionThrown));
        }

        [Fact]
        public void NameConflictsWithFieldOnBase()
        {
            var source = @"
class A
{
    private int f;
}
class B : A
{
    internal double f;
}";
            var assembly = GetAssembly(source);
            var typeB = assembly.GetType("B");
            var instanceB = Activator.CreateInstance(typeB);
            var value = CreateDkmClrValue(instanceB, typeB);
            var result = FormatResult("b", value);
            Verify(GetChildren(result),
                EvalResult("f (A)", "0", "int", "((A)b).f"),
                EvalResult("f", "0", "double", "b.f"));

            var typeA = assembly.GetType("A");
            value = CreateDkmClrValue(instanceB, typeB);
            result = FormatResult("a", value, new DkmClrType((TypeImpl)typeA));
            Verify(GetChildren(result),
                EvalResult("f (A)", "0", "int", "a.f"),
                EvalResult("f", "0", "double", "((B)a).f"));
        }

        [Fact]
        public void NameConflictsWithFieldsOnMultipleBase()
        {
            var source = @"
class A
{
    private int f;
}
class B : A
{
    internal double f;
}
class C : B
{
}";
            var assembly = GetAssembly(source);
            var typeC = assembly.GetType("C");
            var instanceC = Activator.CreateInstance(typeC);
            var value = CreateDkmClrValue(instanceC, typeC);
            var result = FormatResult("c", value);
            Verify(GetChildren(result),
                EvalResult("f (A)", "0", "int", "((A)c).f"),
                EvalResult("f", "0", "double", "c.f"));

            var typeB = assembly.GetType("B");
            value = CreateDkmClrValue(instanceC, typeC);
            result = FormatResult("b", value, new DkmClrType((TypeImpl)typeB));
            Verify(GetChildren(result),
                EvalResult("f (A)", "0", "int", "((A)b).f"),
                EvalResult("f", "0", "double", "b.f"));
        }

        [Fact]
        public void NameConflictsWithPropertyOnNestedBase()
        {
            var source = @"
class A
{
    private int P { get; set; }

    internal class B : A
    {
        internal double P { get; set; }
    }
}
class C : A.B
{
}";
            var assembly = GetAssembly(source);
            var type = assembly.GetType("C");
            var instanceC = Activator.CreateInstance(type);
            var value = CreateDkmClrValue(instanceC, type);
            var result = FormatResult("c", value);
            Verify(GetChildren(result),
                EvalResult("P (A)", "0", "int", "((A)c).P"),
                EvalResult("P", "0", "double", "c.P"));
        }

        [Fact]
        public void NameConflictsWithPropertyOnGenericBase()
        {
            var source = @"
class A<T>
{
    public T P { get; set; }
}
class B : A<int>
{
    private double P { get; set; }
}
class C : B
{
}";
            var assembly = GetAssembly(source);
            var type = assembly.GetType("C");
            var instanceC = Activator.CreateInstance(type);
            var value = CreateDkmClrValue(instanceC, type);
            var result = FormatResult("c", value);
            Verify(GetChildren(result),
                EvalResult("P (A<int>)", "0", "int", "((A<int>)c).P"),
                EvalResult("P", "0", "double", "c.P"));
        }

        [Fact]
        public void PropertyNameConflictsWithFieldOnBase()
        {
            var source = @"
class A
{
    public string F;
}
class B : A
{
    private double F { get; set; }
}
class C : B
{
}";
            var assembly = GetAssembly(source);
            var type = assembly.GetType("C");
            var instanceC = Activator.CreateInstance(type);
            var value = CreateDkmClrValue(instanceC, type);
            var result = FormatResult("c", value);
            Verify(GetChildren(result),
                EvalResult("F (A)", "null", "string", "((A)c).F"),
                EvalResult("F", "0", "double", "c.F"));
        }

        [Fact]
        public void NameConflictsWithIndexerOnBase()
        {
            var source = @"
class A
{
    public string this[string x]
    {
        get
        {
            return ""DeriveMe"";
        }
    }
}
class B : A
{
    public string @this { get { return ""Derived""; } }
}";
            var assembly = GetAssembly(source);
            var type = assembly.GetType("B");
            var instanceB = Activator.CreateInstance(type);
            var value = CreateDkmClrValue(instanceB, type);
            var result = FormatResult("b", value);
            Verify(GetChildren(result),
                EvalResult("@this", "\"Derived\"", "string", "b.@this", DkmEvaluationResultFlags.RawString | DkmEvaluationResultFlags.ReadOnly));
        }

        [Fact]
        public void NameConflictsWithPropertyHiddenByNameOnBase()
        {
            var source = @"
class A
{
    static int S = 42;
    internal virtual int P { get { return 43; } }
}
class B : A
{
    internal override int P { get { return 45; } }
}
class C : B
{
    new double P { get { return 4.4; } }
}";
            var assembly = GetAssembly(source);
            var type = assembly.GetType("C");
            var instanceC = Activator.CreateInstance(type);
            var value = CreateDkmClrValue(instanceC, type);
            var result = FormatResult("c", value);
            var children = GetChildren(result);
            Verify(children,
                EvalResult("P (B)", "45", "int", "((B)c).P", DkmEvaluationResultFlags.ReadOnly),
                EvalResult("P", "4.4", "double", "c.P", DkmEvaluationResultFlags.ReadOnly),
                EvalResult("Static members", null, "", "C", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Class));
            Verify(GetChildren(children[2]),
                EvalResult("S", "42", "int", "A.S"));
        }

        [Fact, WorkItem(1074435)]
        public void NameConflictsWithExplicitInterfaceImplementation()
        {
            var source = @"
interface I
{
    int P { get; }
}
class A : I
{
    int I.P { get { return 1; } }
}
class B : A, I
{
    int I.P { get { return 2; } }
}
class C : B, I
{
    int I.P { get { return 3; } }
}";
            var assembly = GetAssembly(source);
            var typeB = assembly.GetType("B");
            var typeC = assembly.GetType("C");
            var instanceC = Activator.CreateInstance(typeC);
            var value = CreateDkmClrValue(instanceC, typeC);
            var result = FormatResult("b", value, new DkmClrType((TypeImpl)typeB));
            var children = GetChildren(result);
            // Note:  The names and full names below aren't the best...  That is, we never
            // display a type name following the name for types declared on interfaces
            // (e.g. "I.P (A)"), and since only the most derived property (C.I.P) is actually
            // callable from C#, we don't have a way to generate a full name for the others.
            // I think this is OK, because the case is uncommon and native didn't support
            // "Add Watch" on explicit interface implementations (it just generated "c.I.P").
            Verify(children,
                EvalResult("I.P", "1", "int", "((I)b).P", DkmEvaluationResultFlags.ReadOnly),
                EvalResult("I.P", "2", "int", "((I)b).P", DkmEvaluationResultFlags.ReadOnly),
                EvalResult("I.P", "3", "int", "((I)b).P", DkmEvaluationResultFlags.ReadOnly));
        }

        [Fact, WorkItem(1074435)]
        public void NameConflictsWithInterfaceReimplementation()
        {
            var source = @"
interface I
{
    int P { get; }
}
class A : I
{
    public int P { get { return 1; } }
}
class B : A, I
{
    public int P { get { return 2; } }
}
class C : B, I
{
    public int P { get { return 3; } }
}";
            var assembly = GetAssembly(source);
            var typeB = assembly.GetType("B");
            var typeC = assembly.GetType("C");
            var instanceC = Activator.CreateInstance(typeC);
            var value = CreateDkmClrValue(instanceC, typeC);
            var result = FormatResult("b", value, new DkmClrType((TypeImpl)typeB));
            var children = GetChildren(result);
            Verify(children,
                EvalResult("P (A)", "1", "int", "((A)b).P", DkmEvaluationResultFlags.ReadOnly),
                EvalResult("P (B)", "2", "int", "b.P", DkmEvaluationResultFlags.ReadOnly),
                EvalResult("P", "3", "int", "((C)b).P", DkmEvaluationResultFlags.ReadOnly));
        }

        [Fact]
        public void NameConflictsWithVirtualPropertiesAcrossDeclaredType()
        {
            var source = @"
class A 
{
    public virtual int P { get { return 1; } }
}
class B : A
{
    public override int P { get { return 2; } }
}
class C : B
{
}
class D : C
{
    public override int P { get { return 3; } }
}";
            var assembly = GetAssembly(source);
            var typeC = assembly.GetType("C");
            var typeD = assembly.GetType("D");
            var instanceD = Activator.CreateInstance(typeD);
            var value = CreateDkmClrValue(instanceD, typeD);
            var result = FormatResult("c", value, new DkmClrType((TypeImpl)typeC));
            var children = GetChildren(result);
            // Ideally, we would only emit "c.P" for the full name here, but the added
            // complexity of figuring that out (vs. always just calling the most derived)
            // doesn't seem worth it.
            Verify(children,
                EvalResult("P", "3", "int", "((D)c).P", DkmEvaluationResultFlags.ReadOnly));
        }

        /// <summary>
        /// Do not copy state from parent.
        /// </summary>
        [WorkItem(1028624)]
        [Fact]
        public void DoNotCopyParentState()
        {
            var sourceA =
@"public class A
{
    public static object F = 1;
    internal object G = 2;
}";
            var sourceB =
@"class B
{
    private A _1 = new A();
    protected A _2 = new A();
    internal A _3 = new A();
    public A _4 = new A();
}";
            var compilationA = CSharpTestBase.CreateCompilationWithMscorlib(sourceA, options: TestOptions.ReleaseDll);
            var bytesA = compilationA.EmitToArray();
            var referenceA = MetadataReference.CreateFromImage(bytesA);

            var compilationB = CSharpTestBase.CreateCompilationWithMscorlib(sourceB, options: TestOptions.DebugDll, references: new MetadataReference[] { referenceA });
            var bytesB = compilationB.EmitToArray();
            var assemblyA = ReflectionUtilities.Load(bytesA);
            var assemblyB = ReflectionUtilities.Load(bytesB);

            DkmClrRuntimeInstance runtime = null;
            GetModuleDelegate getModule = (r, a) => (a == assemblyB) ? new DkmClrModuleInstance(r, a, new DkmModule(a.GetName().Name + ".dll")) : null;
            runtime = new DkmClrRuntimeInstance(ReflectionUtilities.GetMscorlibAndSystemCore(assemblyA, assemblyB), getModule: getModule);
            using (runtime.Load())
            {
                var type = runtime.GetType("B");
                var value = CreateDkmClrValue(type.Instantiate(), type: type);
                // Format with "Just my code".
                var inspectionContext = CreateDkmInspectionContext(DkmEvaluationFlags.HideNonPublicMembers, runtimeInstance: runtime);
                var evalResult = FormatResult("o", value, inspectionContext: inspectionContext);
                var children = GetChildren(evalResult, inspectionContext: inspectionContext);
                Verify(children,
                    EvalResult("_1", "{A}", "A", "o._1", DkmEvaluationResultFlags.Expandable, DkmEvaluationResultCategory.Data, DkmEvaluationResultAccessType.Private),
                    EvalResult("_2", "{A}", "A", "o._2", DkmEvaluationResultFlags.Expandable, DkmEvaluationResultCategory.Data, DkmEvaluationResultAccessType.Protected),
                    EvalResult("_3", "{A}", "A", "o._3", DkmEvaluationResultFlags.Expandable, DkmEvaluationResultCategory.Data, DkmEvaluationResultAccessType.Internal),
                    EvalResult("_4", "{A}", "A", "o._4", DkmEvaluationResultFlags.Expandable, DkmEvaluationResultCategory.Data, DkmEvaluationResultAccessType.Public));
                var moreChildren = GetChildren(children[0], inspectionContext: inspectionContext);
                Verify(moreChildren,
                    EvalResult("Static members", null, "", "A", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Class, DkmEvaluationResultAccessType.None),
                    EvalResult("Non-Public members", null, "", "o._1, hidden", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Data, DkmEvaluationResultAccessType.None));
                moreChildren = GetChildren(children[1], inspectionContext: inspectionContext);
                Verify(moreChildren,
                    EvalResult("Static members", null, "", "A", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Class, DkmEvaluationResultAccessType.None),
                    EvalResult("Non-Public members", null, "", "o._2, hidden", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Data, DkmEvaluationResultAccessType.None));
                moreChildren = GetChildren(children[2], inspectionContext: inspectionContext);
                Verify(moreChildren,
                    EvalResult("Static members", null, "", "A", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Class, DkmEvaluationResultAccessType.None),
                    EvalResult("Non-Public members", null, "", "o._3, hidden", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Data, DkmEvaluationResultAccessType.None));
                moreChildren = GetChildren(children[3], inspectionContext: inspectionContext);
                Verify(moreChildren,
                    EvalResult("Static members", null, "", "A", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Class, DkmEvaluationResultAccessType.None),
                    EvalResult("Non-Public members", null, "", "o._4, hidden", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Data, DkmEvaluationResultAccessType.None));
            }
        }

        [WorkItem(1130978)]
        [Fact]
        public void NullableValue_Error()
        {
            var source =
@"class C
{
    bool F() { return false; }
    int? P
    {
        get
        {
            while (!F()) { }
            return null;
        }
    }
}";
            DkmClrRuntimeInstance runtime = null;
            GetMemberValueDelegate getMemberValue = (v, m) => (m == "P") ? CreateErrorValue(runtime.GetType(typeof(int?)), "Function evaluation timed out") : null;
            runtime = new DkmClrRuntimeInstance(ReflectionUtilities.GetMscorlibAndSystemCore(GetAssembly(source)), getMemberValue: getMemberValue);
            using (runtime.Load())
            {
                var type = runtime.GetType("C");
                var value = CreateDkmClrValue(type.Instantiate(), type: type);
                var memberValue = value.GetMemberValue("P", (int)System.Reflection.MemberTypes.Property, "C", DefaultInspectionContext);
                var evalResult = FormatResult("o.P", memberValue);
                Verify(evalResult,
                    EvalFailedResult("o.P", "Function evaluation timed out", "int?", "o.P"));
            }
        }

        [Fact]
        public void RootCastExpression()
        {
            var source =
@"class C
{
    object F = 3;
}";
            var runtime = new DkmClrRuntimeInstance(ReflectionUtilities.GetMscorlib(GetAssembly(source)));
            using (runtime.Load())
            {
                var typeC = runtime.GetType("C");

                // var o = (object)new C(); var e = (C)o;
                var value = CreateDkmClrValue(typeC.Instantiate());
                var evalResult = FormatResult("(C)o", value);
                Verify(evalResult,
                    EvalResult("(C)o", "{C}", "C", "(C)o", DkmEvaluationResultFlags.Expandable));
                var children = GetChildren(evalResult);
                Verify(children,
                    EvalResult("F", "3", "object {int}", "((C)o).F"));

                // var c = new C(); var e = (C)((object)c);
                value = CreateDkmClrValue(typeC.Instantiate());
                evalResult = FormatResult("(C)((object)c)", value);
                Verify(evalResult,
                    EvalResult("(C)((object)c)", "{C}", "C", "(C)((object)c)", DkmEvaluationResultFlags.Expandable));
                children = GetChildren(evalResult);
                Verify(children,
                    EvalResult("F", "3", "object {int}", "((C)((object)c)).F"));

                // var a = (object)new[] { new C() }; var e = ((C[])o)[0];
                value = CreateDkmClrValue(typeC.Instantiate());
                evalResult = FormatResult("((C[])o)[0]", value);
                Verify(evalResult,
                    EvalResult("((C[])o)[0]", "{C}", "C", "((C[])o)[0]", DkmEvaluationResultFlags.Expandable));
                children = GetChildren(evalResult);
                Verify(children,
                    EvalResult("F", "3", "object {int}", "((C[])o)[0].F"));
            }
        }

        /// <summary>
        /// Get many items synchronously.
        /// </summary>
        [Fact]
        public void ManyItemsSync()
        {
            const int n = 10000;
            var value = CreateDkmClrValue(Enumerable.Range(0, n).ToArray());
            var evalResult = FormatResult("a", value);
            IDkmClrResultProvider resultProvider = new CSharpResultProvider();
            var workList = new DkmWorkList();

            // GetChildren
            var getChildrenResult = default(DkmGetChildrenAsyncResult);
            resultProvider.GetChildren(evalResult, workList, n, DefaultInspectionContext, r => getChildrenResult = r);
            Assert.Equal(workList.Length, 0);
            Assert.Equal(getChildrenResult.InitialChildren.Length, n);

            // GetItems
            var getItemsResult = default(DkmEvaluationEnumAsyncResult);
            resultProvider.GetItems(getChildrenResult.EnumContext, workList, 0, n, r => getItemsResult = r);
            Assert.Equal(workList.Length, 0);
            Assert.Equal(getItemsResult.Items.Length, n);
        }

        /// <summary>
        /// Multiple items, some completed asynchronously.
        /// </summary>
        [Fact]
        public void MultipleItemsAsync()
        {
            var source =
@"using System.Diagnostics;
[DebuggerDisplay(""{F}"")]
class C
{
    object F;
}";
            var runtime = new DkmClrRuntimeInstance(ReflectionUtilities.GetMscorlib(GetAssembly(source)));
            using (runtime.Load())
            {
                int n = 10;
                var type = runtime.GetType("C");
                // C[] with alternating null and non-null values.
                var value = CreateDkmClrValue(Enumerable.Range(0, n).Select(i => (i % 2) == 0 ? type.Instantiate() : null).ToArray());
                var evalResult = FormatResult("a", value);

                IDkmClrResultProvider resultProvider = new CSharpResultProvider();
                var workList = new DkmWorkList();

                // GetChildren
                var getChildrenResult = default(DkmGetChildrenAsyncResult);
                resultProvider.GetChildren(evalResult, workList, n, DefaultInspectionContext, r => getChildrenResult = r);
                Assert.Equal(workList.Length, 1);
                workList.Execute();
                Assert.Equal(getChildrenResult.InitialChildren.Length, n);

                // GetItems
                var getItemsResult = default(DkmEvaluationEnumAsyncResult);
                resultProvider.GetItems(getChildrenResult.EnumContext, workList, 0, n, r => getItemsResult = r);
                Assert.Equal(workList.Length, 1);
                workList.Execute();
                Assert.Equal(getItemsResult.Items.Length, n);
            }
        }

        [Fact]
        public void MultipleItemsAndExceptions()
        {
            var source =
@"using System.Diagnostics;
[DebuggerDisplay(""{P}"")]
class C
{
    public C(int f) { this.f = f; }
    private readonly int f;
    object P
    {
        get
        {
            if (this.f % 4 == 3) throw new System.ArgumentException();
            return this.f;
        }
    }
}";
            var runtime = new DkmClrRuntimeInstance(ReflectionUtilities.GetMscorlib(GetAssembly(source)));
            using (runtime.Load())
            {
                int n = 10;
                int nFailures = 2;
                var type = runtime.GetType("C");
                var value = CreateDkmClrValue(Enumerable.Range(0, n).Select(i => type.Instantiate(i)).ToArray());
                var evalResult = FormatResult("a", value);

                IDkmClrResultProvider resultProvider = new CSharpResultProvider();
                var workList = new DkmWorkList();

                // GetChildren
                var getChildrenResult = default(DkmGetChildrenAsyncResult);
                resultProvider.GetChildren(evalResult, workList, n, DefaultInspectionContext, r => getChildrenResult = r);
                Assert.Equal(workList.Length, 1);
                workList.Execute();
                var items = getChildrenResult.InitialChildren;
                Assert.Equal(items.Length, n);
                Assert.Equal(items.OfType<DkmFailedEvaluationResult>().Count(), nFailures);

                // GetItems
                var getItemsResult = default(DkmEvaluationEnumAsyncResult);
                resultProvider.GetItems(getChildrenResult.EnumContext, workList, 0, n, r => getItemsResult = r);
                Assert.Equal(workList.Length, 1);
                workList.Execute();
                items = getItemsResult.Items;
                Assert.Equal(items.Length, n);
                Assert.Equal(items.OfType<DkmFailedEvaluationResult>().Count(), nFailures);
            }
        }

        [Fact]
        public void NullFormatSpecifiers()
        {
            var value = CreateDkmClrValue(3);
            // With no format specifiers in full name.
            DkmEvaluationResult evalResult = null;
            value.GetResult(
                new DkmWorkList(),
                DeclaredType: value.Type,
                CustomTypeInfo: null,
                InspectionContext: DefaultInspectionContext,
                FormatSpecifiers: null,
                ResultName: "o",
                ResultFullName: "o",
                CompletionRoutine: asyncResult => evalResult = asyncResult.Result);
            Verify(evalResult,
                EvalResult("o", "3", "int", "o"));
            // With format specifiers in full name.
            evalResult = null;
            value.GetResult(
                new DkmWorkList(),
                DeclaredType: value.Type,
                CustomTypeInfo: null,
                InspectionContext: DefaultInspectionContext,
                FormatSpecifiers: null,
                ResultName: "o",
                ResultFullName: "o, nq",
                CompletionRoutine: asyncResult => evalResult = asyncResult.Result);
            Verify(evalResult,
                EvalResult("o", "3", "int", "o, nq"));
        }
    }
}
