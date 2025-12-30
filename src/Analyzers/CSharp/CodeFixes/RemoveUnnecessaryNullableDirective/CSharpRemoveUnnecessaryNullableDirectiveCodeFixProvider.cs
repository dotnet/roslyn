// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
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
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.RemoveUnnecessaryNullableDirective;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.RemoveUnnecessaryNullableDirective), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CSharpRemoveUnnecessaryNullableDirectiveCodeFixProvider()
    : SyntaxEditorBasedCodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds
        => [
            IDEDiagnosticIds.RemoveRedundantNullableDirectiveDiagnosticId,
            IDEDiagnosticIds.RemoveUnnecessaryNullableDirectiveDiagnosticId,
        ];

    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        foreach (var diagnostic in context.Diagnostics)
        {
            if (diagnostic.Id == IDEDiagnosticIds.RemoveRedundantNullableDirectiveDiagnosticId)
                RegisterCodeFix(context, CSharpAnalyzersResources.Remove_redundant_nullable_directive, nameof(CSharpAnalyzersResources.Remove_redundant_nullable_directive), diagnostic);
            else
                RegisterCodeFix(context, CSharpAnalyzersResources.Remove_unnecessary_nullable_directive, nameof(CSharpAnalyzersResources.Remove_unnecessary_nullable_directive), diagnostic);
        }

        return Task.CompletedTask;
    }

    protected override Task FixAllAsync(
        Document document,
        ImmutableArray<Diagnostic> diagnostics,
        SyntaxEditor editor,
        CancellationToken cancellationToken)
    {
        // We first group the nullable directives by the token they are attached This allows to replace each token
        // separately even if they have multiple nullable directives.
        var nullableDirectives = diagnostics
            .Select(d => d.Location.FindNode(findInsideTrivia: true, getInnermostNodeForTie: true, cancellationToken))
            .OfType<NullableDirectiveTriviaSyntax>();

        foreach (var (token, directives) in nullableDirectives.GroupBy(d => d.ParentTrivia.Token))
        {
            var leadingTrivia = token.LeadingTrivia;
            var nullableDirectiveIndices = nullableDirectives
                .Select(x => leadingTrivia.IndexOf(x.ParentTrivia));

            // Walk backwards through the nullable directives on the token.  That way our indices stay correct as we
            // are removing later directives.
            foreach (var index in nullableDirectiveIndices.OrderByDescending(x => x))
            {
                // Remove the directive itself.
                leadingTrivia = leadingTrivia.RemoveAt(index);

                // If we have a blank line both before and after the directive, then remove the one that follows to
                // keep the code clean.
                if (HasPrecedingBlankLine(leadingTrivia, index - 1) &&
                    HasFollowingBlankLine(leadingTrivia, index))
                {
                    // Delete optional following whitespace.
                    if (leadingTrivia[index].IsWhitespace())
                        leadingTrivia = leadingTrivia.RemoveAt(index);

                    // Then the following blank line.
                    leadingTrivia = leadingTrivia.RemoveAt(index);
                }
            }

            // Update the token and replace it within its parent.
            var node = token.GetRequiredParent();
            editor.ReplaceNode(
                node,
                node.ReplaceToken(token, token.WithLeadingTrivia(leadingTrivia)));
        }

        return Task.CompletedTask;
    }

    private static bool HasPrecedingBlankLine(SyntaxTriviaList leadingTrivia, int index)
    {
        if (index >= 0 && leadingTrivia[index].IsWhitespace())
            index--;

        return index >= 0 && leadingTrivia[index].IsEndOfLine();
    }

    private static bool HasFollowingBlankLine(SyntaxTriviaList leadingTrivia, int index)
    {
        if (index < leadingTrivia.Count && leadingTrivia[index].IsWhitespace())
            index++;

        return index < leadingTrivia.Count && leadingTrivia[index].IsEndOfLine();
    }
}
