// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using DiffPlex.Model;
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
    }
}
