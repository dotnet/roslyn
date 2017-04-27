// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
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
    internal abstract class AbstractMakeFieldReadonlyCodeFixProvider
        <TFieldDeclarationSyntax, TVariableDeclaratorSyntax>
        : SyntaxEditorBasedCodeFixProvider
        where TFieldDeclarationSyntax : SyntaxNode
        where TVariableDeclaratorSyntax : SyntaxNode
    {
        public override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(IDEDiagnosticIds.MakeFieldReadonlyDiagnosticId);

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            context.RegisterCodeFix(new MyCodeAction(
                c => FixAsync(context.Document, context.Diagnostics[0], c)),
                context.Diagnostics);
            return SpecializedTasks.EmptyTask;
        }

        private async Task FixWithEditorAsync(
            Document document, SyntaxEditor editor, ImmutableArray<Diagnostic> diagnostics,
            CancellationToken cancellationToken)
        {
            foreach (var diagnostic in diagnostics)
            {
                var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var diagnosticSpan = diagnostic.Location.SourceSpan;

                var variableDeclarator = (TVariableDeclaratorSyntax)root.FindNode(diagnosticSpan);

                MakeFieldReadonly(document, editor, root, variableDeclarator);
            }
        }

        private async void MakeFieldReadonly(Document document, SyntaxEditor editor, SyntaxNode root, TVariableDeclaratorSyntax declaration)
        {
            var fieldDeclaration = (TFieldDeclarationSyntax)declaration.Parent.Parent;
            if (GetVariableDeclaratorCount(fieldDeclaration) == 1)
            {
                var generator = SyntaxGenerator.GetGenerator(document);
                var modifiers = generator.GetModifiers(fieldDeclaration);

                var newDeclaration = generator.WithModifiers(fieldDeclaration, modifiers | DeclarationModifiers.ReadOnly);
                editor.ReplaceNode(fieldDeclaration, newDeclaration);
            }
            else
            {
                var model = await document.GetSemanticModelAsync().ConfigureAwait(false);
                var symbol = (IFieldSymbol)model.GetDeclaredSymbol(declaration);

                var generator = SyntaxGenerator.GetGenerator(document);
                var newDeclaration = generator.FieldDeclaration(symbol.Name,
                                                                generator.TypeExpression(symbol.Type),
                                                                Accessibility.Private,
                                                                generator.GetModifiers(fieldDeclaration) | DeclarationModifiers.ReadOnly,
                                                                GetInitializerNode(declaration))
                                                .WithAdditionalAnnotations(Formatter.Annotation);

                var newFieldDeclaration = generator.RemoveNode(fieldDeclaration, declaration);

                editor.InsertAfter(fieldDeclaration, newDeclaration);
                editor.ReplaceNode(fieldDeclaration, newFieldDeclaration);
            }
        }

        internal abstract SyntaxNode GetInitializerNode(TVariableDeclaratorSyntax declaration);
        internal abstract int GetVariableDeclaratorCount(TFieldDeclarationSyntax declaration);

        protected override Task FixAllAsync(
            Document document,
            ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor,
            CancellationToken cancellationToken)
        {
            return FixWithEditorAsync(document, editor, diagnostics, cancellationToken);
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument) :
                base(FeaturesResources.Add_readonly_modifier, createChangedDocument)
            {
            }
        }
    }
}
