// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.UseCompoundAssignment
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    internal class CSharpUseCompoundCoalesceAssignmentCodeFixProvider
        : SyntaxEditorBasedCodeFixProvider
    {
        [ImportingConstructor]
        public CSharpUseCompoundCoalesceAssignmentCodeFixProvider()
        {
        }

        public override ImmutableArray<string> FixableDiagnosticIds { get; } =
            ImmutableArray.Create(IDEDiagnosticIds.UseCoalesceCompoundAssignmentDiagnosticId);

        internal sealed override CodeFixCategory CodeFixCategory => CodeFixCategory.CodeStyle;

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var document = context.Document;
            var diagnostic = context.Diagnostics[0];

            context.RegisterCodeFix(new MyCodeAction(
                c => FixAsync(document, diagnostic, c)),
                context.Diagnostics);

            return Task.CompletedTask;
        }

        protected override Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor, CancellationToken cancellationToken)
        {
            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
            var syntaxKinds = syntaxFacts.SyntaxKinds;

            foreach (var diagnostic in diagnostics)
            {
                var coalesce = diagnostic.AdditionalLocations[0].FindNode(getInnermostNodeForTie: true, cancellationToken);

                editor.ReplaceNode(coalesce,
                    (currentCoalesceNode, generator) =>
                    {
                        var currentCoalesce = (BinaryExpressionSyntax)currentCoalesceNode;
                        var coalesceRight = (ParenthesizedExpressionSyntax)currentCoalesce.Right;
                        var assignment = (AssignmentExpressionSyntax)coalesceRight.Expression;

                        var compoundOperator = SyntaxFactory.Token(SyntaxKind.QuestionQuestionEqualsToken);
                        return SyntaxFactory.AssignmentExpression(
                            SyntaxKind.CoalesceAssignmentExpression,
                            assignment.Left,
                            compoundOperator.WithTriviaFrom(assignment.OperatorToken),
                            assignment.Right);
                    });
            }

            return Task.CompletedTask;
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(FeaturesResources.Use_compound_assignment, createChangedDocument, FeaturesResources.Use_compound_assignment)
            {
            }
        }
    }
}
