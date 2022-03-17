// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            UseCompoundAssignmentUtilities.GenerateMaps(kinds, out _binaryToAssignmentMap, out _assignmentToTokenMap);
        }

        protected abstract SyntaxToken Token(TSyntaxKind kind);
        protected abstract TAssignmentSyntax Assignment(
            TSyntaxKind assignmentOpKind, TExpressionSyntax left, SyntaxToken syntaxToken, TExpressionSyntax right);
        protected abstract TExpressionSyntax Increment(TExpressionSyntax left, bool postfix);
        protected abstract TExpressionSyntax Decrement(TExpressionSyntax left, bool postfix);
        protected abstract SyntaxTriviaList PrepareRightExpressionLeadingTrivia(SyntaxTriviaList initialTrivia);

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
                var assignment = diagnostic.AdditionalLocations[0].FindNode(getInnermostNodeForTie: true, cancellationToken);

                editor.ReplaceNode(assignment,
                    (current, generator) =>
                    {
                        if (current is not TAssignmentSyntax currentAssignment)
                            return current;

                        syntaxFacts.GetPartsOfAssignmentExpressionOrStatement(currentAssignment,
                            out var leftOfAssign, out var equalsToken, out var rightOfAssign);

                        while (syntaxFacts.IsParenthesizedExpression(rightOfAssign))
                            rightOfAssign = syntaxFacts.Unparenthesize(rightOfAssign);

                        syntaxFacts.GetPartsOfBinaryExpression(rightOfAssign,
                            out _, out var opToken, out var rightExpr);

                        if (diagnostic.Properties.ContainsKey(UseCompoundAssignmentUtilities.Increment))
                            return Increment((TExpressionSyntax)leftOfAssign, PreferPostfix(syntaxFacts, currentAssignment)).WithTriviaFrom(currentAssignment);

                        if (diagnostic.Properties.ContainsKey(UseCompoundAssignmentUtilities.Decrement))
                            return Decrement((TExpressionSyntax)leftOfAssign, PreferPostfix(syntaxFacts, currentAssignment)).WithTriviaFrom(currentAssignment);

                        var assignmentOpKind = _binaryToAssignmentMap[syntaxKinds.Convert<TSyntaxKind>(rightOfAssign.RawKind)];
                        var compoundOperator = Token(_assignmentToTokenMap[assignmentOpKind]);

                        rightExpr = rightExpr.WithLeadingTrivia(PrepareRightExpressionLeadingTrivia(rightExpr.GetLeadingTrivia()));

                        return Assignment(
                            assignmentOpKind,
                            (TExpressionSyntax)leftOfAssign,
                            compoundOperator.WithTriviaFrom(equalsToken),
                            (TExpressionSyntax)rightExpr);
                    });
            }

            return Task.CompletedTask;
        }

        protected virtual bool PreferPostfix(ISyntaxFactsService syntaxFacts, TAssignmentSyntax currentAssignment)
        {
            // If we have `x = x + 1;` on it's own, then we prefer `x++` as idiomatic.
            if (syntaxFacts.IsSimpleAssignmentStatement(currentAssignment.Parent))
                return true;

            // In any other circumstance, the value of the assignment might be read, so we need to transform to
            // ++x to ensure that we preserve semantics.
            return false;
        }

        private class MyCodeAction : CustomCodeActions.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(AnalyzersResources.Use_compound_assignment, createChangedDocument, AnalyzersResources.Use_compound_assignment)
            {
            }
        }
    }
}
