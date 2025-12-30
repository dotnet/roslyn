// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.UseIsNullCheck;

internal abstract class AbstractUseIsNullCheckForReferenceEqualsCodeFixProvider<TExpressionSyntax>
    : SyntaxEditorBasedCodeFixProvider
    where TExpressionSyntax : SyntaxNode
{
    public override ImmutableArray<string> FixableDiagnosticIds
        => [IDEDiagnosticIds.UseIsNullCheckDiagnosticId];

    protected abstract string GetTitle(bool negated, ParseOptions options);

    protected abstract SyntaxNode CreateNullCheck(TExpressionSyntax argument, bool isUnconstrainedGeneric);
    protected abstract SyntaxNode CreateNotNullCheck(TExpressionSyntax argument);

    private static bool IsSupportedDiagnostic(Diagnostic diagnostic)
        => diagnostic.Properties[UseIsNullConstants.Kind] == UseIsNullConstants.ReferenceEqualsKey;

    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var diagnostic = context.Diagnostics.First();
        if (IsSupportedDiagnostic(diagnostic))
        {
            var negated = diagnostic.Properties.ContainsKey(UseIsNullConstants.Negated);
            var title = GetTitle(negated, diagnostic.Location.SourceTree!.Options);
            RegisterCodeFix(context, title, title);
        }

        return Task.CompletedTask;
    }

    protected override Task FixAllAsync(
        Document document, ImmutableArray<Diagnostic> diagnostics,
        SyntaxEditor editor, CancellationToken cancellationToken)
    {
        var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();

        // Order in reverse so we process inner diagnostics before outer diagnostics.
        // Otherwise, we won't be able to find the nodes we want to replace if they're
        // not there once their parent has been replaced.
        foreach (var diagnostic in diagnostics.OrderByDescending(d => d.Location.SourceSpan.Start))
        {
            if (!IsSupportedDiagnostic(diagnostic))
                continue;

            var invocation = diagnostic.AdditionalLocations[0].FindNode(getInnermostNodeForTie: true, cancellationToken: cancellationToken);
            var negate = diagnostic.Properties.ContainsKey(UseIsNullConstants.Negated);
            var isUnconstrainedGeneric = diagnostic.Properties.ContainsKey(UseIsNullConstants.UnconstrainedGeneric);

            var arguments = syntaxFacts.GetArgumentsOfInvocationExpression(invocation);
            var argument = syntaxFacts.IsNullLiteralExpression(syntaxFacts.GetExpressionOfArgument(arguments[0]))
                ? (TExpressionSyntax)syntaxFacts.GetExpressionOfArgument(arguments[1])
                : (TExpressionSyntax)syntaxFacts.GetExpressionOfArgument(arguments[0]);

            var toReplace = negate ? invocation.GetRequiredParent() : invocation;
            var replacement = negate
                ? CreateNotNullCheck(argument)
                : CreateNullCheck(argument, isUnconstrainedGeneric);

            editor.ReplaceNode(
                toReplace,
                replacement.WithTriviaFrom(toReplace));
        }

        return Task.CompletedTask;
    }
}
