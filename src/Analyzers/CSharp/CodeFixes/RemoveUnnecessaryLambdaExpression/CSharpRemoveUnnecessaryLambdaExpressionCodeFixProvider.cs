// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.RemoveUnnecessaryLambdaExpression;

using static CSharpRemoveUnnecessaryLambdaExpressionDiagnosticAnalyzer;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.RemoveUnnecessaryLambdaExpression), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed partial class CSharpRemoveUnnecessaryLambdaExpressionCodeFixProvider() : SyntaxEditorBasedCodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds
        => [IDEDiagnosticIds.RemoveUnnecessaryLambdaExpressionDiagnosticId];

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        RegisterCodeFix(context, CSharpAnalyzersResources.Remove_unnecessary_lambda_expression, nameof(CSharpAnalyzersResources.Remove_unnecessary_lambda_expression));
    }

    protected override async Task FixAllAsync(
        Document document, ImmutableArray<Diagnostic> diagnostics,
        SyntaxEditor editor, CancellationToken cancellationToken)
    {
        foreach (var diagnostic in diagnostics)
        {
            if (diagnostic.AdditionalLocations[0].FindNode(getInnermostNodeForTie: true, cancellationToken) is not AnonymousFunctionExpressionSyntax anonymousFunction)
                continue;

            editor.ReplaceNode(anonymousFunction,
                (current, generator) =>
                {
                    if (current is AnonymousFunctionExpressionSyntax anonymousFunction &&
                        TryGetAnonymousFunctionInvocation(anonymousFunction, out var invocation, out _))
                    {
                        return invocation.Expression.WithTriviaFrom(current).Parenthesize();
                    }

                    return current;
                });

            // If the inner invocation has important trivia on it, move it to the container of the anonymous function.
            if (TryGetAnonymousFunctionInvocation(anonymousFunction, out var invocation, out _) &&
                invocation.GetLeadingTrivia().Any(t => t.IsSingleOrMultiLineComment()))
            {
                var containingStatement = anonymousFunction.AncestorsAndSelf().OfType<StatementSyntax>().FirstOrDefault();
                if (containingStatement != null)
                {
                    editor.ReplaceNode(containingStatement,
                        (current, generator) => current
                            .WithPrependedLeadingTrivia(TakeComments(invocation.GetLeadingTrivia()))
                            .WithAdditionalAnnotations(Formatter.Annotation));
                }
            }
        }
    }

    private static IEnumerable<SyntaxTrivia> TakeComments(SyntaxTriviaList triviaList)
    {
        var lastComment = triviaList.Last(t => t.IsSingleOrMultiLineComment());
        var lastIndex = triviaList.IndexOf(lastComment) + 1;
        if (lastIndex < triviaList.Count && triviaList[lastIndex].IsEndOfLine())
            lastIndex++;

        return triviaList.Take(lastIndex);
    }
}
