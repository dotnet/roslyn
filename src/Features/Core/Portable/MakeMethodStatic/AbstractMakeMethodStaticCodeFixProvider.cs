// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.MakeMethodStatic
{
    internal abstract class AbstractMakeMethodStaticCodeFixProvider<TMemberAccessSyntax> : SyntaxEditorBasedCodeFixProvider
        where TMemberAccessSyntax : SyntaxNode
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(IDEDiagnosticIds.MakeMethodStaticDiagnosticId);

        public abstract string Title { get; }

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            context.RegisterCodeFix(new MyCodeAction(Title,
                c => FixAsync(context.Document, context.Diagnostics[0], c)),
                context.Diagnostics);
            return SpecializedTasks.EmptyTask;
        }

        protected sealed override async Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor, CancellationToken cancellationToken)
        {
            var root = editor.OriginalRoot;
            var generator = editor.Generator;
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            foreach (var diagnostic in diagnostics)
            {
                var declaration = diagnostic.Location.FindNode(cancellationToken);
                editor.ReplaceNode(declaration, (d, g) => g.WithModifiers(d, DeclarationModifiers.Static));

                var methodSymbol = semanticModel.GetDeclaredSymbol(declaration);
                var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var references = await SymbolFinder.FindReferencesAsync(
                    methodSymbol, document.Project.Solution, cancellationToken).ConfigureAwait(false);
                var locations = references.Single(r => r.Definition == methodSymbol).Locations;
                var usages = locations.Select(loc => syntaxRoot.FindToken(loc.Location.SourceSpan.Start).Parent.FirstAncestorOrSelf<TMemberAccessSyntax>());
                foreach (var usage in usages)
                {
                    if (usage != null)
                    {
                        editor.ReplaceNode(usage, generator.IdentifierName(methodSymbol.Name));
                    }
                }
            }
        }

        private sealed class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedSolution)
                : base(title, createChangedDocument: createChangedSolution)
            {
            }
        }
    }
}
