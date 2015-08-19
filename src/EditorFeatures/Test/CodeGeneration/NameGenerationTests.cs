// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Xunit;

#if false
namespace Roslyn.Services.Editor.UnitTests.CodeGeneration
{
    public class NameGenerationTests : AbstractCodeGenerationTests
    {
        [WpfFact]
        public void TestIdentifierName()
        {
            TestName(
                f => f.CreateIdentifierName("a"),
                cs: "a",
                vb: "a");
        }

        [WpfFact]
        public void TestIdentifierNameCSharpKeyword()
        {
            TestName(
                f => f.CreateIdentifierName("int"),
                cs: "@int",
                vb: "int");
        }

        [WpfFact]
        public void TestIdentifierNameVisualBasicKeyword()
        {
            TestName(
                f => f.CreateIdentifierName("Integer"),
                cs: "Integer",
                vb: "[Integer]");
        }

        [WpfFact]
        public void TestGenericName1()
        {
            TestName(
                f => f.CreateGenericName("Outer", CreateClass("Inner1")),
                cs: "Outer<Inner1>",
                vb: "Outer(Of Inner1)");
        }

        [WpfFact]
        public void TestGenericName2()
        {
            TestName(
                f => f.CreateGenericName("Outer", CreateClass("Inner1"), CreateClass("Inner2")),
                cs: "Outer<Inner1, Inner2>",
                vb: "Outer(Of Inner1, Inner2)");
        }

        [WpfFact]
        public void TestGenericNameCSharpKeyword()
        {
            TestName(
                f => f.CreateGenericName("int", CreateClass("string"), CreateClass("bool")),
                cs: "@int<@string, @bool>",
                vb: "int(Of [string], bool)");
        }

        [WpfFact]
        public void TestGenericNameVisualBasicKeyword()
        {
            TestName(
                f => f.CreateGenericName("Integer", CreateClass("String"), CreateClass("Boolean")),
                cs: "Integer<String, Boolean>",
                vb: "[Integer](Of [String], [Boolean])");
        }

        [WpfFact]
        public void TestQualifiedName1()
        {
            TestName(
                f => f.CreateQualifiedName(f.CreateIdentifierName("Outer"), f.CreateIdentifierName("Inner1")),
                cs: "Outer.Inner1",
                vb: "Outer.Inner1");
        }

        [WpfFact]
        public void TestQualifiedNameCSharpKeywords1()
        {
            TestName(
                f => f.CreateQualifiedName(f.CreateIdentifierName("int"), f.CreateIdentifierName("string")),
                cs: "@int.@string",
                vb: "int.string");
        }

        [WpfFact]
        public void TestQualifiedNameVBKeywords1()
        {
            TestName(
                f => f.CreateQualifiedName(f.CreateIdentifierName("Integer"), f.CreateIdentifierName("String")),
                cs: "Integer.String",
                vb: "[Integer].String");
        }

        [WpfFact]
        public void TestQualifiedGenericName1()
        {
            TestName(
                f => f.CreateQualifiedName(
                    f.CreateIdentifierName("One"),
                    f.CreateGenericName("Outer",
                        CreateClass("Inner1"),
                        CreateClass("Inner2"))),
                cs: "One.Outer<Inner1, Inner2>",
                vb: "One.Outer(Of Inner1, Inner2)");
        }

        [WpfFact]
        public void TestQualifiedGenericName2()
        {
            TestName(
                f => f.CreateQualifiedName(
                    f.CreateGenericName("Outer",
                        CreateClass("Inner1"),
                        CreateClass("Inner2")),
                    f.CreateIdentifierName("One")),
                cs: "Outer<Inner1, Inner2>.One",
                vb: "Outer(Of Inner1, Inner2).One");
        }
    }
}
#endif
