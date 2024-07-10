// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
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

namespace Microsoft.CodeAnalysis.CSharp.RemoveUnnecessaryLambdaExpression;

using static CSharpRemoveUnnecessaryLambdaExpressionDiagnosticAnalyzer;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.RemoveUnnecessaryLambdaExpression), Shared]
internal partial class CSharpRemoveUnnecessaryLambdaExpressionCodeFixProvider : SyntaxEditorBasedCodeFixProvider
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CSharpRemoveUnnecessaryLambdaExpressionCodeFixProvider()
    {
    }

    public override ImmutableArray<string> FixableDiagnosticIds
        => [IDEDiagnosticIds.RemoveUnnecessaryLambdaExpressionDiagnosticId];

    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        RegisterCodeFix(context, CSharpAnalyzersResources.Remove_unnecessary_lambda_expression, nameof(CSharpAnalyzersResources.Remove_unnecessary_lambda_expression));
        return Task.CompletedTask;
    }

    protected override Task FixAllAsync(
        Document document, ImmutableArray<Diagnostic> diagnostics,
        SyntaxEditor editor, CancellationToken cancellationToken)
    {
        foreach (var diagnostic in diagnostics)
        {
            var anonymousFunction = diagnostic.AdditionalLocations[0].FindNode(getInnermostNodeForTie: true, cancellationToken);

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
        }

        return Task.CompletedTask;
    }
}
