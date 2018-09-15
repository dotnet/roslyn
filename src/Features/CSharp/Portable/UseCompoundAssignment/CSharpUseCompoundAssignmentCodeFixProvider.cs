// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
    using static CSharpUseCompoundAssignmentDiagnosticAnalyzer;

    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    internal class CSharpUseCompoundAssignmentCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds { get; } =
            ImmutableArray.Create(IDEDiagnosticIds.UseCompoundAssignmentDiagnosticId);

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
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();

            foreach (var diagnostic in diagnostics)
            {
                var assignment = diagnostic.AdditionalLocations[0].FindNode(getInnermostNodeForTie: true, cancellationToken);

                editor.ReplaceNode(assignment,
                    (currentAssignment, generator) =>
                    {
                        syntaxFacts.GetPartsOfAssignmentStatement(currentAssignment, 
                            out var leftOfAssign, out var equalsToken, out var rightOfAssign);

                        syntaxFacts.GetPartsOfBinaryExpression(rightOfAssign,
                           out _, out var opToken, out var rightExpr);

                        var assignmentOpKind = BinaryToAssignmentMap[rightOfAssign.Kind()];
                        var compoundOperator = SyntaxFactory.Token(AssignmentToTokenMap[assignmentOpKind]);
                        return SyntaxFactory.AssignmentExpression(
                            assignmentOpKind,
                            (ExpressionSyntax)leftOfAssign,
                            compoundOperator.WithTriviaFrom(equalsToken),
                            (ExpressionSyntax)rightExpr);
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
