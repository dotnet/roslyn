// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.MakeFieldReadonly
{
    internal abstract class AbstractMakeFieldReadonlyCodeFixProvider<TSymbolSyntax, TFieldDeclarationSyntax>
        : SyntaxEditorBasedCodeFixProvider
        where TSymbolSyntax : SyntaxNode
        where TFieldDeclarationSyntax : SyntaxNode
    {
        public override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(IDEDiagnosticIds.MakeFieldReadonlyDiagnosticId);

        internal sealed override CodeFixCategory CodeFixCategory => CodeFixCategory.CodeQuality;

        protected abstract SyntaxNode GetInitializerNode(TSymbolSyntax declaration);
        protected abstract ImmutableList<TSymbolSyntax> GetVariableDeclarators(TFieldDeclarationSyntax declaration);

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            context.RegisterCodeFix(new MyCodeAction(
                c => FixAsync(context.Document, context.Diagnostics[0], c)),
                context.Diagnostics);
            return Task.CompletedTask;
        }

        protected override async Task FixAllAsync(
            Document document,
            ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor,
            CancellationToken cancellationToken)
        {
            var declarators = new List<TSymbolSyntax>();
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            foreach (var diagnostic in diagnostics)
            {
                var diagnosticSpan = diagnostic.Location.SourceSpan;

                declarators.Add(root.FindNode(diagnosticSpan, getInnermostNodeForTie: true).FirstAncestorOrSelf<TSymbolSyntax>());
            }

            await MakeFieldReadonlyAsync(document, editor, declarators).ConfigureAwait(false);
        }

        private async Task MakeFieldReadonlyAsync(Document document, SyntaxEditor editor, List<TSymbolSyntax> declarators)
        {
            var declaratorsByField = declarators.GroupBy(g => g.FirstAncestorOrSelf<TFieldDeclarationSyntax>());

            foreach (var fieldDeclarators in declaratorsByField)
            {
                var declarationDeclarators = GetVariableDeclarators(fieldDeclarators.Key);

                if (declarationDeclarators.Count == fieldDeclarators.Count())
                {
                    var modifiers = WithReadOnly(editor.Generator.GetModifiers(fieldDeclarators.Key));

                    editor.SetModifiers(fieldDeclarators.Key, modifiers);
                }
                else
                {
                    var model = await document.GetSemanticModelAsync().ConfigureAwait(false);
                    var generator = editor.Generator;

                    foreach (var declarator in declarationDeclarators.Reverse())
                    {
                        var symbol = (IFieldSymbol)model.GetDeclaredSymbol(declarator);
                        var modifiers = generator.GetModifiers(fieldDeclarators.Key);

                        var newDeclaration = generator.FieldDeclaration(symbol.Name,
                                                                        generator.TypeExpression(symbol.Type),
                                                                        Accessibility.Private,
                                                                        fieldDeclarators.Contains(declarator)
                                                                            ? WithReadOnly(modifiers)
                                                                            : modifiers,
                                                                        GetInitializerNode(declarator))
                                                      .WithAdditionalAnnotations(Formatter.Annotation);

                        editor.InsertAfter(fieldDeclarators.Key, newDeclaration);
                    }

                    editor.RemoveNode(fieldDeclarators.Key, SyntaxRemoveOptions.KeepLeadingTrivia);
                }
            }
        }

        private static DeclarationModifiers WithReadOnly(DeclarationModifiers modifiers)
            => (modifiers - DeclarationModifiers.Volatile) | DeclarationModifiers.ReadOnly;

        private class MyCodeAction : CustomCodeActions.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(AnalyzersResources.Add_readonly_modifier, createChangedDocument)
            {
            }
        }
    }
}
