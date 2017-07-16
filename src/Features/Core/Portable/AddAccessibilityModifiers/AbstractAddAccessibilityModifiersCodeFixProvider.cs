// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Composition;
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

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            var priority = diagnostic.Severity == DiagnosticSeverity.Hidden
                ? CodeActionPriority.Low
                : CodeActionPriority.Medium;
            context.RegisterCodeFix(
                new MyCodeAction(priority, c => FixAsync(context.Document, context.Diagnostics.First(), c)),
                context.Diagnostics);
            return SpecializedTasks.EmptyTask;
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

                editor.ReplaceNode(
                    declaration,
                    (currentDeclaration, generator) =>
                    {
                        return generator.WithAccessibility(currentDeclaration, symbol.DeclaredAccessibility);
                    });
            }
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(CodeActionPriority priority, Func<CancellationToken, Task<Document>> createChangedDocument) 
                : base(FeaturesResources.Add_accessibility_modifiers, createChangedDocument, FeaturesResources.Add_accessibility_modifiers)
            {
                this.Priority = priority;
            }

            internal override CodeActionPriority Priority { get; }
        }
    }
}
