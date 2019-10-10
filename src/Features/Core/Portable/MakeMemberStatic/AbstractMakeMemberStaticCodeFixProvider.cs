// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.MakeMemberStatic
{
    internal abstract class AbstractMakeMemberStaticCodeFixProvider
        : SyntaxEditorBasedCodeFixProvider
    {
        internal sealed override CodeFixCategory CodeFixCategory => CodeFixCategory.Compile;

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            if (context.Diagnostics.Length != 1 ||
                context.Diagnostics[0].Location?.FindNode(context.CancellationToken) == null)
            {
                return Task.CompletedTask;
            }

            context.RegisterCodeFix(
                new MyCodeAction(c => FixAsync(context.Document, context.Diagnostics[0], c)),
                context.Diagnostics);

            return Task.CompletedTask;
        }

        protected sealed override Task FixAllAsync(Document document, ImmutableArray<Diagnostic> diagnostics, SyntaxEditor editor,
            CancellationToken cancellationToken)
        {
            for (int i = 0; i < diagnostics.Length; i++)
            {
                var declaration = diagnostics[i].Location?.FindNode(cancellationToken);
                if (declaration != null)
                {
                    editor.ReplaceNode(declaration,
                    (currentDeclaration, generator) => generator.WithModifiers(currentDeclaration, DeclarationModifiers.Static));
                }
            }

            return Task.CompletedTask;
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(FeaturesResources.Make_member_static, createChangedDocument, nameof(AbstractMakeMemberStaticCodeFixProvider))
            {
            }
        }
    }
}
