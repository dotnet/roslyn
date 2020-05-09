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
        public void TestProperty1()
        {
            AssertEx.AreEqual(
@"class C
{
    int P
    {
        get
        {
        }
    }
}",
Class("C",
    members: ImmutableArray.Create<ISymbol>(
        Property(Int32, "P",
            getMethod: PropertyGet()))).GenerateString());
        }

        [Fact]
        public void TestPublicProperty1()
        {
            AssertEx.AreEqual(
@"class C
{
    public int P
    {
        get
        {
        }
    }
}",
Class("C",
    members: ImmutableArray.Create<ISymbol>(
        Property(Int32, "P",
            declaredAccessibility: Accessibility.Public,
            getMethod: PropertyGet()))).GenerateString());
        }

        [Fact]
        public void TestPublicPropertyAndAccessor1()
        {
            AssertEx.AreEqual(
@"class C
{
    public int P
    {
        get
        {
        }
    }
}",
Class("C",
    members: ImmutableArray.Create<ISymbol>(
        Property(Int32, "P",
            declaredAccessibility: Accessibility.Public,
            getMethod: PropertyGet(
                declaredAccessibility: Accessibility.Public)))).GenerateString());
        }

        [Fact]
        public void TestPublicPropertyAndPrivateAccessor1()
        {
            AssertEx.AreEqual(
@"class C
{
    public int P
    {
        private get
        {
        }
    }
}",
Class("C",
    members: ImmutableArray.Create<ISymbol>(
        Property(Int32, "P",
            declaredAccessibility: Accessibility.Public,
            getMethod: PropertyGet(
                declaredAccessibility: Accessibility.Private)))).GenerateString());
        }

        [Fact]
        public void TestStaticPropertyAndAccessor1()
        {
            AssertEx.AreEqual(
@"class C
{
    static int P
    {
        get
        {
        }
    }
}",
Class("C",
    members: ImmutableArray.Create<ISymbol>(
        Property(Int32, "P",
            modifiers: SymbolModifiers.Static,
            getMethod: PropertyGet(
                modifiers: SymbolModifiers.Static)))).GenerateString());
        }
    }
}
