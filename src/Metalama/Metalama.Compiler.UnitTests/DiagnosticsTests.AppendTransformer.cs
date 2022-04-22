// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Metalama.Compiler.UnitTests;

public partial class DiagnosticsTests
{
    private class AppendTransformer : ISourceTransformer
    {
        private readonly CompilationUnitSyntax _newCode;

        public AppendTransformer(string newCode)
        {
            _newCode = (CompilationUnitSyntax)SyntaxFactory.ParseSyntaxTree(newCode).GetRoot()!;
        }

        public void Execute(TransformerContext context)
        {
            var syntaxTree = context.Compilation.SyntaxTrees.Single();
            var oldRoot = (CompilationUnitSyntax)syntaxTree.GetRoot();
            var newRoot = oldRoot.AddMembers(_newCode.Members.ToArray());
            var modifiedSyntaxTree = syntaxTree.WithRootAndOptions(newRoot, syntaxTree.Options);
            context.ReplaceSyntaxTree(syntaxTree, modifiedSyntaxTree);
        }
    }
}
