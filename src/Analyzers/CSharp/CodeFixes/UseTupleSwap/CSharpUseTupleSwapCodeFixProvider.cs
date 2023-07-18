// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseTupleSwap
{
    using static SyntaxFactory;

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.UseTupleSwap), Shared]
    internal partial class CSharpUseTupleSwapCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpUseTupleSwapCodeFixProvider()
        {
        }

        public override ImmutableArray<string> FixableDiagnosticIds { get; }
            = ImmutableArray.Create(IDEDiagnosticIds.UseTupleSwapDiagnosticId);

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            RegisterCodeFix(context, CSharpAnalyzersResources.Use_tuple_to_swap_values, nameof(CSharpAnalyzersResources.Use_tuple_to_swap_values));
            return Task.CompletedTask;
        }

        protected override Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor, CodeActionOptionsProvider fallbackOptions, CancellationToken cancellationToken)
        {
            foreach (var diagnostic in diagnostics)
                FixOne(editor, diagnostic, cancellationToken);

            return Task.CompletedTask;
        }

        private static void FixOne(
            SyntaxEditor editor, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var localDeclarationStatement = (LocalDeclarationStatementSyntax)diagnostic.AdditionalLocations[0].FindNode(getInnermostNodeForTie: true, cancellationToken);
            // `expr_a = expr_b`;
            var firstAssignmentStatement = (ExpressionStatementSyntax)diagnostic.AdditionalLocations[1].FindNode(getInnermostNodeForTie: true, cancellationToken);
            var secondAssignmentStatement = (ExpressionStatementSyntax)diagnostic.AdditionalLocations[2].FindNode(getInnermostNodeForTie: true, cancellationToken);

            editor.RemoveNode(firstAssignmentStatement);
            editor.RemoveNode(secondAssignmentStatement);

            var assignment = (AssignmentExpressionSyntax)firstAssignmentStatement.Expression;
            var exprA = assignment.Left.WalkDownParentheses().WithoutTrivia();
            var exprB = assignment.Right.WalkDownParentheses().WithoutTrivia();

            var tupleAssignmentStatement = ExpressionStatement(AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                TupleExpression(SeparatedList(new[] { Argument(exprB), Argument(exprA) })),
                TupleExpression(SeparatedList(new[] { Argument(exprA), Argument(exprB) }))));

            editor.ReplaceNode(localDeclarationStatement, tupleAssignmentStatement.WithTriviaFrom(localDeclarationStatement));
        }
    }
}
