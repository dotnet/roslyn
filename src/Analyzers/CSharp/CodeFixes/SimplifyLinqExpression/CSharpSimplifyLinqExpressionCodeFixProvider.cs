// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;

namespace Microsoft.CodeAnalysis.CSharp.SimplifyLinqExpression
{
    internal sealed class CSharpSimplifyLinqExpressionCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public CSharpSimplifyLinqExpressionCodeFixProvider()
        {
        }

        public sealed override ImmutableArray<string> FixableDiagnosticIds
           => ImmutableArray.Create(IDEDiagnosticIds.SimplifyLinqExpressionsDiagnosticId);

        internal override CodeFixCategory CodeFixCategory => CodeFixCategory.CodeQuality;

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            context.RegisterCodeFix(new MyCodeAction(
                c => FixAsync(context.Document, context.Diagnostics.First(), c)), context.Diagnostics);
            return Task.CompletedTask;
        }

        protected override async Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor, CancellationToken cancellationToken)
        {
            var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            foreach (var diagnostic in diagnostics)
            {
                var node = editor.OriginalRoot.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
                RemoveWhere(model, editor, node);
            }
        }

        private static void RemoveWhere(SemanticModel model, SyntaxEditor editor, SyntaxNode node)
        {
            var childNode = ((InvocationExpressionSyntax)node).Expression;

            // Get the Linq expression being invoked
            // Example: 'Single' from 'Data.Where(x => x == 1).Single()'
            var memberAccess = (MemberAccessExpressionSyntax)childNode;

            // Retrieve the lambda expression from the node
            // Example: 'x => x == 1' from 'Data.Where(x => x == 1).Single()'
            var lambda = ((InvocationExpressionSyntax)memberAccess.Expression).ArgumentList;

            // Get the data or object the query is being called on
            // Example: 'Data' from 'Data.Where(x => x == 1).Single()'
            var objectNodeSyntax = model.GetOperation(memberAccess.Expression).Children.FirstOrDefault().Syntax;
            ExpressionSyntax expression;
            if (objectNodeSyntax.IsKind(SyntaxKind.InvocationExpression) || objectNodeSyntax.IsKind(SyntaxKind.SimpleMemberAccessExpression))
            {
                expression = (ExpressionSyntax)objectNodeSyntax;
            }
            else
            {
                expression = SyntaxFactory.IdentifierName(((IdentifierNameSyntax)objectNodeSyntax).Identifier.Text);
            }
            var newNode = SyntaxFactory.InvocationExpression(
                                SyntaxFactory.MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    expression,
                                    memberAccess.Name))
                            .WithArgumentList(lambda);
            editor.ReplaceNode(childNode.Parent, newNode);
        }

        private class MyCodeAction : CustomCodeActions.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(CSharpAnalyzersResources.Simplify_Linq_expression, createChangedDocument, CSharpAnalyzersResources.Simplify_Linq_expression)
            {
            }
        }
    }
}
