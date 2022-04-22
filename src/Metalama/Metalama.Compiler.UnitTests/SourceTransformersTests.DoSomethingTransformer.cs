// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.UnitTests;

namespace Metalama.Compiler.UnitTests;

public partial class SourceTransformersTests
{
    private class DoSomethingTransformer : ISourceTransformer
    {
        public void Execute(TransformerContext context)
        {
            var compilation = context.Compilation;

            foreach (var tree in compilation.SyntaxTrees)
            {
                context.ReplaceSyntaxTree(tree, tree.WithInsertAt(0, "/* comment */"));
            }

            context.AddSyntaxTrees(SyntaxFactory.ParseSyntaxTree("class G {}"));
        }
    }
}
