// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.RemoveUnusedVariable
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.RemoveUnusedVariable), Shared]
    internal partial class RemoveUnusedVariableCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        private const string CS0168 = nameof(CS0168);
        private const string CS0219 = nameof(CS0219);

        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(CS0168, CS0219);

        public async override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            foreach (var diagnostic in context.Diagnostics)
            {
                var root = await context.Document.GetSyntaxRootAsync().ConfigureAwait(false);
                var token = root.FindToken(diagnostic.Location.SourceSpan.Start);
                var ancestor = token.GetAncestor<LocalDeclarationStatementSyntax>();

                if (ancestor == null)
                {
                    return;
                }
            }

            context.RegisterCodeFix(
                new MyCodeAction(c => FixAsync(context.Document, context.Diagnostics.First(), c)),
                context.Diagnostics);

            return ;
        }

        protected override Task FixAllAsync(Document document, ImmutableArray<Diagnostic> diagnostics, SyntaxEditor editor, CancellationToken cancellationToken)
        {
            var root = editor.OriginalRoot;
            foreach (var diagnostic in diagnostics)
            {
                var token = root.FindToken(diagnostic.Location.SourceSpan.Start);
                var variableDeclarator = token.GetAncestor<VariableDeclaratorSyntax>();
                
                var variableDeclarators = token.GetAncestor<VariableDeclarationSyntax>().ChildNodes().Where(x => x is VariableDeclaratorSyntax);

                if (variableDeclarators.Count() == 1)
                {
                    editor.RemoveNode(token.GetAncestor<LocalDeclarationStatementSyntax>());
                }
                else if (variableDeclarators.Count() > 1)
                {
                    editor.RemoveNode(variableDeclarator);
                }
            }
            return SpecializedTasks.EmptyTask;
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument) :
                base(CSharpFeaturesResources.Remove_Unused_Variable, createChangedDocument, CSharpFeaturesResources.Remove_Unused_Variable)
            {
            }
        }
    }
}
