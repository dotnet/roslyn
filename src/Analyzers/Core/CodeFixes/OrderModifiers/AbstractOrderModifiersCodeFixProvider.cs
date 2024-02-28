// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.OrderModifiers;

internal abstract class AbstractOrderModifiersCodeFixProvider : SyntaxEditorBasedCodeFixProvider
{
    private readonly ISyntaxFacts _syntaxFacts;
    private readonly AbstractOrderModifiersHelpers _helpers;

    protected AbstractOrderModifiersCodeFixProvider(
        ISyntaxFacts syntaxFacts,
        AbstractOrderModifiersHelpers helpers)
    {
        _syntaxFacts = syntaxFacts;
        _helpers = helpers;
    }

    protected abstract ImmutableArray<string> FixableCompilerErrorIds { get; }
    protected abstract CodeStyleOption2<string> GetCodeStyleOption(AnalyzerOptionsProvider options);

    public sealed override ImmutableArray<string> FixableDiagnosticIds
        => FixableCompilerErrorIds.Add(IDEDiagnosticIds.OrderModifiersDiagnosticId);

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var syntaxTree = await context.Document.GetRequiredSyntaxTreeAsync(context.CancellationToken).ConfigureAwait(false);
        var syntaxNode = Location.Create(syntaxTree, context.Span).FindNode(context.CancellationToken);

        if (_syntaxFacts.GetModifiers(syntaxNode) != default)
        {
            RegisterCodeFix(context, AnalyzersResources.Order_modifiers, nameof(AnalyzersResources.Order_modifiers));
        }
    }

    protected override async Task FixAllAsync(
        Document document, ImmutableArray<Diagnostic> diagnostics, SyntaxEditor editor, CodeActionOptionsProvider fallbackOptions, CancellationToken cancellationToken)
    {
        var options = await document.GetAnalyzerOptionsProviderAsync(cancellationToken).ConfigureAwait(false);
        var option = GetCodeStyleOption(options);
        if (!_helpers.TryGetOrComputePreferredOrder(option.Value, out var preferredOrder))
        {
            return;
        }

        foreach (var diagnostic in diagnostics)
        {
            var memberDeclaration = diagnostic.Location.FindNode(cancellationToken);

            editor.ReplaceNode(memberDeclaration, (currentNode, _) =>
            {
                var modifiers = _syntaxFacts.GetModifiers(currentNode);
                var orderedModifiers = new SyntaxTokenList(
                    modifiers.OrderBy(CompareModifiers)
                             .Select((t, i) => t.WithTriviaFrom(modifiers[i])));

                var updatedMemberDeclaration = _syntaxFacts.WithModifiers(currentNode, orderedModifiers);
                return updatedMemberDeclaration;
            });
        }

        return;

        // Local functions

        int CompareModifiers(SyntaxToken t1, SyntaxToken t2)
            => GetOrder(t1) - GetOrder(t2);

        int GetOrder(SyntaxToken token)
            => preferredOrder.TryGetValue(token.RawKind, out var value) ? value : int.MaxValue;
    }
}
