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
        public void TestStructType1()
        {
            AssertEx.AreEqual(
"x",
Struct("x").GenerateTypeString());
        }

        [Fact]
        public void TestStructTypeInNamespace1()
        {
            AssertEx.AreEqual(
"NS.x",
Struct(
    "x",
    containingSymbol: Namespace("NS")).GenerateTypeString());
        }

        [Fact]
        public void TestStructTypeInGlobalNamespace1()
        {
            AssertEx.AreEqual(
"global::x",
Struct(
    "x",
    containingSymbol: GlobalNamespace()).GenerateTypeString());
        }

        [Fact]
        public void TestStructTypeInNamespaceInGlobal1()
        {
            AssertEx.AreEqual(
"global::NS.x",
Struct(
    "x",
    containingSymbol: Namespace(
        "NS",
        containingSymbol: GlobalNamespace())).GenerateTypeString());
        }

        [Fact]
        public void TestStructDeclaration1()
        {
            AssertEx.AreEqual(
@"struct X
{
}",
Struct("X").GenerateString());
        }

        [Fact]
        public void TestPublicStructDeclaration1()
        {
            AssertEx.AreEqual(
@"public struct X
{
}",
Struct(
    "X",
    declaredAccessibility: Accessibility.Public).GenerateString());
        }

        [Fact]
        public void TestSealedStructDeclaration1()
        {
            AssertEx.AreEqual(
@"struct X
{
}",
Struct(
    "X",
    modifiers: SymbolModifiers.Sealed).GenerateString());
        }

        [Fact]
        public void TestStructDeclarationInNamespace1()
        {
            AssertEx.AreEqual(
@"namespace N
{
    struct X
    {
    }
}",
Namespace(
    "N",
    members: ImmutableArray.Create<INamespaceOrTypeSymbol>(
        Struct("X"))).GenerateString());
        }

        [Fact]
        public void TestStructDeclarationWithOneMember1()
        {
            AssertEx.AreEqual(
@"struct X
{
    int A;
}",
Struct(
    "X",
    members: ImmutableArray.Create<ISymbol>(
        Field(Int32, "A"))).GenerateString());
        }

        [Fact]
        public void TestStructDeclarationWithTwoMembers1()
        {
            AssertEx.AreEqual(
@"struct X
{
    int A;
    bool B;
}",
Struct(
    "X",
    members: ImmutableArray.Create<ISymbol>(
        Field(Int32, "A"),
        Field(Boolean, "B"))).GenerateString());
        }
    }
}
