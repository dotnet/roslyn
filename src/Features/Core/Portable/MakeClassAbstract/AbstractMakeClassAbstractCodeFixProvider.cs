// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.MakeClassAbstract
{
    internal abstract class AbstractMakeClassAbstractCodeFixProvider<TClassDeclarationSyntax> : SyntaxEditorBasedCodeFixProvider
        where TClassDeclarationSyntax : SyntaxNode
    {
        protected abstract bool IsValidRefactoringContext(SyntaxNode? node, [NotNullWhen(true)] out TClassDeclarationSyntax? classDeclaration);

        internal sealed override CodeFixCategory CodeFixCategory => CodeFixCategory.Compile;

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            if (IsValidRefactoringContext(context.Diagnostics[0].Location?.FindNode(context.CancellationToken), out _))
            {
                context.RegisterCodeFix(
                    new MyCodeAction(c => FixAsync(context.Document, context.Diagnostics[0], c)),
                    context.Diagnostics);
            }

            return Task.CompletedTask;
        }

        protected sealed override Task FixAllAsync(Document document, ImmutableArray<Diagnostic> diagnostics, SyntaxEditor editor,
            CancellationToken cancellationToken)
        {
            for (var i = 0; i < diagnostics.Length; i++)
            {
                if (IsValidRefactoringContext(diagnostics[i].Location?.FindNode(cancellationToken), out var classDeclaration))
                {
                    editor.ReplaceNode(classDeclaration,
                        (currentClassDeclaration, generator) => generator.WithModifiers(currentClassDeclaration, DeclarationModifiers.Abstract));
                }
            }

            return Task.CompletedTask;
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(FeaturesResources.Make_class_abstract, createChangedDocument, FeaturesResources.Make_class_abstract)
            {
            }
        }
    }
}
