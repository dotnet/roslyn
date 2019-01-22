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

            SyntaxNode node;
            var token = root.FindToken(textSpan.Start);
            if (token.IsKind(SyntaxKind.EqualsEqualsToken, SyntaxKind.AmpersandAmpersandToken))
            {
                node = token.Parent;
            }
            else if (token.IsKind(SyntaxKind.WhenKeyword))
            {
                node = token.Parent.Parent;
            }
            else
            {
                return;
            }

            // TODO find outermost AND operator

            //var semanticModel = await context.Document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var analyzedNode = Analyzer.Analyze(node);
            if (analyzedNode == null)
            {
                return;
            }

            // TODO bail on trivial cases

            //if (!analyzedNode.CanReduce)
            //{
            //    return;
            //}

            var reducedNode = analyzedNode.Reduce();
            if (reducedNode == null)
            {
                return;
            }

            context.RegisterRefactoring(new MyCodeAction(
                c => document.ReplaceNodeAsync(node,
                    node.IsKind(SyntaxKind.CasePatternSwitchLabel)
                        ? ((Conjuction)reducedNode).AsCasePatternSwitchLabelSyntax()
                        : (SyntaxNode)reducedNode.AsExpressionSyntax(), c)));
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
