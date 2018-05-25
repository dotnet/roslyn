// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.CodeGeneration
{
    [Trait(Traits.Feature, Traits.Features.CodeGeneration)]
    public class NameGenerationTests : AbstractCodeGenerationTests
    {
        [Fact]
        public void TestIdentifierName()
        {
            Test(
                f => f.IdentifierName("a"),
                cs: "a",
                vb: "a");
        }

        [Fact]
        public void TestIdentifierNameCSharpKeyword()
        {
            Test(
                f => f.IdentifierName("int"),
                cs: "@int",
                vb: "int");
        }

        [Fact]
        public void TestIdentifierNameVisualBasicKeyword()
        {
            Test(
                f => f.IdentifierName("Integer"),
                cs: "Integer",
                vb: "[Integer]");
        }

        [Fact]
        public void TestGenericName1()
        {
            Test(
                f => f.GenericName("Outer", CreateClass("Inner1")),
                cs: "Outer<Inner1>",
                vb: "Outer(Of Inner1)");
        }

        [Fact]
        public void TestGenericName2()
        {
            Test(
                f => f.GenericName("Outer", CreateClass("Inner1"), CreateClass("Inner2")),
                cs: "Outer<Inner1, Inner2>",
                vb: "Outer(Of Inner1, Inner2)");
        }

        [Fact]
        public void TestGenericNameCSharpKeyword()
        {
            Test(
                f => f.GenericName("int", CreateClass("string"), CreateClass("bool")),
                cs: "@int<@string, @bool>",
                vb: "int(Of [string], bool)");
        }

        [Fact]
        public void TestGenericNameVisualBasicKeyword()
        {
            Test(
                f => f.GenericName("Integer", CreateClass("String"), CreateClass("Boolean")),
                cs: "Integer<String, Boolean>",
                vb: "[Integer](Of [String], [Boolean])");
        }

        [Fact]
        public void TestQualifiedName1()
        {
            Test(
                f => f.QualifiedName(f.IdentifierName("Outer"), f.IdentifierName("Inner1")),
                cs: "Outer.Inner1",
                vb: "Outer.Inner1");
        }

        [Fact]
        public void TestQualifiedNameCSharpKeywords1()
        {
            Test(
                f => f.QualifiedName(f.IdentifierName("int"), f.IdentifierName("string")),
                cs: "@int.@string",
                vb: "int.[string]");
        }

        [Fact]
        public void TestQualifiedNameVBKeywords1()
        {
            Test(
                f => f.QualifiedName(f.IdentifierName("Integer"), f.IdentifierName("String")),
                cs: "Integer.String",
                vb: "[Integer].[String]");
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
                vb: "One.Outer(Of Inner1, Inner2)");
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
                vb: "Outer(Of Inner1, Inner2).One");
        }
    }
}
