// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.OrderModifiers
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    internal class CSharpOrderModifiersCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds { get; } =
            ImmutableArray.Create(IDEDiagnosticIds.OrderModifiers);

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            context.RegisterCodeFix(
                new MyCodeAction(c => FixAsync(context.Document, context.Diagnostics[0], c)),
                context.Diagnostics);
            return SpecializedTasks.EmptyTask;
        }

        protected override async Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics, SyntaxEditor editor, CancellationToken cancellationToken)
        {
            var options = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
            var option = options.GetOption(CSharpCodeStyleOptions.PreferredModifierOrder);
            if (!CSharpOrderModifiersHelper.Instance.TryGetOrComputePreferredOrder(option.Value, out var preferredOrder))
            {
                return;
            }

            // order diagnostics from latest to earliest so we always process inner nodes before
            // outer nodes.
            //var orderedDiagnostics = diagnostics.OrderBy((d1, d2) => d1.Location.SourceSpan.Start - d2.Location.SourceSpan.Start);
            foreach (var diagnostic in diagnostics)
            {
                var memberDeclaration = diagnostic.Location.FindNode(cancellationToken);

                editor.ReplaceNode(memberDeclaration, (currentNode, _) =>
                {
                    var modifiers = currentNode.GetModifiers();
                    var orderedModifiers = SyntaxFactory.TokenList(
                        modifiers.OrderBy(CompareModifiers)
                                 .Select((t, i) => t.WithTriviaFrom(modifiers[i])));

                    var updatedMemberDeclaration = currentNode.WithModifiers(orderedModifiers);
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
