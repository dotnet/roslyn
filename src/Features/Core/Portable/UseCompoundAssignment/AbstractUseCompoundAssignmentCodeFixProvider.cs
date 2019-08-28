// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.UseCompoundAssignment
{
    internal abstract class AbstractUseCompoundAssignmentCodeFixProvider<
        TSyntaxKind, TAssignmentSyntax, TExpressionSyntax>
        : SyntaxEditorBasedCodeFixProvider
        where TSyntaxKind : struct
        where TAssignmentSyntax : SyntaxNode
        where TExpressionSyntax : SyntaxNode
    {
        public override ImmutableArray<string> FixableDiagnosticIds { get; } =
            ImmutableArray.Create(IDEDiagnosticIds.UseCompoundAssignmentDiagnosticId);

        internal sealed override CodeFixCategory CodeFixCategory => CodeFixCategory.CodeStyle;

        // See comments in the analyzer for what these maps are for.

        private readonly ImmutableDictionary<TSyntaxKind, TSyntaxKind> _binaryToAssignmentMap;
        private readonly ImmutableDictionary<TSyntaxKind, TSyntaxKind> _assignmentToTokenMap;

        protected AbstractUseCompoundAssignmentCodeFixProvider(
            ImmutableArray<(TSyntaxKind exprKind, TSyntaxKind assignmentKind, TSyntaxKind tokenKind)> kinds)
        {
            Utilities.GenerateMaps(kinds, out _binaryToAssignmentMap, out _assignmentToTokenMap);
        }

        protected abstract TSyntaxKind GetSyntaxKind(int rawKind);
        protected abstract SyntaxToken Token(TSyntaxKind kind);
        protected abstract TAssignmentSyntax Assignment(
            TSyntaxKind assignmentOpKind, TExpressionSyntax left, SyntaxToken syntaxToken, TExpressionSyntax right);

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
                        syntaxFacts.GetPartsOfAssignmentExpressionOrStatement(currentAssignment,
                            out var leftOfAssign, out var equalsToken, out var rightOfAssign);

                        syntaxFacts.GetPartsOfBinaryExpression(rightOfAssign,
                           out _, out var opToken, out var rightExpr);

                        var assignmentOpKind = _binaryToAssignmentMap[GetSyntaxKind(rightOfAssign.RawKind)];
                        var compoundOperator = Token(_assignmentToTokenMap[assignmentOpKind]);
                        return Assignment(
                            assignmentOpKind,
                            (TExpressionSyntax)leftOfAssign,
                            compoundOperator.WithTriviaFrom(equalsToken),
                            (TExpressionSyntax)rightExpr);
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
