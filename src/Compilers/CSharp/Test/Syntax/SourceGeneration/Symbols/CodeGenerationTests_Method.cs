// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.SourceGeneration;
using Microsoft.CodeAnalysis.SourceGeneration;
using Roslyn.Test.Utilities;
using Xunit;
using static Microsoft.CodeAnalysis.SourceGeneration.CodeGenerator;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.SourceGeneration
{
    public partial class CodeGenerationTests
    {
        [Fact]
        public void TestMethod1()
        {
            AssertEx.AreEqual(
@"class C
{
    void M();
}",
Class("C").WithMembers(Method(Void, "M")).GenerateString());
        }

        [Fact]
        public void TestMethodWithAccessibility1()
        {
            AssertEx.AreEqual(
@"class C
{
    public void M();
}",
Class("C").WithMembers(
    Method(
        Void,
        "M",
        declaredAccessibility: Accessibility.Public)).GenerateString());
        }

        [Fact]
        public void TestMethodWithModifiers1()
        {
            AssertEx.AreEqual(
@"class C
{
    unsafe void M();
}",
Class("C").WithMembers(
    Method(
        Void,
        "M",
        modifiers: SymbolModifiers.Unsafe)).GenerateString());
        }

        [Fact]
        public void TestGenericMethod1()
        {
            AssertEx.AreEqual(
@"class C
{
    void M<X>();
}",
Class("C").WithMembers(
    Method(Void, "M").WithTypeArguments(
        TypeParameter("X"))).GenerateString());
        }

        [Fact]
        public void TestGenericMethodWithConstraint1()
        {
            AssertEx.AreEqual(
@"class C
{
    void M<X>()
        where X : new();
}",
Class("C").WithMembers(
    Method(Void, "M").WithTypeArguments(
        TypeParameter("X", constructorConstraint: true))).GenerateString());
        }

        [Fact]
        public void TestMethodWithParameters1()
        {
            AssertEx.AreEqual(
@"class C
{
    void M(int i);
}",
Class("C").WithMembers(
    Method(Void, "M").WithParameters(
        Parameter(Int32, "i"))).GenerateString());
        }

        [Fact]
        public void TestMethodWithParamsParameters1()
        {
            AssertEx.AreEqual(
@"class C
{
    void M(params int i);
}",
Class("C").WithMembers(
    Method(Void, "M").WithParameters(
        Parameter(Int32, "i", modifiers: SymbolModifiers.Params))).GenerateString());
        }

        [Fact]
        public void TestMethodWithExplicitImpl1()
        {
            AssertEx.AreEqual(
@"class C
{
    void I.M();
}",
Class("C").WithMembers(
    Method(Void, "M").WithExplicitInterfaceImplementations(
        Method(Void, "M",
            containingSymbol: Interface("I")))).GenerateString());
        }

        [Fact]
        public void TestMethodWithPrivateExplicitImpl1()
        {
            AssertEx.AreEqual(
@"class C
{
    void I.M();
}",
Class("C").WithMembers(
    Method(Void, "M").WithExplicitInterfaceImplementations(
        Method(Void, "M",
            declaredAccessibility: Accessibility.Private,
            containingSymbol: Interface("I")))).GenerateString());
        }
    }
}
