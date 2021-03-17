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
using Microsoft.CodeAnalysis.LanguageServices;
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

        // Intended for use by AbstractEditorFactory to add accessibility when creating a new file.
        internal static async Task<SyntaxNode> GetTransformedSyntaxRootAsync(Document document, CancellationToken cancellationToken)
        {
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
            var typeDeclarations = root.DescendantNodes().Where(node => syntaxFacts.IsTypeDeclaration(node));
            var editor = new SyntaxEditor(root, SyntaxGenerator.GetGenerator(document));
            foreach (var declaration in typeDeclarations)
            {
                UpdateDeclaration(declaration, editor, semanticModel, cancellationToken);
            }

            return editor.GetChangedRoot();
        }

        private static void UpdateDeclaration(SyntaxNode declaration, SyntaxEditor editor, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var symbol = semanticModel.GetDeclaredSymbol(declaration, cancellationToken);
            Contract.ThrowIfNull(symbol);

            var preferredAccessibility = GetPreferredAccessibility(symbol);

            // Check to see if we need to add or remove
            // If there's a modifier, then we need to remove it, otherwise no modifier, add it.
            editor.ReplaceNode(
                declaration,
                (currentDeclaration, _) => UpdateAccessibility(currentDeclaration, preferredAccessibility));

            return;

            SyntaxNode UpdateAccessibility(SyntaxNode declaration, Accessibility preferredAccessibility)
            {
                var generator = editor.Generator;

                // If there was accessibility on the member, then remove it.  If there was no accessibility, then add
                // the preferred accessibility for this member.
                return generator.GetAccessibility(declaration) == Accessibility.NotApplicable
                    ? generator.WithAccessibility(declaration, preferredAccessibility)
                    : generator.WithAccessibility(declaration, Accessibility.NotApplicable);
            }
        }

        protected sealed override async Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            foreach (var diagnostic in diagnostics)
            {
                var declaration = diagnostic.AdditionalLocations[0].FindNode(cancellationToken);
                var declarator = MapToDeclarator(declaration);
                UpdateDeclaration(declarator, editor, semanticModel, cancellationToken);
            }
        }

        private static Accessibility GetPreferredAccessibility(ISymbol symbol)
        {
            // If we have an overridden member, then if we're adding an accessibility modifier, use the
            // accessibility of the member we're overriding as both should be consistent here.
            if (symbol.GetOverriddenMember() is { DeclaredAccessibility: var accessibility })
                return accessibility;

            // Default abstract members to be protected, and virtual members to be public.  They can't be private as
            // that's not legal.  And these are reasonable default values for them.
            if (symbol is IMethodSymbol or IPropertySymbol or IEventSymbol)
            {
                if (symbol.IsAbstract)
                    return Accessibility.Protected;

                if (symbol.IsVirtual)
                    return Accessibility.Public;
            }

            // Otherwise, default to whatever accessibility no-accessibility means for this member;
            return symbol.DeclaredAccessibility;
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
