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
        public void TestConstructor1()
        {
            AssertEx.AreEqual(
@"class C
{
    C();
}",
Class(
    "C",
    members: ImmutableArray.Create<ISymbol>(
        Constructor())).GenerateString());
        }

        [Fact]
        public void TestGenericTypeConstructor1()
        {
            AssertEx.AreEqual(
@"class C<T>
{
    C();
}",
Class(
    "C",
    typeArguments: ImmutableArray.Create<ITypeSymbol>(TypeParameter("T")),
    members: ImmutableArray.Create<ISymbol>(
        Constructor())).GenerateString());
        }

        [Fact]
        public void TestConstructorWithAccessibility1()
        {
            AssertEx.AreEqual(
@"class C
{
    public C();
}",
Class(
    "C",
    members: ImmutableArray.Create<ISymbol>(
        Constructor(
            declaredAccessibility: Accessibility.Public))).GenerateString());
        }

        [Fact]
        public void TestConstructorWithModifiers1()
        {
            AssertEx.AreEqual(
@"class C
{
    unsafe C();
}",
Class(
    "C",
    members: ImmutableArray.Create<ISymbol>(
        Constructor(
            modifiers: SymbolModifiers.Unsafe))).GenerateString());
        }

        [Fact]
        public void TestConstructorWithParameters1()
        {
            AssertEx.AreEqual(
@"class C
{
    C(int i);
}",
Class(
    "C",
    members: ImmutableArray.Create<ISymbol>(
        Constructor(
            parameters: ImmutableArray.Create(Parameter(
                Int32, "i"))))).GenerateString());
        }

        [Fact]
        public void TestConstructorWithParamsParameters1()
        {
            AssertEx.AreEqual(
@"class C
{
    C(params int i);
}",
Class(
    "C",
    members: ImmutableArray.Create<ISymbol>(
        Constructor(
            parameters: ImmutableArray.Create(Parameter(
                Int32,
                "i",
                modifiers: SymbolModifiers.Params))))).GenerateString());
        }
    }
}
