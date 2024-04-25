// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseCompoundAssignment;

using static CSharpSyntaxTokens;
using static SyntaxFactory;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.UseCompoundCoalesceAssignment), Shared]
internal class CSharpUseCompoundCoalesceAssignmentCodeFixProvider : SyntaxEditorBasedCodeFixProvider
{
    [ImportingConstructor]
    [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
    public CSharpUseCompoundCoalesceAssignmentCodeFixProvider()
    {
    }

    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        [IDEDiagnosticIds.UseCoalesceCompoundAssignmentDiagnosticId];

    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        RegisterCodeFix(context, AnalyzersResources.Use_compound_assignment, nameof(AnalyzersResources.Use_compound_assignment));
        return Task.CompletedTask;
    }

    protected override async Task FixAllAsync(
        Document document, ImmutableArray<Diagnostic> diagnostics,
        SyntaxEditor editor, CodeActionOptionsProvider fallbackOptions, CancellationToken cancellationToken)
    {
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

        var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
        var syntaxKinds = syntaxFacts.SyntaxKinds;

        foreach (var diagnostic in diagnostics)
        {
            var coalesceOrIfStatement = diagnostic.AdditionalLocations[0].FindNode(getInnermostNodeForTie: true, cancellationToken);

            if (coalesceOrIfStatement is IfStatementSyntax ifStatement)
            {
                Contract.ThrowIfFalse(CSharpUseCompoundCoalesceAssignmentDiagnosticAnalyzer.GetWhenTrueAssignment(
                    ifStatement, out var whenTrueStatement, out var assignment));

                // we have `if (x is null) x = y;`.  Update `x = y` to be `x ??= y`, then replace the entire
                // if-statement with that assignment statement.
                var newAssignment = AssignmentExpression(
                    SyntaxKind.CoalesceAssignmentExpression,
                    assignment.Left,
                    QuestionQuestionEqualsToken.WithTriviaFrom(assignment.OperatorToken),
                    assignment.Right).WithTriviaFrom(assignment);

                var newWhenTrueStatement = whenTrueStatement.ReplaceNode(assignment, newAssignment);

                // If there's leading trivia on the original inner statement, then combine that with the leading
                // trivia on the if-statement.  We'll need to add a formatting annotation so that the leading comments
                // are put in the right location.
                if (newWhenTrueStatement.GetLeadingTrivia().Any(t => t.IsSingleOrMultiLineComment()))
                {
                    newWhenTrueStatement = newWhenTrueStatement
                        .WithPrependedLeadingTrivia(ifStatement.GetLeadingTrivia())
                        .WithAdditionalAnnotations(Formatter.Annotation);
                }
                else
                {
                    newWhenTrueStatement = newWhenTrueStatement.WithLeadingTrivia(ifStatement.GetLeadingTrivia());
                }

                // If there's trailing comments on the original inner statement, then preserve that.  Otherwise,
                // replace it with the trailing trivia of hte original if-statement.
                if (!newWhenTrueStatement.GetTrailingTrivia().Any(t => t.IsSingleOrMultiLineComment()))
                    newWhenTrueStatement = newWhenTrueStatement.WithTrailingTrivia(ifStatement.GetTrailingTrivia());

                editor.ReplaceNode(ifStatement, newWhenTrueStatement);
            }
            else
            {
                var coalesce = coalesceOrIfStatement;
                // changing from `x ?? (x = y)` to `x ??= y` can change the type.  Specifically,
                // with nullable value types (`int?`) it could change from `int?` to `int`.
                //
                // Add an explicit cast to the original type to ensure semantics are preserved. 
                // Simplification engine can then remove it if it's not necessary.
                var type = semanticModel.GetTypeInfo(coalesce, cancellationToken).Type;

                editor.ReplaceNode(coalesce,
                    (currentCoalesceNode, generator) =>
                    {
                        var currentCoalesce = (BinaryExpressionSyntax)currentCoalesceNode;
                        var coalesceRight = (ParenthesizedExpressionSyntax)currentCoalesce.Right;
                        var assignment = (AssignmentExpressionSyntax)coalesceRight.Expression;

                        var compoundOperator = QuestionQuestionEqualsToken;
                        var finalAssignment = AssignmentExpression(
                            SyntaxKind.CoalesceAssignmentExpression,
                            assignment.Left,
                            compoundOperator.WithTriviaFrom(assignment.OperatorToken),
                            assignment.Right);

                        return type == null || type.IsErrorType()
                            ? finalAssignment
                            : generator.CastExpression(type, finalAssignment);
                    });
            }
        }
    }
}
