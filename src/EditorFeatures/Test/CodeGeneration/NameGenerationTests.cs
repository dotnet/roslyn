// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.CodeGeneration;

[Trait(Traits.Feature, Traits.Features.CodeGeneration)]
public class NameGenerationTests : AbstractCodeGenerationTests
{
    [Fact]
    public void TestIdentifierName()
    {
        Test(
            f => f.IdentifierName("a"),
            cs: "a",
            csSimple: "a",
            vb: "a",
            vbSimple: "a");
    }

    [Fact]
    public void TestIdentifierNameCSharpKeyword()
    {
        Test(
            f => f.IdentifierName("int"),
            cs: "@int",
            csSimple: "@int",
            vb: "int",
            vbSimple: "int");
    }

    [Fact]
    public void TestIdentifierNameVisualBasicKeyword()
    {
        Test(
            f => f.IdentifierName("Integer"),
            cs: "Integer",
            csSimple: "Integer",
            vb: "[Integer]",
            vbSimple: "[Integer]");
    }

    [Fact]
    public void TestGenericName1()
    {
        Test(
            f => f.GenericName("Outer", CreateClass("Inner1")),
            cs: "Outer<Inner1>",
            csSimple: null,
            vb: "Outer(Of Inner1)",
            vbSimple: null);
    }

    [Fact]
    public void TestGenericName2()
    {
        Test(
            f => f.GenericName("Outer", CreateClass("Inner1"), CreateClass("Inner2")),
            cs: "Outer<Inner1, Inner2>",
            csSimple: null,
            vb: "Outer(Of Inner1, Inner2)",
            vbSimple: null);
    }

    [Fact]
    public void TestGenericNameCSharpKeyword()
    {
        Test(
            f => f.GenericName("int", CreateClass("string"), CreateClass("bool")),
            cs: "@int<@string, @bool>",
            csSimple: null,
            vb: "int(Of [string], bool)",
            vbSimple: null);
    }

    [Fact]
    public void TestGenericNameVisualBasicKeyword()
    {
        Test(
            f => f.GenericName("Integer", CreateClass("String"), CreateClass("Boolean")),
            cs: "Integer<String, Boolean>",
            csSimple: null,
            vb: "[Integer](Of [String], [Boolean])",
            vbSimple: null);
    }

    [Fact]
    public void TestQualifiedName1()
    {
        Test(
            f => f.QualifiedName(f.IdentifierName("Outer"), f.IdentifierName("Inner1")),
            cs: "Outer.Inner1",
            csSimple: "Outer.Inner1",
            vb: "Outer.Inner1",
            vbSimple: "Outer.Inner1");
    }

    [Fact]
    public void TestQualifiedNameCSharpKeywords1()
    {
        Test(
            f => f.QualifiedName(f.IdentifierName("int"), f.IdentifierName("string")),
            cs: "@int.@string",
            csSimple: "@int.@string",
            vb: "int.[string]",
            vbSimple: "int.string");
    }

    [Fact]
    public void TestQualifiedNameVBKeywords1()
    {
        Test(
            f => f.QualifiedName(f.IdentifierName("Integer"), f.IdentifierName("String")),
            cs: "Integer.String",
            csSimple: "Integer.String",
            vb: "[Integer].[String]",
            vbSimple: "Integer.String");
    }

    [Fact]
    public void TestQualifiedGenericName1()
    {
        Test(
            f => f.QualifiedName(
                f.IdentifierName("One"),
                f.GenericName("Outer",
                    CreateClass("Inner1"),
                    CreateClass("Inner2"))),
            cs: "One.Outer<Inner1, Inner2>",
            csSimple: "One.Outer<Inner1, Inner2>",
            vb: "One.Outer(Of Inner1, Inner2)",
            vbSimple: "One.Outer(Of Inner1, Inner2)");
    }

    [Fact]
    public void TestQualifiedGenericName2()
    {
        Test(
            f => f.QualifiedName(
                f.GenericName("Outer",
                    CreateClass("Inner1"),
                    CreateClass("Inner2")),
                f.IdentifierName("One")),
            cs: "Outer<Inner1, Inner2>.One",
            csSimple: "Outer<Inner1, Inner2>.One",
            vb: "Outer(Of Inner1, Inner2).One",
            vbSimple: "Outer(Of Inner1, Inner2).One");
    }
}
