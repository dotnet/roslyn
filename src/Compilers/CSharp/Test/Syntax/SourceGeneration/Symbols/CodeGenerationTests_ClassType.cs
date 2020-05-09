// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.SourceGeneration;
using Roslyn.Test.Utilities;
using Xunit;
using static Microsoft.CodeAnalysis.SourceGeneration.CodeGenerator;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.SourceGeneration
{
    public partial class CodeGenerationTests
    {
        [Fact]
        public void TestClassType1()
        {
            AssertEx.AreEqual(
"x",
Class("x").GenerateTypeString());
        }

        [Fact]
        public void TestClassTypeWithTypeArguments1()
        {
            AssertEx.AreEqual(
"X<int>",
Class(
    "X",
    typeArguments: ImmutableArray.Create(Int32)).GenerateTypeString());
        }

        [Fact]
        public void TestClassTypeWithTypeArguments2()
        {
            AssertEx.AreEqual(
"X<int, bool>",
Class(
    "X",
    typeArguments: ImmutableArray.Create(Int32, Boolean)).GenerateTypeString());
        }

        [Fact]
        public void TestClassTypeInNamespace1()
        {
            AssertEx.AreEqual(
"NS.x",
Class(
    "x",
    containingSymbol: Namespace("NS")).GenerateTypeString());
        }

        [Fact]
        public void TestClassTypeInGlobalNamespace1()
        {
            AssertEx.AreEqual(
"global::x",
Class(
    "x",
    containingSymbol: GlobalNamespace()).GenerateTypeString());
        }

        [Fact]
        public void TestClassTypeInNamespaceInGlobal1()
        {
            AssertEx.AreEqual(
"global::NS.x",
Class(
    "x",
    containingSymbol: Namespace(
        "NS",
        containingSymbol: GlobalNamespace())).GenerateTypeString());
        }

        [Fact]
        public void TestClassDeclaration1()
        {
            AssertEx.AreEqual(
@"class X
{
}",
Class("X").GenerateString());
        }

        [Fact]
        public void TestClassDeclarationWithField1()
        {
            AssertEx.AreEqual(
@"class X
{
    int i;
}",
Class(
    "X",
    members: ImmutableArray.Create<ISymbol>(Field(Int32, "i"))).GenerateString());
        }

        [Fact]
        public void TestClassDeclarationWithNestedClass1()
        {
            AssertEx.AreEqual(
@"class X
{
    class Y
    {
    }
}",
Class(
    "X",
    members: ImmutableArray.Create<ISymbol>(Class("Y"))).GenerateString());
        }

        [Fact]
        public void TestClassWithBaseType()
        {
            AssertEx.AreEqual(
@"class X : Y
{
}",
Class(
    "X",
    baseType: Class("Y")).GenerateString());
        }

        [Fact]
        public void TestClassWithObjectType()
        {
            AssertEx.AreEqual(
@"class X
{
}",
Class(
    "X",
    baseType: System_Object).GenerateString());
        }

        [Fact]
        public void TestClassWithInterfaces()
        {
            AssertEx.AreEqual(
@"class X : Y, Z
{
}",
Class(
    "X",
    interfaces: ImmutableArray.Create(Interface("Y"), Interface("Z"))).GenerateString());
        }

        [Fact]
        public void TestGenericClass()
        {
            AssertEx.AreEqual(
@"class X<Y>
{
}",
Class(
    "X",
    typeArguments: ImmutableArray.Create<ITypeSymbol>(TypeParameter("Y"))).GenerateString());
        }

        [Fact]
        public void TestGenericClassWithInVariance()
        {
            AssertEx.AreEqual(
@"class X<in Y>
{
}",
Class(
    "X",
    typeArguments: ImmutableArray.Create<ITypeSymbol>(
        TypeParameter(
            "Y",
            variance: VarianceKind.In))).GenerateString());
        }

        [Fact]
        public void TestGenericClassWithOutVariance()
        {
            AssertEx.AreEqual(
@"class X<out Y>
{
}",
Class(
    "X",
    typeArguments: ImmutableArray.Create<ITypeSymbol>(
        TypeParameter(
            "Y",
            variance: VarianceKind.Out))).GenerateString());
        }

        [Fact]
        public void TestGenericClassWithConstructorConstraint()
        {
            AssertEx.AreEqual(
@"class X<Y>
    where Y : new()
{
}",
Class(
    "X",
    typeArguments: ImmutableArray.Create<ITypeSymbol>(
        TypeParameter(
            "Y",
            hasConstructorConstraint: true))).GenerateString());
        }

        [Fact]
        public void TestGenericClassWithNotNullConstraint()
        {
            AssertEx.AreEqual(
@"class X<Y>
    where Y : notnull
{
}",
Class(
    "X",
    typeArguments: ImmutableArray.Create<ITypeSymbol>(
        TypeParameter(
            "Y",
            hasNotNullConstraint: true))).GenerateString());
        }

        [Fact]
        public void TestGenericClassWithReferenceTypeConstraint()
        {
            AssertEx.AreEqual(
@"class X<Y>
    where Y : class
{
}",
Class(
    "X",
    typeArguments: ImmutableArray.Create<ITypeSymbol>(
        TypeParameter(
            "Y",
            hasReferenceTypeConstraint: true))).GenerateString());
        }

        [Fact]
        public void TestGenericClassWithUnmanagedConstraint()
        {
            AssertEx.AreEqual(
@"class X<Y>
    where Y : unmanaged
{
}",
Class(
    "X",
    typeArguments: ImmutableArray.Create<ITypeSymbol>(
        TypeParameter(
            "Y",
            hasUnmanagedTypeConstraint: true))).GenerateString());
        }

        [Fact]
        public void TestGenericClassWithValueConstraint()
        {
            AssertEx.AreEqual(
@"class X<Y>
    where Y : struct
{
}",
Class(
    "X",
    typeArguments: ImmutableArray.Create<ITypeSymbol>(
        TypeParameter(
            "Y",
            hasValueTypeConstraint: true))).GenerateString());
        }

        [Fact]
        public void TestGenericClassWithTypeConstraint()
        {
            AssertEx.AreEqual(
@"class X<Y>
    where Y : Z
{
}",
Class(
    "X",
    typeArguments: ImmutableArray.Create<ITypeSymbol>(
        TypeParameter(
            "Y",
            constraintTypes: ImmutableArray.Create<ITypeSymbol>(
                Class("Z"))))).GenerateString());
        }
    }
}
