// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.AddAnonymousTypeMemberName
{
    internal abstract class AbstractAddAnonymousTypeMemberNameCodeFixProvider<
        TExpressionSyntax,
        TAnonymousObjectInitializer,
        TAnonymousObjectMemberDeclaratorSyntax>
        : SyntaxEditorBasedCodeFixProvider
        where TExpressionSyntax : SyntaxNode
        where TAnonymousObjectInitializer : SyntaxNode
        where TAnonymousObjectMemberDeclaratorSyntax : SyntaxNode
    {
        protected AbstractAddAnonymousTypeMemberNameCodeFixProvider()
        {
        }

        protected abstract bool HasName(TAnonymousObjectMemberDeclaratorSyntax declarator);
        protected abstract TExpressionSyntax GetExpression(TAnonymousObjectMemberDeclaratorSyntax declarator);
        protected abstract TAnonymousObjectMemberDeclaratorSyntax WithName(TAnonymousObjectMemberDeclaratorSyntax declarator, SyntaxToken name);
        protected abstract IEnumerable<string> GetAnonymousObjectMemberNames(TAnonymousObjectInitializer initializer);

        internal sealed override CodeFixCategory CodeFixCategory => CodeFixCategory.CodeStyle;

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

            if (!(declarator.Parent is TAnonymousObjectInitializer))
            {
                return null;
            }

            return declarator;
        }

        protected override async Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor, CancellationToken cancellationToken)
        {
            // If we're only introducing one name, then add the rename annotation to
            // it so the user can pick a better name if they want.
            var annotation = diagnostics.Length == 1 ? RenameAnnotation.Create() : null;

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            foreach (var diagnostic in diagnostics)
            {
                await FixOneAsync(
                    document, semanticModel, diagnostic,
                    editor, annotation, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task FixOneAsync(
            Document document, SemanticModel semanticModel, Diagnostic diagnostic,
            SyntaxEditor editor, SyntaxAnnotation annotation, CancellationToken cancellationToken)
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

            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            editor.ReplaceNode(
                declarator,
                (current, generator) =>
                {
                    var currentDeclarator = (TAnonymousObjectMemberDeclaratorSyntax)current;
                    var initializer = (TAnonymousObjectInitializer)currentDeclarator.Parent;
                    var existingNames = GetAnonymousObjectMemberNames(initializer);
                    var anonymousType = current.Parent;
                    var uniqueName = NameGenerator.EnsureUniqueness(name, existingNames, syntaxFacts.IsCaseSensitive);

                    var nameToken = generator.Identifier(uniqueName);
                    if (annotation != null)
                    {
                        nameToken = nameToken.WithAdditionalAnnotations(annotation);
                    }

                    return WithName(currentDeclarator, nameToken);
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
