// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.UseRecursivePatterns
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(CSharpUseRecursivePatternsCodeRefactoringProvider)), Shared]
    internal sealed partial class CSharpUseRecursivePatternsCodeRefactoringProvider : CodeRefactoringProvider
    {
        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var textSpan = context.Span;
            if (textSpan.Length > 0)
            {
                return;
            }

            var document = context.Document;
            var cancellationToken = context.CancellationToken;

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var node = GetOutermostExpression(root.FindToken(textSpan.Start).Parent);
            if (node == null)
            {
                return;
            }

            var semanticModel = await context.Document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var analyzedNode = Analyzer.Analyze(node, semanticModel)?.Reduce();
            if (analyzedNode is null)
            {
                return;
            }

            if (!analyzedNode.IsReduced)
            {
                return;
            }

            context.RegisterRefactoring(new MyCodeAction(
                c => document.ReplaceNodeAsync(node,
                    node.IsKind(SyntaxKind.CasePatternSwitchLabel)
                        ? ((Conjunction)analyzedNode).AsCasePatternSwitchLabelSyntax()
                        : analyzedNode.AsExpressionSyntax(), c)));
        }

        private static SyntaxNode GetOutermostExpression(SyntaxNode node)
        {
            if (node.IsKind(SyntaxKind.WhenClause))
            {
                return node.Parent;
            }

            var current = node;
            SyntaxNode last = null;
            while (current.IsKind(SyntaxKind.LogicalAndExpression, SyntaxKind.EqualsExpression))
            {
                last = current;
                current = current.Parent;
            }

            return last;
        }

        private sealed class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument)
                : base("Use recursive patterns", createChangedDocument)
            {
            }
        }
    }
}
