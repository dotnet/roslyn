// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Metalama.Compiler.UnitTests;

public partial class DiagnosticsTests
{
    private class ChangeTreeParentAndReportTransformer : CSharpSyntaxRewriter, ISourceTransformer
    {
        private static readonly DiagnosticDescriptor _warning = new("MY001", "Test", "Warning on '{0}'", "Test", DiagnosticSeverity.Warning, true);
        private TransformerContext? _context;

        public void Execute(TransformerContext context)
        {
            this._context = context;
            foreach (var tree in context.Compilation.SyntaxTrees)
            {
                var oldRoot = tree.GetRoot();
                var newRoot = Visit(oldRoot);
                if (newRoot != oldRoot)
                {
                    context.ReplaceSyntaxTree(tree, tree.WithRootAndOptions(newRoot, tree.Options));
                }
            }
        }

        public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            var firstStatement = node.Body!.Statements.First();
            _context!.ReportDiagnostic( Microsoft.CodeAnalysis.Diagnostic.Create(_warning, firstStatement.Location, firstStatement.ToString()));
                
            return node.WithBody(
                SyntaxFactory.Block(SyntaxFactory.LockStatement(SyntaxFactory.ThisExpression(), node.Body!)));
        }
    }
}
