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
        public void TestField1()
        {
            AssertEx.AreEqual(
"int f;",
Field("f", SpecialType(SpecialType.System_Int32)).GenerateString());
        }

        [Fact]
        public void TestFieldWithAccessibility1()
        {
            AssertEx.AreEqual(
"public int f;",
Field(
    "f",
    SpecialType(SpecialType.System_Int32),
    declaredAccessibility: Accessibility.Public).GenerateString());
        }

        [Fact]
        public void TestFieldWithModifiers1()
        {
            AssertEx.AreEqual(
"static int f;",
Field(
    "f",
    SpecialType(SpecialType.System_Int32),
    modifiers: SymbolModifiers.Static).GenerateString());
        }

        [Fact]
        public void TestFieldWithAccessibilityAndModifiers1()
        {
            AssertEx.AreEqual(
"private static int f;",
Field(
    "f",
    SpecialType(SpecialType.System_Int32),
    declaredAccessibility: Accessibility.Private,
    modifiers: SymbolModifiers.Static).GenerateString());
        }

        [Fact]
        public void TestConstantField1()
        {
            AssertEx.AreEqual(
"const int f;",
Field(
    "f",
    SpecialType(SpecialType.System_Int32),
    modifiers: SymbolModifiers.Const).GenerateString());
        }
    }
}
