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
        public void TestNamespace()
        {
            AssertEx.AreEqual(
@"namespace N
{
}",
Namespace("N").GenerateString());
        }

        [Fact]
        public void TestGlobalNamespace()
        {
            AssertEx.AreEqual(
@"",
GlobalNamespace().GenerateString());
        }

        [Fact]
        public void TestNamespaceWithImport()
        {
            AssertEx.AreEqual(
@"namespace N
{
    using N2;
}",
Namespace("N",
    imports: ImmutableArray.Create<INamespaceOrTypeSymbol>(Namespace("N2"))).GenerateString());
        }

        [Fact]
        public void TestGlobalNamespaceWithImport()
        {
            AssertEx.AreEqual(
@"using N2;",
GlobalNamespace(
    imports: ImmutableArray.Create<INamespaceOrTypeSymbol>(Namespace("N2"))).GenerateString());
        }

        [Fact]
        public void TestNamespaceWithDottedName()
        {
            AssertEx.AreEqual(
@"namespace N1.N2
{
}",
Namespace("N1.N2").GenerateString());
        }

        [Fact]
        public void TestNamespaceWithNestedNamespace()
        {
            AssertEx.AreEqual(
@"namespace N1
{
    namespace N2
    {
    }
}",
Namespace("N1",
    members: ImmutableArray.Create<INamespaceOrTypeSymbol>(Namespace("N2"))).GenerateString());
        }

        [Fact]
        public void TestGlobalNamespaceWithNestedNamespace()
        {
            AssertEx.AreEqual(
@"namespace N2
{
}",
GlobalNamespace(
    members: ImmutableArray.Create<INamespaceOrTypeSymbol>(Namespace("N2"))).GenerateString());
        }
    }
}
