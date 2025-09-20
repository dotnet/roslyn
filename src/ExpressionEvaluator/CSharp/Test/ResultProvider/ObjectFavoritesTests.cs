// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.VisualStudio.Debugger.Clr;
using Microsoft.VisualStudio.Debugger.Evaluation;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator.UnitTests
{
    public class ObjectFavoritesTests : CSharpResultProviderTestBase
    {
        [Fact]
        public void Expansion()
        {
            var source =
@"class A
{
    string s1 = ""S1"";
    string s2 = ""S2"";
}
class B : A
{
    string s3 = ""S3"";
    string s4 = ""S4"";
}
class C
{
    A a = new A();
    B b = new B();
}";

            var assembly = GetAssembly(source);
            var type = assembly.GetType("C");
            var rootExpr = "new C()";

            var favoritesByTypeName = new Dictionary<string, DkmClrObjectFavoritesInfo>()
            {
                { "C", new DkmClrObjectFavoritesInfo(new[] { "b" }) },
                { "B", new DkmClrObjectFavoritesInfo(new[] { "s4", "s2" }) }
            };

            var runtime = new DkmClrRuntimeInstance(ReflectionUtilities.GetMscorlib(assembly), favoritesByTypeName);

            var value = CreateDkmClrValue(
                value: Activator.CreateInstance(type),
                type: runtime.GetType((TypeImpl)type));

            var evalResult = FormatResult(rootExpr, value);
            Verify(evalResult,
                EvalResult(rootExpr, "{C}", "C", rootExpr, DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.HasFavorites));
            var children = GetChildren(evalResult);
            Verify(children,
                EvalResult("b", "{B}", "B", "(new C()).b", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.CanFavorite | DkmEvaluationResultFlags.IsFavorite | DkmEvaluationResultFlags.HasFavorites),
                EvalResult("a", "{A}", "A", "(new C()).a", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.CanFavorite));

            // B b = new B();
            var more = GetChildren(children[0]);
            Verify(more,
                EvalResult("s4", @"""S4""", "string", "(new C()).b.s4", DkmEvaluationResultFlags.RawString | DkmEvaluationResultFlags.CanFavorite | DkmEvaluationResultFlags.IsFavorite, editableValue: @"""S4"""),
                EvalResult("s2", @"""S2""", "string", "(new C()).b.s2", DkmEvaluationResultFlags.RawString | DkmEvaluationResultFlags.CanFavorite | DkmEvaluationResultFlags.IsFavorite, editableValue: @"""S2"""),
                EvalResult("s1", @"""S1""", "string", "(new C()).b.s1", DkmEvaluationResultFlags.RawString | DkmEvaluationResultFlags.CanFavorite, editableValue: @"""S1"""),
                EvalResult("s3", @"""S3""", "string", "(new C()).b.s3", DkmEvaluationResultFlags.RawString | DkmEvaluationResultFlags.CanFavorite, editableValue: @"""S3"""));

            // A a = new A();
            more = GetChildren(children[1]);
            Verify(more,
                EvalResult("s1", @"""S1""", "string", "(new C()).a.s1", DkmEvaluationResultFlags.RawString | DkmEvaluationResultFlags.CanFavorite, editableValue: @"""S1"""),
                EvalResult("s2", @"""S2""", "string", "(new C()).a.s2", DkmEvaluationResultFlags.RawString | DkmEvaluationResultFlags.CanFavorite, editableValue: @"""S2"""));
        }

        [Fact]
        public void ExpansionOfNullValue()
        {
            var source =
@"class A
{
    string s1 = ""S1"";
    string s2 = ""S2"";
}
class B
{
    A a1 = new A();
    A a2 = null;
}";

            var assembly = GetAssembly(source);
            var type = assembly.GetType("B");
            var rootExpr = "new B()";

            var favoritesByTypeName = new Dictionary<string, DkmClrObjectFavoritesInfo>()
            {
                { "A", new DkmClrObjectFavoritesInfo(new[] { "s2" }) }
            };

            var runtime = new DkmClrRuntimeInstance(ReflectionUtilities.GetMscorlib(assembly), favoritesByTypeName);

            var value = CreateDkmClrValue(
                value: Activator.CreateInstance(type),
                type: runtime.GetType((TypeImpl)type));

            var evalResult = FormatResult(rootExpr, value);
            Verify(evalResult,
                EvalResult(rootExpr, "{B}", "B", rootExpr, DkmEvaluationResultFlags.Expandable));
            var children = GetChildren(evalResult);
            Verify(children,
                EvalResult("a1", "{A}", "A", "(new B()).a1", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.CanFavorite | DkmEvaluationResultFlags.HasFavorites),
                EvalResult("a2", "null", "A", "(new B()).a2", DkmEvaluationResultFlags.CanFavorite));

            // A a1 = new A();
            var more = GetChildren(children[0]);
            Verify(more,
                EvalResult("s2", @"""S2""", "string", "(new B()).a1.s2", DkmEvaluationResultFlags.RawString | DkmEvaluationResultFlags.CanFavorite | DkmEvaluationResultFlags.IsFavorite, editableValue: @"""S2"""),
                EvalResult("s1", @"""S1""", "string", "(new B()).a1.s1", DkmEvaluationResultFlags.RawString | DkmEvaluationResultFlags.CanFavorite, editableValue: @"""S1"""));
        }

        [Fact]
        public void FilteredExpansion()
        {
            var source =
@"class A
{
    string s1 = ""S1"";
    string s2 = ""S2"";
}
class B : A
{
    string s3 = ""S3"";
    string s4 = ""S4"";
}
class C
{
    A a = new A();
    B b = new B();
}";

            var assembly = GetAssembly(source);
            var type = assembly.GetType("C");
            var rootExpr = "new C()";

            var favoritesByTypeName = new Dictionary<string, DkmClrObjectFavoritesInfo>()
            {
                { "C", new DkmClrObjectFavoritesInfo(new[] { "b" }) },
                { "B", new DkmClrObjectFavoritesInfo(new[] { "s4", "s2" }) }
            };

            var runtime = new DkmClrRuntimeInstance(ReflectionUtilities.GetMscorlib(assembly), favoritesByTypeName);

            var value = CreateDkmClrValue(
                value: Activator.CreateInstance(type),
                type: runtime.GetType((TypeImpl)type));

            var evalResult = FormatResult(rootExpr, value, null, CreateDkmInspectionContext(DkmEvaluationFlags.FilterToFavorites));
            Verify(evalResult,
                EvalResult(rootExpr, "{C}", "C", rootExpr, DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.HasFavorites));
            var children = GetChildren(evalResult, CreateDkmInspectionContext(DkmEvaluationFlags.FilterToFavorites));
            Verify(children,
                EvalResult("b", "{B}", "B", "(new C()).b", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.CanFavorite | DkmEvaluationResultFlags.IsFavorite | DkmEvaluationResultFlags.HasFavorites));

            // B b = new B();
            var more = GetChildren(children[0], CreateDkmInspectionContext(DkmEvaluationFlags.FilterToFavorites));
            Verify(more,
                EvalResult("s4", @"""S4""", "string", "(new C()).b.s4", DkmEvaluationResultFlags.RawString | DkmEvaluationResultFlags.CanFavorite | DkmEvaluationResultFlags.IsFavorite, editableValue: @"""S4"""),
                EvalResult("s2", @"""S2""", "string", "(new C()).b.s2", DkmEvaluationResultFlags.RawString | DkmEvaluationResultFlags.CanFavorite | DkmEvaluationResultFlags.IsFavorite, editableValue: @"""S2"""));
        }

        [Fact]
        public void DisplayString()
        {
            var source =
@"class A
{
    string s1 = ""S1"";
    string s2 = ""S2"";
    string s3 = ""S3"";
    string s4 = ""S4"";
}";

            var assembly = GetAssembly(source);
            var type = assembly.GetType("A");
            var rootExpr = "new A()";

            var favoritesByTypeName = new Dictionary<string, DkmClrObjectFavoritesInfo>()
            {
                { "A", new DkmClrObjectFavoritesInfo(new[] { "s4", "s2" }, "s4 = {s4}, s2 = {s2}") }
            };

            var runtime = new DkmClrRuntimeInstance(ReflectionUtilities.GetMscorlib(assembly), favoritesByTypeName);

            var value = CreateDkmClrValue(
                value: Activator.CreateInstance(type),
                type: runtime.GetType((TypeImpl)type));

            var evalResult = FormatResult(rootExpr, value);
            Verify(evalResult,
                EvalResult(rootExpr, @"s4 = ""S4"", s2 = ""S2""", "A", rootExpr, DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.HasFavorites));
        }

        [Fact]
        public void SimpleDisplayString()
        {
            var source =
@"class A
{
    string s1 = ""S1"";
    string s2 = ""S2"";
    string s3 = ""S3"";
    string s4 = ""S4"";
}";

            var assembly = GetAssembly(source);
            var type = assembly.GetType("A");
            var rootExpr = "new A()";

            var favoritesByTypeName = new Dictionary<string, DkmClrObjectFavoritesInfo>()
            {
                { "A", new DkmClrObjectFavoritesInfo(new[] { "s4", "s2" }, "s4 = {s4}, s2 = {s2}", "{s4}, {s2}") }
            };

            var runtime = new DkmClrRuntimeInstance(ReflectionUtilities.GetMscorlib(assembly), favoritesByTypeName);

            var value = CreateDkmClrValue(
                value: Activator.CreateInstance(type),
                type: runtime.GetType((TypeImpl)type));

            var evalResult = FormatResult(rootExpr, value, null, CreateDkmInspectionContext(DkmEvaluationFlags.UseSimpleDisplayString));
            Verify(evalResult,
                EvalResult(rootExpr, @"""S4"", ""S2""", "A", rootExpr, DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.HasFavorites));
        }

        [Fact]
        public void Nullable()
        {
            var source =
@"struct A
{
    public string s1;
    public string s2;

    public A(string s1, string s2)
    {
        this.s1 = s1;
        this.s2 = s2;
    }
}
class B 
{
    A? a1 = null;
    A? a2 = new A(""S1"", ""S2"");
}";

            var assembly = GetAssembly(source);
            var type = assembly.GetType("B");
            var rootExpr = "new B()";

            var favoritesByTypeName = new Dictionary<string, DkmClrObjectFavoritesInfo>()
            {
                { "B", new DkmClrObjectFavoritesInfo(new[] { "a2" }) },
                { "A", new DkmClrObjectFavoritesInfo(new[] { "s2" }) }
            };

            var runtime = new DkmClrRuntimeInstance(ReflectionUtilities.GetMscorlib(assembly), favoritesByTypeName);

            var value = CreateDkmClrValue(
                value: Activator.CreateInstance(type),
                type: runtime.GetType((TypeImpl)type));

            var evalResult = FormatResult(rootExpr, value);
            Verify(evalResult,
                EvalResult(rootExpr, "{B}", "B", rootExpr, DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.HasFavorites));
            var children = GetChildren(evalResult);
            Verify(children,
                EvalResult("a2", "{A}", "A?", "(new B()).a2", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.CanFavorite | DkmEvaluationResultFlags.IsFavorite | DkmEvaluationResultFlags.HasFavorites),
                EvalResult("a1", "null", "A?", "(new B()).a1", DkmEvaluationResultFlags.CanFavorite));

            // A? a2 = new A();
            var more = GetChildren(children[0]);
            Verify(more,
                EvalResult("s2", @"""S2""", "string", "(new B()).a2.s2", DkmEvaluationResultFlags.RawString | DkmEvaluationResultFlags.CanFavorite | DkmEvaluationResultFlags.IsFavorite, editableValue: @"""S2"""),
                EvalResult("s1", @"""S1""", "string", "(new B()).a2.s1", DkmEvaluationResultFlags.RawString | DkmEvaluationResultFlags.CanFavorite, editableValue: @"""S1"""));
        }

        [Fact]
        public void DuplicateNames()
        {
            var source =
@"class A
{
    public string S1 { get; }
    public string S2 { get; }
}
class B : A
{
    public new string S1 { get; }
    public string S3 { get; }
}";

            var assembly = GetAssembly(source);
            var type = assembly.GetType("B");
            var rootExpr = "new B()";

            var favoritesByTypeName = new Dictionary<string, DkmClrObjectFavoritesInfo>()
            {
                { "B", new DkmClrObjectFavoritesInfo(new[] { "S1" }) },
                { "A", new DkmClrObjectFavoritesInfo(new[] { "S2" }) }
            };

            var runtime = new DkmClrRuntimeInstance(ReflectionUtilities.GetMscorlib(assembly), favoritesByTypeName);

            var value = CreateDkmClrValue(
                value: Activator.CreateInstance(type),
                type: runtime.GetType((TypeImpl)type));

            var evalResult = FormatResult(rootExpr, value);
            Verify(evalResult,
                EvalResult(rootExpr, "{B}", "B", rootExpr, DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.HasFavorites));
            var children = GetChildren(evalResult);
            Verify(children,
                EvalResult("S1", @"null", "string", "(new B()).S1", DkmEvaluationResultFlags.ReadOnly | DkmEvaluationResultFlags.CanFavorite | DkmEvaluationResultFlags.IsFavorite),
                EvalResult("S1 (A)", @"null", "string", "((A)(new B())).S1", DkmEvaluationResultFlags.ReadOnly), /* Inherited and hidden does not currently support favorites */
                EvalResult("S2", @"null", "string", "(new B()).S2", DkmEvaluationResultFlags.ReadOnly | DkmEvaluationResultFlags.CanFavorite),
                EvalResult("S3", @"null", "string", "(new B()).S3", DkmEvaluationResultFlags.ReadOnly | DkmEvaluationResultFlags.CanFavorite));
        }
    }
}
