// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
            if (!(root.FindToken(textSpan.Start).Parent is BinaryExpressionSyntax node))
            {
                return;
            }

            if (!node.IsKind(SyntaxKind.LogicalAndExpression, SyntaxKind.EqualsExpression))
            {
                return;
            }

            // TODO find outermost AND operator
            // TODO when clause
            // TODO stop on evaluation node

            //var semanticModel = await context.Document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var analyzedNode = Analyzer.Analyze(node);
            if (analyzedNode == null)
            {
                return;
            }

            var reducedNode = Reducer.Reduce(analyzedNode);
            if (reducedNode == null)
            {
                return;
            }

            context.RegisterRefactoring(new MyCodeAction(
                c => document.ReplaceNodeAsync(node, Rewriter.Rewrite(reducedNode), c)));
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
