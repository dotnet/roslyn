// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.OrderModifiers
{
    internal abstract class AbstractOrderModifiersCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        private readonly ISyntaxFactsService _syntaxFacts;
        private readonly Option<CodeStyleOption<string>> _option;
        private readonly AbstractOrderModifiersHelpers _helpers;

        protected AbstractOrderModifiersCodeFixProvider(
            ISyntaxFactsService syntaxFacts,
            Option<CodeStyleOption<string>> option,
            AbstractOrderModifiersHelpers helpers)
        {
            _syntaxFacts = syntaxFacts;
            _option = option;
            _helpers = helpers;
        }

        protected abstract ImmutableArray<string> FixableCompilerErrorIds { get; }

        public sealed override ImmutableArray<string> FixableDiagnosticIds
            => FixableCompilerErrorIds.Add(IDEDiagnosticIds.OrderModifiersDiagnosticId);

        internal sealed override CodeFixCategory CodeFixCategory => CodeFixCategory.CodeStyle;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var syntaxTree = await context.Document.GetSyntaxTreeAsync(context.CancellationToken).ConfigureAwait(false);
            var syntaxNode = Location.Create(syntaxTree, context.Span).FindNode(context.CancellationToken);

            if (_syntaxFacts.GetModifiers(syntaxNode) != default)
            {
                context.RegisterCodeFix(
                    new MyCodeAction(c => FixAsync(context.Document, context.Diagnostics[0], c)),
                    context.Diagnostics);
            }
        }

        protected override async Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics, SyntaxEditor editor, CancellationToken cancellationToken)
        {
            var options = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
            var option = options.GetOption(_option);
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

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(FeaturesResources.Order_modifiers, createChangedDocument, FeaturesResources.Order_modifiers)
            {
            }
        }
    }
}
