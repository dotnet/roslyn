// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using System.Linq;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics
{
    /// <summary>
    /// Tests for problems discovered feeding the compiler random trash.
    /// </summary>
    public class FuzzTests : CompilingTestBase
    {
        [Fact, WorkItem(16167, "https://github.com/dotnet/roslyn/issues/16167")]
        public void CompileXmlAsSource()
        {
            var text = @"
using System;
using System.Collections.Generic;
using System.Linq;

class C
{
    public static void Main()
    {
      <summary>Copies a range of elements from an <see cref=""T:System.Array"" /> starting at the first element and pastes them into another <see cref=""T:System.Array"" /> starting at the first element. The length is specified as a 32-bit integer.</summary>
      <param name=""sourceArray"">The <see cref=""T:System.Array"" /> that contains the data to copy.</param>
      <param name=""destinationArray"">The <see cref=""T:System.Array"" /> that receives the data.</param>
      <param name=""length"">A 32-bit integer that represents the number of elements to copy.</param>
      <exception cref=""T:System.ArgumentNullException"">
        <paramref name=""sourceArray"" /> is null.-or-<paramref name=""destinationArray"" /> is null.</exception>
      <exception cref=""T:System.RankException"">
        <paramref name=""sourceArray"" /> and <paramref name=""destinationArray"" /> have different ranks.</exception>
      <exception cref=""T:System.ArrayTypeMismatchException"">
        <paramref name=""sourceArray"" /> and <paramref name=""destinationArray"" /> are of incompatible types.</exception>
      <exception cref=""T:System.InvalidCastException"">At least one element in <paramref name=""sourceArray"" /> cannot be cast to the type of <paramref name=""destinationArray"" />.</exception>
      <exception cref=""T:System.ArgumentOutOfRangeException"">
        <paramref name=""length"" /> is less than zero.</exception>
      <exception cref=""T:System.ArgumentException"">
        <paramref name=""length"" /> is greater than the number of elements in <paramref name=""sourceArray"" />.-or-<paramref name=""length"" /> is greater than the number of elements in <paramref name=""destinationArray"" />.</exception>
    }
}
";
            var compilation = CreateCompilationWithMscorlib45(text);
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);
            foreach (var node in tree.GetRoot().DescendantNodes())
            {
                var _ = model.GetSymbolInfo(node);
            }
        }

        [Fact, WorkItem(16167, "https://github.com/dotnet/roslyn/issues/16167")]
        public void Bug16167()
        {
            var text = @"
class C
{
    public static void Main(int arg)
    {
        void Local1(bool b = M(arg is int z1, z1), int s1 = z1) {}
        void Local2(bool b = M(M(out int z2), z2)), int s2 = z2) {}
        void Local3(bool b = M(M((int z3, int a2) = (1, 2)), z3), int a3 = z3) {}

        void Local4(bool b = M(arg is var z4, z4), int s1 = z4) {}
        void Local5(bool b = M(M(out var z5), z5)), int s2 = z5) {}
        void Local6(bool b = M(M((var z6, int a2) = (1, 2)), z6), int a3 = z6) {}
    }
    static void M(out int z) => z = 1; // needed to infer type of z5
}
";
            // the scope of an expression variable introduced in the default expression
            // of a local function parameter is that default expression.
            var compilation = CreateCompilationWithMscorlib45(text);
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);
            var descendents = tree.GetRoot().DescendantNodes();
            for (int i = 1; i <= 6; i++)
            {
                var name = $"z{i}";
                var designation = descendents.OfType<SingleVariableDesignationSyntax>().Where(d => d.Identifier.ValueText == name).Single();
                var symbol = (ILocalSymbol)model.GetDeclaredSymbol(designation);
                Assert.NotNull(symbol);
                Assert.Equal("System.Int32", symbol.Type.ToTestDisplayString());
                var refs = descendents.OfType<IdentifierNameSyntax>().Where(n => n.Identifier.ValueText == name).ToArray();
                Assert.Equal(2, refs.Length);
                Assert.Equal(symbol, model.GetSymbolInfo(refs[0]).Symbol);
                Assert.Null(model.GetSymbolInfo(refs[1]).Symbol);
            }
        }
    }
}
