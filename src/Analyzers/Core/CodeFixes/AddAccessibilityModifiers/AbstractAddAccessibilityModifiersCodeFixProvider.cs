// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.AddAccessibilityModifiers
{
    internal abstract class AbstractAddAccessibilityModifiersCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        protected abstract SyntaxNode MapToDeclarator(SyntaxNode declaration);

        public sealed override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(IDEDiagnosticIds.AddAccessibilityModifiersDiagnosticId);

        internal sealed override CodeFixCategory CodeFixCategory => CodeFixCategory.CodeStyle;

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();

#if CODE_STYLE // 'CodeActionPriority' is not a public API, hence not supported in CodeStyle layer.
            var codeAction = new MyCodeAction(c => FixAsync(context.Document, context.Diagnostics.First(), c));
#else
            var priority = diagnostic.Severity == DiagnosticSeverity.Hidden
                ? CodeActionPriority.Low
                : CodeActionPriority.Medium;
            var codeAction = new MyCodeAction(priority, c => FixAsync(context.Document, context.Diagnostics.First(), c));
#endif
            context.RegisterCodeFix(
                codeAction,
                context.Diagnostics);
            return Task.CompletedTask;
        }

        protected sealed override async Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            foreach (var diagnostic in diagnostics)
            {
                var declaration = diagnostic.AdditionalLocations[0].FindNode(cancellationToken);
                var declarator = MapToDeclarator(declaration);

                var symbol = semanticModel.GetDeclaredSymbol(declarator, cancellationToken);

                // Check to see if we need to add or remove
                // If there's a modifier, then we need to remove it, otherwise no modifier, add it.
                editor.ReplaceNode(
                    declaration,
                    (currentDeclaration, generator) =>
                    {
                        return generator.GetAccessibility(currentDeclaration) == Accessibility.NotApplicable
                                    ? generator.WithAccessibility(currentDeclaration, symbol.DeclaredAccessibility) // No accessibility was declared, we need to add it
                                    : generator.WithAccessibility(currentDeclaration, Accessibility.NotApplicable); // There was an accessibility, so remove it                       
                    });
            }
        }

        private class MyCodeAction : CustomCodeActions.DocumentChangeAction
        {
#if CODE_STYLE // 'CodeActionPriority' is not a public API, hence not supported in CodeStyle layer.
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(AnalyzersResources.Add_accessibility_modifiers, createChangedDocument, AnalyzersResources.Add_accessibility_modifiers)
            {
            }
#else
            public MyCodeAction(CodeActionPriority priority, Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(AnalyzersResources.Add_accessibility_modifiers, createChangedDocument, AnalyzersResources.Add_accessibility_modifiers)
            {
                Priority = priority;
            }

            internal override CodeActionPriority Priority { get; }
#endif
        }
    }
}
