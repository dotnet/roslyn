// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.UseCoalesceExpression;

[ExportCodeFixProvider(LanguageNames.CSharp, LanguageNames.VisualBasic, Name = PredefinedCodeFixProviderNames.UseCoalesceExpressionForIfNullStatementCheck), Shared]
[ExtensionOrder(Before = PredefinedCodeFixProviderNames.AddBraces)]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal class UseCoalesceExpressionForIfNullStatementCheckCodeFixProvider() : SyntaxEditorBasedCodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds
        => [IDEDiagnosticIds.UseCoalesceExpressionForIfNullCheckDiagnosticId];

    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        RegisterCodeFix(context, AnalyzersResources.Use_coalesce_expression, nameof(AnalyzersResources.Use_coalesce_expression));
        return Task.CompletedTask;
    }

    protected override Task FixAllAsync(
        Document document, ImmutableArray<Diagnostic> diagnostics,
        SyntaxEditor editor, CodeActionOptionsProvider fallbackOptions, CancellationToken cancellationToken)
    {
        var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
        var generator = editor.Generator;

        foreach (var diagnostic in diagnostics)
        {
            var expressionToCoalesce = diagnostic.AdditionalLocations[0].FindNode(getInnermostNodeForTie: true, cancellationToken);
            var ifStatement = diagnostic.AdditionalLocations[1].FindNode(getInnermostNodeForTie: true, cancellationToken);
            var whenTrueStatement = diagnostic.AdditionalLocations[2].FindNode(getInnermostNodeForTie: true, cancellationToken);

            editor.RemoveNode(ifStatement);
            editor.ReplaceNode(
                expressionToCoalesce,
                generator.CoalesceExpression(
                    expressionToCoalesce.WithoutTrivia(),
                    GetWhenNullExpression(whenTrueStatement).WithoutTrailingTrivia()).WithTriviaFrom(expressionToCoalesce));
        }

        return Task.CompletedTask;

        SyntaxNode GetWhenNullExpression(SyntaxNode whenTrueStatement)
        {
            if (syntaxFacts.IsSimpleAssignmentStatement(whenTrueStatement))
            {
                syntaxFacts.GetPartsOfAssignmentStatement(whenTrueStatement, out _, out var right);
                return right;
            }
            else if (syntaxFacts.IsThrowStatement(whenTrueStatement))
            {
                var expression = syntaxFacts.GetExpressionOfThrowStatement(whenTrueStatement);
                Contract.ThrowIfNull(expression); // checked in analyzer.
                return generator.ThrowExpression(expression);
            }
            else
            {
                throw ExceptionUtilities.Unreachable();
            }
        }
    }
}
