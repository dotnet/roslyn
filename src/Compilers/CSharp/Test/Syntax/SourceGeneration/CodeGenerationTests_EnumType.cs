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
        public void TestEnumType1()
        {
            AssertEx.AreEqual(
"x",
Enum("x").GenerateTypeString());
        }

        [Fact]
        public void TestEnumTypeInNamespace1()
        {
            AssertEx.AreEqual(
"NS.x",
Enum(
    "x",
    containingSymbol: Namespace("NS")).GenerateTypeString());
        }

        [Fact]
        public void TestEnumTypeInGlobalNamespace1()
        {
            AssertEx.AreEqual(
"global::x",
Enum(
    "x",
    containingSymbol: GlobalNamespace()).GenerateTypeString());
        }

        [Fact]
        public void TestEnumTypeInNamespaceInGlobal1()
        {
            AssertEx.AreEqual(
"global::NS.x",
Enum(
    "x",
    containingSymbol: Namespace(
        "NS",
        containingSymbol: GlobalNamespace())).GenerateTypeString());
        }

        [Fact]
        public void TestEnumDeclaration1()
        {
            AssertEx.AreEqual(
@"enum X
{
}",
Enum("X").GenerateString());
        }

        [Fact]
        public void TestPublicEnumDeclaration1()
        {
            AssertEx.AreEqual(
@"public enum X
{
}",
Enum(
    "X",
    declaredAccessibility: Accessibility.Public).GenerateString());
        }

        [Fact]
        public void TestEnumDeclarationInNamespace1()
        {
            AssertEx.AreEqual(
@"namespace N
{
    enum X
    {
    }
}",
Namespace(
    "N",
    members: ImmutableArray.Create<INamespaceOrTypeSymbol>(
        Enum("X"))).GenerateString());
        }

        [Fact]
        public void TestEnumDeclarationWithBaseType1()
        {
            AssertEx.AreEqual(
@"enum X : int
{
}",
Enum(
    "X",
    baseType: (INamedTypeSymbol)Int32).GenerateString());
        }

        [Fact]
        public void TestEnumDeclarationWithOneMember1()
        {
            AssertEx.AreEqual(
@"enum X
{
    A
}",
Enum(
    "X",
    members: ImmutableArray.Create<ISymbol>(
        EnumMember("A"))).GenerateString());
        }

        [Fact]
        public void TestEnumDeclarationWithTwoMembers1()
        {
            AssertEx.AreEqual(
@"enum X
{
    A,
    B
}",
Enum(
    "X",
    members: ImmutableArray.Create<ISymbol>(
        EnumMember("A"),
        EnumMember("B"))).GenerateString());
        }
    }
}
