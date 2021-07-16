// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
    }
}
