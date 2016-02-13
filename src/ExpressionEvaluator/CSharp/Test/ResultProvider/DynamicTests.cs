// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Debugger.Clr;
using Microsoft.VisualStudio.Debugger.Evaluation;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator.UnitTests
{
    public class DynamicTests : CSharpResultProviderTestBase
    {
        [Fact]
        public void Simple()
        {
            var value = CreateDkmClrValue(new object());
            var rootExpr = "d";
            var evalResult = FormatResult(rootExpr, rootExpr, value, declaredType: new DkmClrType((TypeImpl)typeof(object)), declaredTypeInfo: new[] { true });
            Verify(evalResult,
                EvalResult(rootExpr, "{object}", "dynamic {object}", rootExpr));
        }

        [Fact]
        public void Member()
        {
            var source = @"
class C
{
    dynamic F;
    dynamic P { get; set; }
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
                EvalResult("F", "null", "dynamic {object}", "(new C()).F"),
                EvalResult("P", "null", "dynamic {object}", "(new C()).P"));
        }

        [Fact]
        public void Member_ConstructedType()
        {
            var source = @"
class C<T, U>
{
    T Simple;
    U[] Array;
    C<U, T> Constructed;
    C<C<C<object, T>, dynamic[]>, U[]>[] Complex;
}";
            var assembly = GetAssembly(source);
            var typeC = assembly.GetType("C`2");
            var typeC_Constructed1 = typeC.MakeGenericType(typeof(object), typeof(object)); // C<object, dynamic>
            var typeC_Constructed2 = typeC.MakeGenericType(typeof(object), typeC_Constructed1); // C<dynamic, C<object, dynamic>> (i.e. T = dynamic, U = C<object, dynamic>)
            var value = CreateDkmClrValue(Activator.CreateInstance(typeC_Constructed2));
            var rootExpr = "c";
            var evalResult = FormatResult(rootExpr, rootExpr, value, new DkmClrType((TypeImpl)typeC_Constructed2), new[] { false, true, false, false, true });
            Verify(evalResult,
                EvalResult(rootExpr, "{C<object, C<object, object>>}", "C<dynamic, C<object, dynamic>> {C<object, C<object, object>>}", rootExpr, DkmEvaluationResultFlags.Expandable));
            var children = GetChildren(evalResult);
            Verify(children,
                EvalResult("Array", "null", "C<object, dynamic>[] {C<object, object>[]}", "c.Array"),
                EvalResult("Complex", "null", "C<C<C<object, dynamic>, dynamic[]>, C<object, dynamic>[]>[] {C<C<C<object, object>, object[]>, C<object, object>[]>[]}", "c.Complex"),
                EvalResult("Constructed", "null", "C<C<object, dynamic>, dynamic> {C<C<object, object>, object>}", "c.Constructed"),
                EvalResult("Simple", "null", "dynamic {object}", "c.Simple"));
        }

        [Fact]
        public void Member_NestedType()
        {
            var source = @"
class Outer<T>
{
    class Inner<U>
    {
        T Simple;
        U[] Array;
        Outer<U>.Inner<T> Constructed;
    }
}";
            var assembly = GetAssembly(source);
            var typeInner = assembly.GetType("Outer`1+Inner`1");
            var typeInner_Constructed = typeInner.MakeGenericType(typeof(object), typeof(object)); // Outer<dynamic>.Inner<object>
            var value = CreateDkmClrValue(Activator.CreateInstance(typeInner_Constructed));
            var rootExpr = "i";
            var evalResult = FormatResult(rootExpr, rootExpr, value, new DkmClrType((TypeImpl)typeInner_Constructed), new[] { false, true, false });
            Verify(evalResult,
                EvalResult(rootExpr, "{Outer<object>.Inner<object>}", "Outer<dynamic>.Inner<object> {Outer<object>.Inner<object>}", rootExpr, DkmEvaluationResultFlags.Expandable));
            var children = GetChildren(evalResult);
            Verify(children,
                EvalResult("Array", "null", "object[]", "i.Array"),
                EvalResult("Constructed", "null", "Outer<object>.Inner<dynamic> {Outer<object>.Inner<object>}", "i.Constructed"),
                EvalResult("Simple", "null", "dynamic {object}", "i.Simple"));
        }

        [Fact]
        public void Member_ConstructedTypeMember()
        {
            var source = @"
class C<T>
    where T : new()
{
    T Simple = new T();
    T[] Array = new[] { new T() };
    D<T, object, dynamic> Constructed = new D<T, object, dynamic>();
}

class D<T, U, V>
{
    T TT;
    U UU;
    V VV;
}";
            var assembly = GetAssembly(source);
            var typeD = assembly.GetType("D`3");
            var typeD_Constructed = typeD.MakeGenericType(typeof(object), typeof(object), typeof(int)); // D<object, dynamic, int>
            var typeC = assembly.GetType("C`1");
            var typeC_Constructed = typeC.MakeGenericType(typeD_Constructed); // C<D<object, dynamic, int>>
            var value = CreateDkmClrValue(Activator.CreateInstance(typeC_Constructed));
            var rootExpr = "c";
            var evalResult = FormatResult(rootExpr, rootExpr, value, new DkmClrType((TypeImpl)typeC_Constructed), new[] { false, false, false, true, false });
            Verify(evalResult,
                EvalResult(rootExpr, "{C<D<object, object, int>>}", "C<D<object, dynamic, int>> {C<D<object, object, int>>}", rootExpr, DkmEvaluationResultFlags.Expandable));
            var children = GetChildren(evalResult);
            Verify(children,
                EvalResult("Array", "{D<object, object, int>[1]}", "D<object, dynamic, int>[] {D<object, object, int>[]}", "c.Array", DkmEvaluationResultFlags.Expandable),
                EvalResult("Constructed", "{D<D<object, object, int>, object, object>}", "D<D<object, dynamic, int>, object, dynamic> {D<D<object, object, int>, object, object>}", "c.Constructed", DkmEvaluationResultFlags.Expandable),
                EvalResult("Simple", "{D<object, object, int>}", "D<object, dynamic, int> {D<object, object, int>}", "c.Simple", DkmEvaluationResultFlags.Expandable));

            Verify(GetChildren(children[0]),
                EvalResult("[0]", "{D<object, object, int>}", "D<object, dynamic, int> {D<object, object, int>}", "c.Array[0]", DkmEvaluationResultFlags.Expandable));

            Verify(GetChildren(children[1]),
                EvalResult("TT", "null", "D<object, dynamic, int> {D<object, object, int>}", "c.Constructed.TT"),
                EvalResult("UU", "null", "object", "c.Constructed.UU"),
                EvalResult("VV", "null", "dynamic {object}", "c.Constructed.VV"));

            Verify(GetChildren(children[2]),
                EvalResult("TT", "null", "object", "c.Simple.TT"),
                EvalResult("UU", "null", "dynamic {object}", "c.Simple.UU"),
                EvalResult("VV", "0", "int", "c.Simple.VV"));
        }

        [Fact]
        public void Member_ExplicitInterfaceImplementation()
        {
            var source = @"
interface I<V, W>
{
    V P { get; set; }
    W Q { get; set; }
}

class C<T, U> : I<long, T>, I<bool, U>
{
    long I<long, T>.P { get; set; }
    T I<long, T>.Q { get; set; }
    bool I<bool, U>.P { get; set; }
    U I<bool, U>.Q { get; set; }
}";
            var assembly = GetAssembly(source);
            var typeC = assembly.GetType("C`2");
            var typeC_Constructed = typeC.MakeGenericType(typeof(object), typeof(object)); // C<dynamic, object>
            var value = CreateDkmClrValue(Activator.CreateInstance(typeC_Constructed));
            var rootExpr = "c";
            var evalResult = FormatResult(rootExpr, rootExpr, value, new DkmClrType((TypeImpl)typeC_Constructed), new[] { false, true, false });
            Verify(evalResult,
                EvalResult(rootExpr, "{C<object, object>}", "C<dynamic, object> {C<object, object>}", rootExpr, DkmEvaluationResultFlags.Expandable));
            var children = GetChildren(evalResult);
            Verify(children,
                EvalResult("I<bool, object>.P", "false", "bool", "((I<bool, object>)c).P", DkmEvaluationResultFlags.Boolean),
                EvalResult("I<bool, object>.Q", "null", "object", "((I<bool, object>)c).Q"),
                EvalResult("I<long, dynamic>.P", "0", "long", "((I<long, dynamic>)c).P"),
                EvalResult("I<long, dynamic>.Q", "null", "dynamic {object}", "((I<long, dynamic>)c).Q"));
        }

        [Fact]
        public void Member_BaseType()
        {
            var source = @"
class Base<T, U, V, W>
{
    public T P;
    public U Q;
    public V R;
    public W S;
}

class Derived<T, U> : Base<T, U, object, dynamic>
{
    new public T[] P;
    new public U[] Q;
    new public dynamic[] R;
    new public object[] S;
}";
            var assembly = GetAssembly(source);
            var typeDerived = assembly.GetType("Derived`2");
            var typeDerived_Constructed = typeDerived.MakeGenericType(typeof(object), typeof(object)); // Derived<dynamic, object>
            var value = CreateDkmClrValue(Activator.CreateInstance(typeDerived_Constructed));
            var rootExpr = "d";
            var evalResult = FormatResult(rootExpr, rootExpr, value, new DkmClrType((TypeImpl)typeDerived_Constructed), new[] { false, true, false });
            Verify(evalResult,
                EvalResult(rootExpr, "{Derived<object, object>}", "Derived<dynamic, object> {Derived<object, object>}", rootExpr, DkmEvaluationResultFlags.Expandable));

            // CONSIDER: It would be nice to substitute "dynamic" where appropriate.
            var children = GetChildren(evalResult);
            Verify(children,
                EvalResult("P (Base<object, object, object, object>)", "null", "object", "((Base<object, object, object, object>)d).P"),
                EvalResult("P", "null", "dynamic[] {object[]}", "d.P"),
                EvalResult("Q (Base<object, object, object, object>)", "null", "object", "((Base<object, object, object, object>)d).Q"),
                EvalResult("Q", "null", "object[]", "d.Q"),
                EvalResult("R (Base<object, object, object, object>)", "null", "object", "((Base<object, object, object, object>)d).R"),
                EvalResult("R", "null", "dynamic[] {object[]}", "d.R"),
                EvalResult("S (Base<object, object, object, object>)", "null", "object", "((Base<object, object, object, object>)d).S"),
                EvalResult("S", "null", "object[]", "d.S"));
        }

        [Fact]
        public void ArrayElement()
        {
            var value = CreateDkmClrValue(new object[1]);
            var rootExpr = "d";
            var evalResult = FormatResult(rootExpr, rootExpr, value, declaredType: new DkmClrType((TypeImpl)typeof(object[])), declaredTypeInfo: new[] { false, true });
            Verify(evalResult,
                EvalResult(rootExpr, "{object[1]}", "dynamic[] {object[]}", rootExpr, DkmEvaluationResultFlags.Expandable));
            var children = GetChildren(evalResult);
            Verify(children,
                EvalResult("[0]", "null", "dynamic {object}", "d[0]"));
        }

        [Fact]
        public void TypeVariables()
        {
            var intrinsicSource =
@".class private abstract sealed beforefieldinit specialname '<>c__TypeVariables'<T,U,V>
{
  .method public hidebysig specialname rtspecialname instance void .ctor() { ret }
}";
            ImmutableArray<byte> assemblyBytes;
            ImmutableArray<byte> pdbBytes;
            CommonTestBase.EmitILToArray(intrinsicSource, appendDefaultHeader: true, includePdb: false, assemblyBytes: out assemblyBytes, pdbBytes: out pdbBytes);
            var assembly = ReflectionUtilities.Load(assemblyBytes);
            var reflectionType = assembly.GetType(ExpressionCompilerConstants.TypeVariablesClassName).MakeGenericType(new[] { typeof(object), typeof(object), typeof(object[]) });
            var value = CreateDkmClrValue(value: null, type: reflectionType, valueFlags: DkmClrValueFlags.Synthetic);
            var evalResult = FormatResult("typevars", "typevars", value, new DkmClrType((TypeImpl)reflectionType), new[] { false, true, false, false, true });
            Verify(evalResult,
                EvalResult("Type variables", "", "", null, DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Data));
            var children = GetChildren(evalResult);
            Verify(children,
                EvalResult("T", "dynamic", "dynamic", null, DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Data),
                EvalResult("U", "object", "object", null, DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Data),
                EvalResult("V", "dynamic[]", "dynamic[]", null, DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Data));
        }
    }
}