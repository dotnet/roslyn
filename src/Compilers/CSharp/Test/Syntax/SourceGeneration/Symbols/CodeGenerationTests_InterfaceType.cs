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
        public void TestInterfaceType1()
        {
            AssertEx.AreEqual(
"x",
Interface("x").GenerateTypeString());
        }

        [Fact]
        public void TestInterfaceTypeWithTypeArguments1()
        {
            AssertEx.AreEqual(
"X<int>",
Interface("X").WithTypeArguments(Int32).GenerateTypeString());
        }

        [Fact]
        public void TestInterfaceTypeWithTypeArguments2()
        {
            AssertEx.AreEqual(
"X<int, bool>",
Interface("X").WithTypeArguments(Int32, Boolean).GenerateTypeString());
        }

        [Fact]
        public void TestInterfaceTypeInNamespace1()
        {
            AssertEx.AreEqual(
"NS.x",
Interface(
    "x",
    containingSymbol: Namespace("NS")).GenerateTypeString());
        }

        [Fact]
        public void TestInterfaceTypeInGlobalNamespace1()
        {
            AssertEx.AreEqual(
"global::x",
Interface(
    "x",
    containingSymbol: GlobalNamespace()).GenerateTypeString());
        }

        [Fact]
        public void TestInterfaceTypeInNamespaceInGlobal1()
        {
            AssertEx.AreEqual(
"global::NS.x",
Interface(
    "x",
    containingSymbol: Namespace(
        "NS",
        containingSymbol: GlobalNamespace())).GenerateTypeString());
        }

        [Fact]
        public void TestInterfaceDeclaration1()
        {
            AssertEx.AreEqual(
@"interface X
{
}",
Interface("X").GenerateString());
        }

        [Fact]
        public void TestAbstractInterfaceDeclaration1()
        {
            AssertEx.AreEqual(
@"interface X
{
}",
Interface(
    "X",
    modifiers: SymbolModifiers.Abstract).GenerateString());
        }

        [Fact]
        public void TestInterfaceDeclarationWithMethod1()
        {
            AssertEx.AreEqual(
@"interface X
{
    int M();
}",
Interface("X").WithMembers(Method(Int32, "M")).GenerateString());
        }

        [Fact]
        public void TestInterfaceDeclarationWithAbstractMethod1()
        {
            AssertEx.AreEqual(
@"interface X
{
    int M();
}",
Interface("X").WithMembers(
    Method(
        Int32,
        "M",
        modifiers: SymbolModifiers.Abstract)).GenerateString());
        }

        [Fact]
        public void TestInterfaceDeclarationWithPublicMethod1()
        {
            AssertEx.AreEqual(
@"interface X
{
    int M();
}",
Interface("X").WithMembers(
    Method(
        Int32,
        "M",
        declaredAccessibility: Accessibility.Public)).GenerateString());
        }

        [Fact]
        public void TestInterfaceDeclarationWithProtectedMethod1()
        {
            AssertEx.AreEqual(
@"interface X
{
    protected int M();
}",
Interface("X").WithMembers(
    Method(
        Int32,
        "M",
        declaredAccessibility: Accessibility.Protected)).GenerateString());
        }

        [Fact]
        public void TestInterfaceDeclarationWithNestedInterface1()
        {
            AssertEx.AreEqual(
@"interface X
{
    interface Y
    {
    }
}",
Interface("X").WithMembers(Interface("Y")).GenerateString());
        }

        [Fact]
        public void TestInterfaceWithInterfaces()
        {
            AssertEx.AreEqual(
@"interface X : Y, Z
{
}",
Interface("X").WithInterfaces(Interface("Y"), Interface("Z")).GenerateString());
        }

        [Fact]
        public void TestGenericInterface()
        {
            AssertEx.AreEqual(
@"interface X<Y>
{
}",
Interface("X").WithTypeArguments(TypeParameter("Y")).GenerateString());
        }

        [Fact]
        public void TestGenericInterfaceWithInVariance()
        {
            AssertEx.AreEqual(
@"interface X<in Y>
{
}",
Interface("X").WithTypeArguments(
    TypeParameter(
        "Y",
        variance: VarianceKind.In)).GenerateString());
        }

        [Fact]
        public void TestGenericInterfaceWithOutVariance()
        {
            AssertEx.AreEqual(
@"interface X<out Y>
{
}",
Interface("X").WithTypeArguments(
    TypeParameter(
        "Y",
        variance: VarianceKind.Out)).GenerateString());
        }
    }
}
