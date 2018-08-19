// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.AddAnonymousTypeMemberName
{
    internal abstract class AbstractAddAnonymousTypeMemberNameCodeFixProvider<
        TExpressionSyntax,
        TAnonymousObjectMemberDeclaratorSyntax>
        : SyntaxEditorBasedCodeFixProvider
        where TExpressionSyntax : SyntaxNode
        where TAnonymousObjectMemberDeclaratorSyntax : SyntaxNode
    {
        protected AbstractAddAnonymousTypeMemberNameCodeFixProvider()
        {
        }

        protected abstract bool HasName(TAnonymousObjectMemberDeclaratorSyntax declarator);
        protected abstract TExpressionSyntax GetExpression(TAnonymousObjectMemberDeclaratorSyntax declarator);
        protected abstract TAnonymousObjectMemberDeclaratorSyntax WithName(TAnonymousObjectMemberDeclaratorSyntax currentDeclarator, string name);

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var document = context.Document;
            var cancellationToken = context.CancellationToken;

            var diagnostic = context.Diagnostics[0];
            var declarator = await GetMemberDeclaratorAsync(document, diagnostic, cancellationToken).ConfigureAwait(false);
            if (declarator == null)
            {
                return;
            }

            context.RegisterCodeFix(
                new MyCodeAction(c => FixAsync(document, diagnostic, c)),
                context.Diagnostics);
        }

        private async Task<TAnonymousObjectMemberDeclaratorSyntax> GetMemberDeclaratorAsync(
            Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var span = diagnostic.Location.SourceSpan;
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var node = root.FindNode(span, getInnermostNodeForTie: true) as TExpressionSyntax;
            if (node?.Span != span)
            {
                return null;
            }

            if (!(node.Parent is TAnonymousObjectMemberDeclaratorSyntax declarator))
            {
                return null;
            }

            // Can't add a name of the declarator already has a name.
            if (HasName(declarator))
            {
                return null;
            }

            return declarator;
        }

        protected override async Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            foreach (var diagnostic in diagnostics)
            {
                await FixOneAsync(
                    document, semanticModel, diagnostic, 
                    editor, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task FixOneAsync(
            Document document, SemanticModel semanticModel, Diagnostic diagnostic, 
            SyntaxEditor editor, CancellationToken cancellationToken)
        {
            var declarator = await GetMemberDeclaratorAsync(document, diagnostic, cancellationToken).ConfigureAwait(false);
            if (declarator == null)
            {
                return;
            }

            var semanticFacts = document.GetLanguageService<ISemanticFactsService>();
            var name = semanticFacts.GenerateNameForExpression(semanticModel, GetExpression(declarator), capitalize: true, cancellationToken);
            if (string.IsNullOrEmpty(name))
            {
                return;
            }

            editor.ReplaceNode(
                declarator,
                (current, _) =>
                {
                    var currentDeclarator = (TAnonymousObjectMemberDeclaratorSyntax)current;
                    return WithName(currentDeclarator, name);
                });
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument) 
                : base(FeaturesResources.Add_member_name, createChangedDocument)
            {
            }
        }
    }
}
