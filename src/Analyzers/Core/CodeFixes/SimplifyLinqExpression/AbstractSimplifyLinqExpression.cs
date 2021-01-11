// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;

namespace Microsoft.CodeAnalysis.SimplifyLinqExpression
{
    internal abstract class AbstractSimplifyLinqExpressionCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
           => ImmutableArray.Create(IDEDiagnosticIds.SimplifyLinqExpressionsDiagnosticId);

        internal override CodeFixCategory CodeFixCategory => CodeFixCategory.CodeQuality;

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            context.RegisterCodeFix(new MyCodeAction(
                c => FixAsync(context.Document, context.Diagnostics.First(), c)),
                context.Diagnostics);
            return Task.CompletedTask;
        }

        protected override Task FixAllAsync(Document document,
                                            ImmutableArray<Diagnostic> diagnostics,
                                            SyntaxEditor editor,
                                            CancellationToken cancellationToken)
        {
            var root = editor.OriginalRoot;
            foreach (var diagnostic in diagnostics)
            {
                var linqExpression = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
                editor.ReplaceNode(linqExpression, (nodeToRewrite, generator) =>
                {
                    var memberAccessExpression = GetMemberAccessExpression(nodeToRewrite);
                    var identiferName = GetIdentifierName(nodeToRewrite);
                    var lambdaExpression = GetLambdaExpression(nodeToRewrite);
                    return generator.InvocationExpression(
                        generator.MemberAccessExpression(memberAccessExpression, identiferName),
                        lambdaExpression);
                });
            }

            return Task.CompletedTask;
        }

        protected abstract SyntaxNode? GetIdentifierName(SyntaxNode node);

        protected abstract SyntaxNode[] GetLambdaExpression(SyntaxNode node);

        protected abstract SyntaxNode GetMemberAccessExpression(SyntaxNode node);

        private class MyCodeAction : CustomCodeActions.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(AnalyzersResources.Simplify_Linq_expression, createChangedDocument, AnalyzersResources.Simplify_Linq_expression)
            {
            }
        }
    }
}
